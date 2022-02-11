using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;
using SteamKit2.Internal;

namespace SteamKitDota2
{
    /* Вообще, доки мсдн пишут, что ю маст нот (или шуд нот?) юзать классы в классах для группировки.
     * Но Стимкит так делает, поэтому и я так делаю */
    /* Наверное, было бы логичнее сделать мсдж хендлер для рич презенса
     * И вокруг него построить этот стимдота, тому шо ему больше нужны колбеки, чем сообщения
     * Но так впадлу. */
    public partial class SteamDota : ClientMsgHandler
    {
        const uint dotaAppId = 570;

        public static readonly TimeSpan helloRepeatDelay = TimeSpan.FromSeconds(7.5);
        /// <summary>
        /// После этого количества ретраев будет выдан колбек хеллотаймаут
        /// </summary>
        public static readonly int helloRetriesLimit = 5;

        readonly SteamGameCoordinator gameCoordinator;

        readonly Dictionary<uint, Action<IPacketGCMsg>> dispatchMapGC;

        CancellationTokenSource sessionCancellationSource = new();
        CancellationTokenSource? helloWorkerCancellationSource;

        /// <summary>
        /// Подключен к доте и готов к выполнению операций.
        /// </summary>
        public bool Ready { get; private set; } = false;

        public SteamDota(SteamGameCoordinator gameCoordinator, CallbackManager callbackMgr)
        {
            this.gameCoordinator = gameCoordinator;

            //в тевории нужно ловить логед и миседж через хендл миседж, но впадлу
            //к тому же дисконект там не поймаешь
            callbackMgr.Subscribe<SteamUser.LoggedOnCallback>(LoggedOnHandler);
            callbackMgr.Subscribe<SteamClient.DisconnectedCallback>(SteamDisconnectedHandler);
            callbackMgr.Subscribe<SteamGameCoordinator.MessageCallback>(GCMessageHandler);

            dispatchMapGC = new()
            {
                { (uint)EGCBaseClientMsg.k_EMsgGCClientWelcome, WelcomeHandler },
                { (uint)EGCBaseClientMsg.k_EMsgGCPingRequest, PingRequestHandler },
                { (uint)EGCBaseClientMsg.k_EMsgGCClientConnectionStatus, ClientConnectionStatusHandler },
                { (uint)EDOTAGCMsg.k_EMsgGCSpectateFriendGameResponse, SpectateFriendGameResponseHandler },
                { (uint)EDOTAGCMsg.k_EMsgGCToClientFindTopSourceTVGamesResponse, FindTopSourceTvGamesHandler },
            };
        }

        //

        private void DeclareReady()
        {
            if (helloWorkerCancellationSource != null)
                try { helloWorkerCancellationSource.Cancel(); helloWorkerCancellationSource.Dispose(); helloWorkerCancellationSource = null; } catch { }

            Ready = true;

            Client.PostCallback(new DotaReadyCallback());
        }

        private void DeclareNotReady(string reason)
        {
            specificSourceTvJobId = null;
            sourceTvJobId = null;

            Ready = false;

            Client.PostCallback(new DotaNotReadyCallback(reason));
        }

        #region Received Callbacks
        private void LoggedOnHandler(SteamUser.LoggedOnCallback obj)
        {
            if (obj.Result == EResult.OK)
            {
                //такого не должно быть, но блять
                try { sessionCancellationSource.Cancel(); sessionCancellationSource.Dispose(); } catch { }
                if (helloWorkerCancellationSource != null)
                    try { helloWorkerCancellationSource.Cancel(); helloWorkerCancellationSource.Dispose(); helloWorkerCancellationSource = null; } catch { }

                sessionCancellationSource = new();
                StartEnter(sessionCancellationSource.Token);
            }
        }

        private void SteamDisconnectedHandler(SteamClient.DisconnectedCallback obj)
        {
            try { sessionCancellationSource.Cancel(); sessionCancellationSource.Dispose(); } catch { }
            if (helloWorkerCancellationSource != null)
                try { helloWorkerCancellationSource.Cancel(); helloWorkerCancellationSource.Dispose(); helloWorkerCancellationSource = null; } catch { }

            if (Ready)
            {
                DeclareNotReady("Steam Disconnected");
            }
        }

        private void GCMessageHandler(SteamGameCoordinator.MessageCallback obj)
        {
            if (obj.AppID != dotaAppId)
                return;

            var payloadMessage = obj.Message;

            if (!dispatchMapGC.TryGetValue(payloadMessage.MsgType, out var handler))
                return;

            handler.Invoke(payloadMessage);
        }
        #endregion

        public override void HandleMsg(IPacketMsg packetMsg)
        {
            if (packetMsg.MsgType == EMsg.ClientRichPresenceInfo)
            {
                ClientRichPresenceInfoHandler(packetMsg);
            }
        }

