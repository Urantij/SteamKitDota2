using SteamKit2;

namespace SteamKitDota2;

public partial class SteamDota
{
    /// <summary>
    /// Лимит попыток подключиться исчерпан, возможно стоит перезапустить клиент стима.
    /// </summary>
    public class DotaHelloTimeoutCallback : CallbackMsg
    {
    }
}