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

    private readonly ILogger? _logger;

    private readonly SteamGameCoordinator gameCoordinator;
    private readonly SteamFriends friends;
    private readonly IReadOnlyDictionary<uint, Action<IPacketGCMsg>> dispatchMapGC;

    /// <summary>
    /// Сессия может вылететь из нескольких мест.
    /// И значит, может начаться в нескольких местах.
    /// </summary>
    private readonly Lock _helloSpamSessionLocker = new();

    // Клиент должен долбить сервера стима, процесс долбления начинается с плей сообщения, продолжается спамом хелло сообщениями
    // Этот процесс называется сессия
    // Затем есть 3 варианта
    // 1. Мы получаем вход. Спам завершаем, работаем
    // 2. Мы не получаем вход. Кидаем таймаут колбек. Если текущая сессия не была отменена, начинаем новую
    // 3. Дисконнект. Всё бросаем.

    // Клиент начинает долбёжку, когда делается вход, и когда приходит пакетик, что вы больше не в игре.
    // Сессия есть, если хелолупидентити не нул. Токен отмены может висеть.
    // После дисконнекта ВСЕГДА сессии быть не должно. Текущая сессия будет отменена, и если она увидит, что стс нулл, значит, ниче делать не надо.

    /// <summary>
    /// Токен даёт потокам следить, какая сейчас сессия.
    /// Сессия начинается с send.Play.
    /// Отменяется, когда клиент вылетает, или когда начинается новая сессия.
    /// Новая сессия начинается, когда клиент подключается и когда происходит дота таймаут.
    /// Если нулл, значит сессия ещё не была запущена в этом коннекте. После дисконнекта становится нулл.
    /// </summary>
    private CancellationTokenSource? _helloSpamSessionCts = null;

    /// <summary>
    /// Позволяет следить за вмешательство в хеллолуп.
    /// Если не нулл, спам луп запущен.
    /// </summary>
    private object? _helloSpamSessionIdentity = null;

    /// <summary>
    /// Подключен к доте и готов к выполнению операций.
    /// </summary>
    public bool Ready { get; private set; } = false;

    private readonly SteamDotaSender send;
    private readonly bool setPersonaState;

    /// <param name="setPersonaState">Тру, чтобы получать информацию через ClientPersonaState <see cref="DotaPersonaStateCallback"/></param>
    public SteamDota(SteamClient client, CallbackManager callbackMgr, bool setPersonaState,
        ILoggerFactory? loggerFactory)
    {
        this.setPersonaState = setPersonaState;

        _logger = loggerFactory?.CreateLogger(this.GetType());

        SpecificSourceTvGamesJobId = client.GetNextJobID();
        SourceTvGamesJobId = client.GetNextJobID();

        this.gameCoordinator = client.GetHandler<SteamGameCoordinator>()!;
        this.friends = client.GetHandler<SteamFriends>()!;

        send = new SteamDotaSender(client, gameCoordinator);

        callbackMgr.Subscribe<SteamUser.LoggedOnCallback>(LoggedOnHandler);
        callbackMgr.Subscribe<SteamUser.AccountInfoCallback>(AccountInfoHandler);
        callbackMgr.Subscribe<SteamClient.DisconnectedCallback>(SteamDisconnectedHandler);
        callbackMgr.Subscribe<SteamGameCoordinator.MessageCallback>(GCMessageHandler);

        dispatchMapGC = new Dictionary<uint, Action<IPacketGCMsg>>()
        {
            { (uint)EGCBaseClientMsg.k_EMsgGCClientWelcome, WelcomeHandler },
            { (uint)EGCBaseClientMsg.k_EMsgGCPingRequest, PingRequestHandler },
            { (uint)EGCBaseClientMsg.k_EMsgGCClientConnectionStatus, ClientConnectionStatusHandler },
            { (uint)EDOTAGCMsg.k_EMsgGCSpectateFriendGameResponse, SpectateFriendGameResponseHandler },
            { (uint)EDOTAGCMsg.k_EMsgGCToClientFindTopSourceTVGamesResponse, FindTopSourceTvGamesHandler },
            { (uint)EDOTAGCMsg.k_EMsgDOTAGetPlayerMatchHistoryResponse, GetPlayerMatchHistoryResponseHandler },
            { (uint)EDOTAGCMsg.k_EMsgGCMatchDetailsResponse, MatchDetailsResponseHandler }
        };
    }

    // Здесь лежат методы, необходимые для работы хендлера.
    // Методы для разных операций лежат в SteamDotaOperations

    /// <summary>
    /// Отправляет плей, а затем начинает дудосить сервер приветами, пока не получит ответ или не запнётся о лимит.
    /// Если спам сессия уже идёт, ниче не делает.
    /// Не делает проверку на онлайн клиента.
    /// </summary>
    private void StartSession(bool firstTime)
    {
        ResetRequestId();

        CancellationTokenSource thisCts;
        object thisIdentity;
        lock (_helloSpamSessionLocker)
        {
            if (_helloSpamSessionIdentity != null)
                return;

            // Если сессия создаётся из коннекта не в первый раз, значит стс токен уже должен быть
            // если его нет, значит, был вылет, и вход должен быть первым.
            if (!firstTime && _helloSpamSessionCts == null)
                return;

            thisCts = _helloSpamSessionCts = new CancellationTokenSource();
            thisIdentity = _helloSpamSessionIdentity = new object();
        }

        _logger?.LogInformation(Events.NewSession, "Начинаем новую сессию.");

        send.Play();

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), thisCts.Token);
            }
            catch
            {
                return;
            }

            await HelloLoopAsync(thisIdentity, thisCts);
        });
    }

    private async Task HelloLoopAsync(object identity, CancellationTokenSource sessionCts)
    {
        int helloAttempts = 0;

        // Пока тот же хеллолуп, и пока та же сессия
        while (true)
        {
            lock (_helloSpamSessionLocker)
            {
                if (_helloSpamSessionIdentity != identity || sessionCts.IsCancellationRequested)
                    return;
            }

            if (helloAttempts > helloRetriesLimit)
            {
                // идентити сессии не сбрасываем, чтобы никто другой не начал сессию, пока мы не подождали 10 сек
                // если будет выход из сети, то идентити там уйдёт в ноль с отменой токена.

                _logger?.LogInformation(Events.DeadSession, "Сессия закрыта из-за превышения лимита.");

                Client.PostCallback(new DotaHelloTimeoutCallback());

                // Если за 10 секунд ниче не изменилось, создаём новую сессию
                // Если токен отменили, либо клиент вылетел, и нам тут больше нечего делать вообще
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), sessionCts.Token);
                }
                catch
                {
                    return;
                }

                lock (_helloSpamSessionLocker)
                {
                    if (_helloSpamSessionCts == null || _helloSpamSessionCts.IsCancellationRequested)
                        return;
                    _helloSpamSessionIdentity = null;
                }

                // Если клиент не вырубили вручную выше, то ниче другое по сути не запустит сессию
                // Сессия запускается при входе и когда приходит статус ниже. статус не пришёл на вход - на вылет тоже не придёт

                StartSession(false);
                return;
            }

            _logger?.LogDebug(Events.Hello, "Отправляем привет...");

            send.Hello();
            helloAttempts++;

            try
            {
                await Task.Delay(helloRepeatDelay, sessionCts.Token);
            }
            catch
            {
                return;
            }
        }
    }

    private void DeclareReady()
    {
        _logger?.LogInformation(Events.Ready, "Хендлер готов.");

        lock (_helloSpamSessionLocker)
        {
            _helloSpamSessionIdentity = null;
        }

        Ready = true;

        Client.PostCallback(new DotaReadyCallback());
    }

    private void DeclareNotReady(string reason)
    {
        _logger?.LogInformation(Events.NotReady, "Хендлер не готов. ({reason})", reason);

        Ready = false;

        Client.PostCallback(new DotaNotReadyCallback(reason));
    }

    #region Received Callbacks

    private void LoggedOnHandler(SteamUser.LoggedOnCallback callback)
    {
        if (callback.Result == EResult.OK)
        {
            StartSession(true);
        }
    }

    private void AccountInfoHandler(SteamUser.AccountInfoCallback obj)
    {
        if (setPersonaState)
        {
            // Требуется, чтобы получать клиентперсонастейт
            friends.SetPersonaState(EPersonaState.Invisible);
        }
    }

    private void SteamDisconnectedHandler(SteamClient.DisconnectedCallback callback)
    {
        bool killedSession;
        lock (_helloSpamSessionLocker)
        {
            if (_helloSpamSessionCts != null)
            {
                killedSession = !_helloSpamSessionCts.IsCancellationRequested;

                try
                {
                    _helloSpamSessionCts.Cancel();
                }
                catch
                {
                }

                _helloSpamSessionCts.Dispose();
                _helloSpamSessionCts = null;
            }
            else killedSession = false;

            _helloSpamSessionIdentity = null;
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
        else if (packetMsg.MsgType == EMsg.ClientPersonaState)
        {
            ClientPersonaStateHandler(packetMsg);
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
                DeclareNotReady($"GCConnectionStatus: {status.Body.status}");

                StartSession(false);
            }
        }
    }

    #endregion
}