        private void ClientRichPresenceInfoHandler(IPacketMsg payloadMessage)
        {
            var response = new ClientMsgProtobuf<CMsgClientRichPresenceInfo>(payloadMessage);

            var callback = new RichPresenceInfoCallback(response.Body)
            {
                JobID = response.TargetJobID
            };

            Client.PostCallback(callback);
        }

        #region GC Callbacks
        private void WelcomeHandler(IPacketGCMsg payloadMessage)
        {
            if (Ready)
                return;

            DeclareReady();
        }

        private void PingRequestHandler(IPacketGCMsg payloadMessage)
        {
            SendPong();
        }

        private void ClientConnectionStatusHandler(IPacketGCMsg payloadMessage)
        {
            var status = new ClientGCMsgProtobuf<CMsgConnectionStatus>(payloadMessage);

            if (status.Body.status == GCConnectionStatus.GCConnectionStatus_HAVE_SESSION)
            {
                if (!Ready)
                {
                    DeclareReady();
                }
            }
            else
            {
                if (Ready)
                {
                    DeclareNotReady($"GCConnectionStatus: {status.Body.status}");

                    //Чтобы всё наебалось, нужно чтобы дисконект пришел перед конекшн статусом. но это же невозможно?
                    StartHelloWorker(sessionCancellationSource.Token);
                }
            }
        }

        private void SpectateFriendGameResponseHandler(IPacketGCMsg payloadMessage)
        {
            var response = new ClientGCMsgProtobuf<CMsgSpectateFriendGameResponse>(payloadMessage);

            var callback = new SpectateCallback(response.Body)
            {
                JobID = response.TargetJobID
            };

            Client.PostCallback(callback);
        }

        private void FindTopSourceTvGamesHandler(IPacketGCMsg payloadMessage)
        {
            var response = new ClientGCMsgProtobuf<CMsgGCToClientFindTopSourceTVGamesResponse>(payloadMessage);

            //кансер, но что поделать
            if (response.Body.specific_games)
            {
                //хотел, шобы оно подписывалось само, но хз, как отписаться, если таймаут задачи произошёл.
                //без превращения асинкжоба в таск
                var callback = new SpecificSourceTvGamesCallback(response.Body.game_list);
                if (specificSourceTvJobId != null)
                {
                    callback.JobID = specificSourceTvJobId;
                    specificSourceTvJobId = null;
                }

                Client.PostCallback(callback);
            }
            else
            {
                var callback = new SourceTvGamesCallback(response.Body);

                if (sourceTvJobId != null)
                {
                    callback.JobID = sourceTvJobId;
                    sourceTvJobId = null;
                }

                Client.PostCallback(callback);
            }
        }
        #endregion

        /// <summary>
        /// Отправляет плей, а затем начинает дудосить сервер приветами, пока не получит ответ или не запнётся о лимит.
        /// </summary>
        /// <param name="cancellationToken"></param>
        private void StartEnter(CancellationToken cancellationToken)
        {
            SendPlay();

            StartHelloWorker(cancellationToken);
        }

        async void StartHelloWorker(CancellationToken cancellationToken)
        {
            /* воркер запускается, если хуйня была только запущена, или если она была реди и перестала
             * В любом случае, двух воркеров быть не должно. реди хуйня отменяется, запуск хуйни это сессия и она отменяется */
            var source = helloWorkerCancellationSource = new();
            try
            {
                using var workerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(source.Token, cancellationToken);

                try { await Task.Delay(1000, workerCancellationSource.Token); } catch { return; }

                if (Ready) return;

                int helloAttempts = 0;
                while (helloAttempts < helloRetriesLimit)
                {
                    SendHello();
                    helloAttempts++;

                    try { await Task.Delay(helloRepeatDelay, workerCancellationSource.Token); } catch { return; }
                    if (Ready) return;
                }

                Client.PostCallback(new DotaHelloTimeoutCallback());

                //это время, чтобы успеть дёрнуть рубильник отключения
                //типа смысл в том, что я хочу дисконнект клиента делать.
                //странная логика, конечно. ну да ладно.
                try { await Task.Delay(2000, workerCancellationSource.Token); } catch { return; }
                if (Ready) return;

                //хуёво, потому что я может быть хочу сделать дисконнект, если таймаут, а он сразу отправить херню, что уже лишнее
                StartEnter(cancellationToken);
            }
            finally
            {
                //А вообще, он, случаем, не всегда нулл тут будет?
                if (source == helloWorkerCancellationSource)
                {
                    helloWorkerCancellationSource.Dispose();
                    helloWorkerCancellationSource = null;
                }
            }
        }
    
        public static ulong GetSteamID64(uint steamID32)
        {
            return steamID32 + (ulong)76561197960265728;
        }

        public static uint GetSteamID32(ulong steamID64)
        {
            return (uint)(steamID64 - 76561197960265728);
        }
    }
}