using Microsoft.Extensions.Logging;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;
using SteamKit2.Internal;
using SteamKitDota2.More;

namespace SteamKitDota2;

// Вообще, доки мсдн пишут, что ю шуд нот юзать классы в классах для группировки.
// Но Стимкит так делает, поэтому и я так делаю.
public partial class SteamDota : ClientMsgHandler
{
    public const uint dotaAppId = 570;

    /// <summary>
    /// Через какое количество времени повторить попытку отправить хелло.
    /// </summary>
    public static readonly TimeSpan helloRepeatDelay = TimeSpan.FromSeconds(7.5);
    /// <summary>
    /// После этого количества попыток отправить хелло будет выдан колбек <see cref="DotaHelloTimeoutCallback"/>
    /// </summary>
    public static readonly int helloRetriesLimit = 5;

    readonly ILogger? _logger;

    readonly SteamGameCoordinator gameCoordinator;

    readonly IReadOnlyDictionary<uint, Action<IPacketGCMsg>> dispatchMapGC;

    /// <summary>
    /// Сессия может вылететь из нескольких мест.
    /// И значит, может начаться в нескольких местах.
    /// </summary>
    readonly object sessionLocker = new();

    /// <summary>
    /// Токен даёт потокам следить, какая сейчас сессия.
    /// Сессия начинается с send.Play.
    /// Заканчивается, когда клиент вылетает, или когда заканчивается лимит попыток подключиться.
    /// </summary>
    CancellationTokenSource? sessionCancellationSource = null;
    /// <summary>
    /// Позволяет следить за вмешательство в хеллолуп
    /// </summary>
    object? helloLoopIdentity = null;

    /// <summary>
    /// Подключен к доте и готов к выполнению операций.
    /// </summary>
    public bool Ready { get; private set; } = false;

    readonly SteamDotaSender send;

    public SteamDota(SteamClient client, CallbackManager callbackMgr, ILoggerFactory? loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger(this.GetType());

        this.gameCoordinator = client.GetHandler<SteamGameCoordinator>()!;

        send = new(client, gameCoordinator);

        callbackMgr.Subscribe<SteamUser.LoggedOnCallback>(LoggedOnHandler);
        callbackMgr.Subscribe<SteamClient.DisconnectedCallback>(SteamDisconnectedHandler);
        callbackMgr.Subscribe<SteamGameCoordinator.MessageCallback>(GCMessageHandler);

        dispatchMapGC = new Dictionary<uint, Action<IPacketGCMsg>>()
        {
            { (uint)EGCBaseClientMsg.k_EMsgGCClientWelcome, WelcomeHandler },
            { (uint)EGCBaseClientMsg.k_EMsgGCPingRequest, PingRequestHandler },
            { (uint)EGCBaseClientMsg.k_EMsgGCClientConnectionStatus, ClientConnectionStatusHandler },
            { (uint)EDOTAGCMsg.k_EMsgGCSpectateFriendGameResponse, SpectateFriendGameResponseHandler },
            { (uint)EDOTAGCMsg.k_EMsgGCToClientFindTopSourceTVGamesResponse, FindTopSourceTvGamesHandler },
        };
    }

    // Здесь лежат методы, необходимые для работы хендлера.
    // Методы для разных операций лежат в SteamDotaOperations

