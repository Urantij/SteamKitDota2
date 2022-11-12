using Microsoft.Extensions.Logging;
using SteamKit2;
using SteamKitDota2;
using SteamKitDota2.More;

namespace Example;
class Program
{
    static ILogger logger;
    static SteamClient steamClient;
    static SteamUser steamUser;
    static CallbackManager callbackMgr;

    static bool isRunning = false;
    /// <summary>
    /// Сессия начинается со Start, заканчивается на Stop
    /// </summary>
    static object? sessionObject = null;

    static readonly TimeSpan reconnectTime = TimeSpan.FromSeconds(10);

    static string username;
    static string password;

    static async Task Main(string[] appArgs)
    {
        Console.WriteLine("Hello, World!");

        if (!File.Exists("IgnoreMe"))
        {
            System.Console.WriteLine("Нужен файл IgnoreMe с логином и паролем.");
            System.Console.WriteLine("Логин на первой строчке.");
            System.Console.WriteLine("Пароль на второй.");
            return;
        }

        var lines = await File.ReadAllLinesAsync("IgnoreMe");
        username = lines[0];
        password = lines[1];

        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddSimpleConsole());

        logger = loggerFactory.CreateLogger(typeof(Program));

        steamClient = new SteamClient();
        callbackMgr = new CallbackManager(steamClient);

        var dotaHandler = new SteamDota(steamClient, callbackMgr, loggerFactory);
        steamClient.AddHandler(dotaHandler);

        steamUser = steamClient.GetHandler<SteamUser>()!;

        callbackMgr.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
        callbackMgr.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

        callbackMgr.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
        callbackMgr.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

        Start();

        while (true)
        {
            System.Console.WriteLine("da? SteamId64");
            string? line = Console.ReadLine();

            if (line == null || !ulong.TryParse(line, out ulong id))
                continue;

            var rpList = await dotaHandler.RequestRichPresence(id);

            var rp = rpList.response.rich_presence.FirstOrDefault();
            if (rp == null)
            {
                System.Console.WriteLine($"{nameof(rp)} null");
                continue;
            }

            var parsedRp = new RichPresenceKv(rp.rich_presence_kv);

            System.Console.WriteLine(parsedRp.raw);

            if (parsedRp.watchableGameId == null)
            {
                System.Console.WriteLine($"{nameof(parsedRp.watchableGameId)} null");
                continue;
            }

            var games = await dotaHandler.RequestSpecificSourceTvGames(parsedRp.watchableGameId.Value);

            var game = games.response.game_list.FirstOrDefault();
            if (game == null)
            {
                System.Console.WriteLine($"{nameof(game)} null");
                continue;
            }

            System.Console.WriteLine($"Игра {game.match_id} Счёт {game.radiant_score}:{game.dire_score}");
        }
    }

    public static void Start()
    {
        if (isRunning)
            return;

        isRunning = true;

        var thatObject = sessionObject = new();

        logger.LogInformation("Запускаем стим клиент...");

        Task.Run(() =>
        {
            TryConnect();

            // Без sessionObject
            // Если слишком быстро сделать Stop Start, в теории можно запустить два цикла обработки колбеков
            while (isRunning && thatObject == sessionObject)
            {
                // in order for the callbacks to get routed, they need to be handled by the manager
                callbackMgr.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        });
    }

    public static void Stop()
    {
        if (!isRunning)
            return;

        isRunning = false;

        sessionObject = null;

        logger.LogInformation("Останавливает стим клиент...");

        steamClient.Disconnect();
    }

    private static void TryConnect()
    {
        logger.LogInformation("Стим клиент пытается подключиться...");

        // begin the connection to steam
        steamClient.Connect();
    }

    private static void OnConnected(SteamClient.ConnectedCallback obj)
    {
        logger.LogInformation("Клиент стима подключился, выполняется логин...");

        steamUser.LogOn(new SteamUser.LogOnDetails
        {
            Username = username,
            Password = password,
        });
    }

    private static void OnDisconnected(SteamClient.DisconnectedCallback obj)
    {
        logger.LogInformation("Клиент стима потерял соединение. {UserInitiated}", obj.UserInitiated);

        if (isRunning)
        {
            Task.Run(async () =>
            {
                await Task.Delay(reconnectTime);

                TryConnect();
            });
        }
    }

    private static void OnLoggedOn(SteamUser.LoggedOnCallback obj)
    {
        logger.LogInformation("Логин завершён. {Result}", obj.Result);
    }

    private static void OnLoggedOff(SteamUser.LoggedOffCallback obj)
    {
        logger.LogInformation("Разлогинились. {Result}", obj.Result);
    }
}
