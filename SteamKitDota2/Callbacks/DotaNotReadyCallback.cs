using SteamKit2;

namespace SteamKitDota2
{
    public partial class SteamDota
    {
        public class DotaNotReadyCallback : CallbackMsg
        {
            public readonly string reason;

            public DotaNotReadyCallback(string reason)
            {
                this.reason = reason;
            }
        }
    }
}