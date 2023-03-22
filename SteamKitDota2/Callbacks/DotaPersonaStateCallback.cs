using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteamKit2;
using SteamKitDota2.More;
using static SteamKit2.Internal.CMsgClientPersonaState;

namespace SteamKitDota2;

public partial class SteamDota
{
    /// <summary>
    /// Пришёл ClientPersonaState
    /// Работает только если установить параметр setPersonaState=true в конструкторе хендлера. />
    /// </summary>
    public class DotaPersonaStateCallback : CallbackMsg
    {
        public readonly SteamID friendId;
        public readonly string playerName;
        public readonly DotaRichPresenceInfo? richPresence;

        public readonly Friend raw;

        public DotaPersonaStateCallback(SteamID friendId, string playerName, DotaRichPresenceInfo? richPresence, Friend raw)
        {
            this.friendId = friendId;
            this.playerName = playerName;
            this.richPresence = richPresence;
            this.raw = raw;
        }
    }
}
