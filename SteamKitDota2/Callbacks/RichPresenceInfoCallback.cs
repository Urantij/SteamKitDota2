using SteamKit2;
using SteamKit2.Internal;

namespace SteamKitDota2;

public partial class SteamDota
{
    public class RichPresenceInfoCallback : CallbackMsg
    {
        public readonly CMsgClientRichPresenceInfo response;

        public RichPresenceInfoCallback(CMsgClientRichPresenceInfo response)
        {
            this.response = response;
        }
    }
}