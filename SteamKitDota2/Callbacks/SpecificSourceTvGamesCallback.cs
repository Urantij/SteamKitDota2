using SteamKit2;
using SteamKit2.GC.Dota.Internal;

namespace SteamKitDota2.Callbacks
{
    public partial class SteamDota
    {
        public class SpecificSourceTvGamesCallback : CallbackMsg
        {
            public List<CSourceTVGameSmall> games;

            public SpecificSourceTvGamesCallback(List<CSourceTVGameSmall> games)
            {
                this.games = games;
            }
        }
    }
}