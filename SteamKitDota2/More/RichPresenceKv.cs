using System.Text.RegularExpressions;

namespace SteamKitDota2.More;

public class RichPresenceKv
{
    static readonly Regex partyMembersRegex = new(@"members { steam_id: (\d+) }", RegexOptions.Compiled);

    /// <summary>
    /// "#DOTA_RP_PLAYING_AS"
    /// </summary>
    public string? status;
    /// <summary>
    /// "#DOTA_RP_PLAYING_AS" "#DOTA_RP_IDLE"
    /// </summary>
    public string? steam_display;

    public ulong? watchableGameId;

    public string? partyValue;

    //public ulong partyId;
    //public string partyState;
    //public bool party_Open;
    //public ulong[] party_Members;
    /// <summary>
    /// SteamId64
    /// </summary>
    public ulong[]? party_Members;

    /// <summary>
    /// Что-то непонятное здесь лежит. Когда играли аганим, было "1 — 1"
    /// "#DOTA_lobby_type_name_ranked"
    /// </summary>
    public string? lobby_type;
    /// <summary>
    /// "#npc_dota_hero_earthshaker"
    /// </summary>
    public string? hero;

    public SteamKit2.KeyValue raw;

    //TODO Определить, что нулл или не нулл
    public RichPresenceKv(byte[] kv_bytes)
    {
        using MemoryStream memory = new(kv_bytes);

        var kv = new SteamKit2.KeyValue();
        kv.TryReadAsBinary(memory);

        raw = kv;

        status = kv["status"].Value;
        steam_display = kv["steam_display"].Value;

        //var num_params = kv["num_params"] //3?

        var watchableGameIdKv = kv["WatchableGameID"];
        if (watchableGameIdKv.Value != null)
        {
            watchableGameId = watchableGameIdKv.AsUnsignedLong();
        }

        //var watching_server = kv["watching_server"]; //"[A:1:4293012484:18436]"
        //var watching_from_server = kv["watching_from_server"]; //"[A:1:1439054849:18436]"

        partyValue = kv["party"].Value;
        //"party_id: 27373182836692517 party_state: IN_MATCH open: false members { steam_id: 76561198083316966 }"
        //"party_id: 27447041842341340 party_state: IN_MATCH open: false members { steam_id: 76561198074384365 } members { steam_id: 76561198038260924 }"
        //"party_state: IN_MATCH"

        if (partyValue != null)
        {
            var matches = partyMembersRegex.Matches(partyValue);
            if (matches.Count > 0)
            {
                party_Members = matches.Select(m => ulong.Parse(m.Groups[1].Value)).ToArray();
            }
        }

        lobby_type = kv["param0"].Value;
        //var idk = kv["param1"]; //4? //level
        hero = kv["param2"].Value;
    }
}