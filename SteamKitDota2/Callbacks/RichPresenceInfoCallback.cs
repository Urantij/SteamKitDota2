using SteamKit2;
using SteamKit2.Internal;

namespace SteamKitDota2;

public partial class SteamDota
{
    /// <summary>
    /// Для получения полезной информации можно использовать <see cref="SteamKitDota2.More.DotaRichPresenceInfo"/>
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