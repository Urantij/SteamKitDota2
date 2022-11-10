using SteamKit2;
using SteamKit2.GC.Dota.Internal;

namespace SteamKitDota2;

public partial class SteamDota
{
    public class SpectateCallback : CallbackMsg
    {
        public readonly CMsgSpectateFriendGameResponse response;

        public SpectateCallback(CMsgSpectateFriendGameResponse response)
        {
            this.response = response;
        }
    }
}