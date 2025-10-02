using SteamKit2;
using SteamKit2.Internal;
using SteamKitDota2.More;

namespace SteamKitDota2;

public partial class SteamDota
{
    /// <summary>
    /// Для получения полезной информации можно использовать <see cref="DotaRichPresenceInfo.FromRichPresence"/>
    /// </summary>
    public class RichPresenceInfoCallback : CallbackMsg
    {
        public readonly CMsgClientRichPresenceInfo response;

        public RichPresenceInfoCallback(CMsgClientRichPresenceInfo response)
        {
            this.response = response;
        }
    }
}