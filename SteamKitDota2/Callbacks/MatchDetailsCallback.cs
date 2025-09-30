using SteamKit2;
using SteamKit2.GC.Dota.Internal;

namespace SteamKitDota2;

public partial class SteamDota
{
    public class MatchDetailsCallback(CMsgGCMatchDetailsResponse response) : CallbackMsg
    {
        public CMsgGCMatchDetailsResponse Response { get; } = response;
    }
}