using SteamKit2;
using SteamKit2.GC.Dota.Internal;

namespace SteamKitDota2
{
    public partial class SteamDota
    {
        public class SourceTvGamesCallback : CallbackMsg
        {
            public CMsgGCToClientFindTopSourceTVGamesResponse response;

            public SourceTvGamesCallback(CMsgGCToClientFindTopSourceTVGamesResponse response)
            {
                this.response = response;
            }
        }
    }
}