    /// <summary>
    /// Отправляет плей, а затем начинает дудосить сервер приветами, пока не получит ответ или не запнётся о лимит.
    /// Так как токен меняется только здесь, чтобы предотвратить запуск нескольких сессий, нужно дать токен предыдущей сессии.
    /// И если он равен текущему токену, значит, это первый заход сюда с просьбой сделать новую сессию.
    /// </summary>
    /// <param name="prevSessionCTS">Токен предыдущей сессии</param>
    private void StartSession(CancellationTokenSource? prevSessionCTS)
    {
        CancellationTokenSource thisCTS;
        bool killedSession;
        lock (sessionLocker)
        {
            if (sessionCancellationSource != null)
            {
                if (sessionCancellationSource != prevSessionCTS)
                {
                    // Кто-то уже попытался запустить новую сессию из старой.
                    return;
                }

                killedSession = !sessionCancellationSource.IsCancellationRequested;
                try { sessionCancellationSource.Cancel(); } catch { }
                sessionCancellationSource.Dispose();
            }
            else killedSession = false;

            thisCTS = sessionCancellationSource = new();
        }
        if (killedSession)
        {
            _logger?.LogInformation(Events.DeadSession, "Сессия закрыта перед новой.");
        }

        object thisIdentity = helloLoopIdentity = new object();

        _logger?.LogInformation(Events.NewSession, "Начинаем новую сессию.");

        send.Play();

        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            await HelloLoopAsync(thisIdentity, thisCTS);
        });
    }

    async Task HelloLoopAsync(object identity, CancellationTokenSource sessionCTS)
    {
        int helloAttempts = 0;

        // Пока тот же хеллолуп, и пока та же сессия
        while (identity == helloLoopIdentity && !sessionCTS.IsCancellationRequested)
        {
            if (helloAttempts > helloRetriesLimit)
            {
                bool killedSession;
                lock (sessionLocker)
                {
                    killedSession = !sessionCTS.IsCancellationRequested;

                    try { sessionCTS.Cancel(); } catch { }
                    sessionCTS.Dispose();
                }
                if (killedSession)
                {
                    _logger?.LogInformation(Events.DeadSession, "Сессия закрыта из-за превышения лимита.");
                }

                await Task.Delay(TimeSpan.FromSeconds(10));

                StartSession(sessionCTS);
                return;
            }

            _logger?.LogDebug(Events.Hello, "Отправляем привет...");

            send.Hello();
            helloAttempts++;

            try
            {
                await Task.Delay(helloRepeatDelay, sessionCTS.Token);
            }
            catch { return; }
        }
    }

    private void DeclareReady()
    {
        _logger?.LogInformation(Events.Ready, "Хендлер готов.");

        helloLoopIdentity = null;

        Ready = true;

        Client.PostCallback(new DotaReadyCallback());
    }

    private void DeclareNotReady(string reason)
    {
        _logger?.LogInformation(Events.NotReady, "Хендлер не готов.");

        specificSourceTvJobId = null;
        sourceTvJobId = null;

        Ready = false;

        Client.PostCallback(new DotaNotReadyCallback(reason));
    }

    #region Received Callbacks
    private void LoggedOnHandler(SteamUser.LoggedOnCallback callback)
    {
        if (callback.Result == EResult.OK)
        {
            StartSession(null);
        }
    }

    private void SteamDisconnectedHandler(SteamClient.DisconnectedCallback callback)
    {
        bool killedSession;
        lock (sessionLocker)
        {
            if (sessionCancellationSource != null)
            {
                killedSession = !sessionCancellationSource.IsCancellationRequested;

                try { sessionCancellationSource.Cancel(); } catch { }
                sessionCancellationSource.Dispose();
                sessionCancellationSource = null;
            }
            else killedSession = false;
        }
        if (killedSession)
        {
            _logger?.LogInformation(Events.DeadSession, "Сессия закрыта, стимклиент закрылся.");
        }

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

    #region GC Callbacks
    private void WelcomeHandler(IPacketGCMsg payloadMessage)
    {
        _logger?.LogDebug(Events.Welcome, "Добро пожаловать. {ready}", Ready);

        if (Ready)
            return;

        DeclareReady();
    }

    private void PingRequestHandler(IPacketGCMsg payloadMessage)
    {
        send.Pong();
    }

    private void ClientConnectionStatusHandler(IPacketGCMsg payloadMessage)
    {
        var status = new ClientGCMsgProtobuf<CMsgConnectionStatus>(payloadMessage);

        _logger?.LogDebug(Events.ConnectionStatus, "Статус соединения. {status}", status);

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
                var cts = sessionCancellationSource;

                DeclareNotReady($"GCConnectionStatus: {status.Body.status}");

                StartSession(cts);
            }
        }
    }
    #endregion
}