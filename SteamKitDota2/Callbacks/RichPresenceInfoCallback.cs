using SteamKit2;
using SteamKit2.Internal;

namespace SteamKitDota2.Callbacks
{
    public partial class SteamDota
    {
        public class RichPresenceInfoCallback : CallbackMsg
        {
            public CMsgClientRichPresenceInfo response;

            public RichPresenceInfoCallback(CMsgClientRichPresenceInfo response)
            {
                this.response = response;
            }
        }
    }
}