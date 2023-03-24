using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static SteamKit2.Internal.CMsgClientPersonaState.Friend;

namespace SteamKitDota2.More;

public class DotaRichPresenceInfo
{
    public static readonly Regex partyMembersRegex = new(@"members { steam_id: (?'id'\d+) }", RegexOptions.Compiled);

    // При входе в игру этот протобаф даёт информацию.ServiceMethodResponse (147)
    /// <summary>
    /// Есть всегда. Вроде как.
    /// В зависимости от статуса в параметрах лежат разные вещи. Например, #DOTA_RP_PLAYING_AS в первом будет тип лобби (ранкед, демо), во втором уровень, в третьем герой.
    /// "#DOTA_RP_PLAYING_AS"
    /// "#DOTA_RP_INIT", "#DOTA_RP_IDLE", "#DOTA_RP_SPECTATING", "#DOTA_RP_FINDING_MATCH", "#DOTA_RP_GAME_IN_PROGRESS_CUSTOM"
    /// #DOTA_RP_AWAY #DOTA_RP_BOTPRACTICE #DOTA_RP_BUSY #DOTA_RP_CASTING #DOTA_RP_COACHING #DOTA_RP_COOPBOT #DOTA_RP_DISCONNECT #DOTA_RP_FINDING_EVENT_MATCH #DOTA_RP_FINDING_YEAR_BEAST_BRAWL #DOTA_RP_GAME_IN_PROGRESS #DOTA_RP_GAME_IN_PROGRESS_CUSTOM_UNNAMED #DOTA_RP_HERO_SELECTION #DOTA_RP_LEAGUE_MATCH #DOTA_RP_LEAGUE_MATCH_PLAYING_AS #DOTA_RP_LOBBY_CUSTOM #DOTA_RP_LOBBY_CUSTOM_UNNAMED #DOTA_RP_LOOKING_TO_PLAY надоело честно говоря
    /// </summary>
    public readonly string? status;
    /// <summary>
    /// Есть всегда. Вроде как.
    /// И вроде как всегда равен <see cref="status"/>
    /// "#DOTA_RP_PLAYING_AS" "#DOTA_RP_IDLE"
    /// </summary>
    public readonly string? steam_display;

    /// <summary>
    /// Также известный как LobbyId.
    /// Может быть просто 0, если его нет.
    /// </summary>
    public readonly ulong? watchableGameId;

    /// <summary>
    /// "party_id: 27373182836692517 party_state: IN_MATCH open: false members { steam_id: 76561198083316966 }"
    /// "party_id: 27447041842341340 party_state: IN_MATCH open: false members { steam_id: 76561198074384365 } members { steam_id: 76561198038260924 }"
    /// <see cref="party_Members"/>
    /// </summary>
    public readonly string? partyValue;

    //public ulong partyId;
    //public string partyState;
    //public bool party_Open;
    //public ulong[] party_Members;
    /// <summary>
    /// SteamId64
    /// </summary>
    public readonly ulong[]? party_Members;

    /// <summary>
    /// lobby: lobby_id: 28176233713761692 lobby_state: UI password: true game_mode: DOTA_GAMEMODE_AP member_count: 1 max_member_count: 10 name: "hueglot" lobby_type: 1 (True):True
    /// </summary>
    public readonly string? lobbyValue;

    /// <summary>
    /// Есть всегда. Вроде как.
    /// Когда был в главном меню, было 0.
    /// Что-то непонятное здесь лежит. Когда играли аганим, было "1 — 1"
    /// "#DOTA_lobby_type_name_ranked"
    /// </summary>
    public readonly string? lobby_type;
    
    public readonly IReadOnlyDictionary<string, string?> raw;

    public DotaRichPresenceInfo(Dictionary<string, string?> dict)
    {
        raw = dict;

        status = raw.GetValueOrDefault("status");
        steam_display = raw.GetValueOrDefault("steam_display");

        //var num_params = kv["num_params"] //3?

        var watchableGameIdKv = raw.GetValueOrDefault("WatchableGameID");
        if (watchableGameIdKv != null)
        {
            watchableGameId = ulong.Parse(watchableGameIdKv);
        }

        //var watching_server = kv["watching_server"]; //"[A:1:4293012484:18436]"
        //var watching_from_server = kv["watching_from_server"]; //"[A:1:1439054849:18436]"

        partyValue = raw.GetValueOrDefault("party");
        lobbyValue = raw.GetValueOrDefault("lobby");
        //"party_state: IN_MATCH"

        if (partyValue != null)
        {
            var matches = partyMembersRegex.Matches(partyValue);
            if (matches.Count > 0)
            {
                party_Members = matches.Select(m => ulong.Parse(m.Groups["id"].Value)).ToArray();
            }
        }
    }

    public DotaRichPresenceInfo(List<KV> list)
        : this(CreateDict(list))
    {

    }

    public DotaRichPresenceInfo(byte[] kv_bytes)
        : this(CreateDict(kv_bytes))
    {

    }

    static Dictionary<string, string?> CreateDict(List<KV> list)
    {
        return list.ToDictionary(key => key.key, value => value.ShouldSerializevalue() ? value.value : null);
    }

    static Dictionary<string, string?> CreateDict(byte[] kv_bytes)
    {
        using MemoryStream memory = new(kv_bytes);

        var kv = new SteamKit2.KeyValue();
        kv.TryReadAsBinary(memory);

        return kv.Children.ToDictionary(key => key.Name!, value => value.AsString());
    }
}
