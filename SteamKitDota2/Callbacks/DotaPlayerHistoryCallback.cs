using SteamKit2;
using SteamKit2.GC.Dota.Internal;

namespace SteamKitDota2;

public partial class SteamDota
{
    public class DotaPlayerHistoryCallback(CMsgDOTAGetPlayerMatchHistoryResponse response) : CallbackMsg
    {
        public CMsgDOTAGetPlayerMatchHistoryResponse Response { get; } = response;
    }
}