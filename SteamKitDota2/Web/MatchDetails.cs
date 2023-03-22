using System.Collections;
using SteamKit2;

namespace SteamKitDota2.Web;

public partial class DotaApi
{
    //https://wiki.teamfortress.com/wiki/WebAPI/GetMatchDetails
    public class MatchDetails
    {
        public class Player
        {
            public uint account_id;
            public byte player_slot;
            public uint hero_id;
            public int leaver_status;

            internal Player(KeyValue kv)
            {
                account_id = kv["account_id"].AsUnsignedInteger();
                player_slot = kv["player_slot"].AsUnsignedByte();
                hero_id = kv["hero_id"].AsUnsignedInteger();
                leaver_status = kv["leaver_status"].AsInteger();
            }

            /// <summary>
            /// Пока что возвращает только команду, true - radiance
            /// </summary>
            /// <returns></returns>
            public bool ParsePlayerSlot()
            {
                //   ┌─────────────── Team (false if Radiant, true if Dire).
                //   │ ┌─┬─┬─┬─────── Not used.
                //   │ │ │ │ │ ┌─┬─┬─ The position of a player within their team (0-4).
                //   │ │ │ │ │ │ │ │
                //   0 0 0 0 0 0 0 0

                BitArray array = new(new byte[] { player_slot });
                //как я понял, в этом массиве биты расположены наоборот от схемы

                bool radiant = !array[7];
                //тут нужно 3 бита перевести в байт, но мне очень впадлу, потому что это не нужно

                return radiant;
            }
        }

        public const string MatchNotFoundError = "Match ID not found";
        public const string PracticeNotAvailableError = "Practice matches are not available via GetMatchDetails";

        public Player[]? players;

        public bool? radiant_win;

        /// <summary>
        /// Unix timestamp of when the match began. In seconds.
        /// </summary>
        public long start_time;

        /// <summary>
        /// "Match ID not found"
        /// </summary>
        public string? error;

        /// <summary>
        /// -1 - Invalid
        /// 0 - Public matchmaking
        /// 1 - Practise
        /// 2 - Tournament
        /// 3 - Tutorial
        /// 4 - Co-op with bots.
        /// 5 - Team match
        /// 6 - Solo Queue
        /// 7 - Ranked
        /// 8 - 1v1 Mid
        /// </summary>
        public int lobby_type;
        /// <summary>
        /// 0 - None
        /// 1 - All Pick
        /// 2 - Captain's Mode
        /// 3 - Random Draft
        /// 4 - Single Draft
        /// 5 - All Random
        /// 6 - Intro
        /// 7 - Diretide
        /// 8 - Reverse Captain's Mode
        /// 9 - The Greeviling
        /// 10 - Tutorial
        /// 11 - Mid Only
        /// 12 - Least Played
        /// 13 - New Player Pool
        /// 14 - Compendium Matchmaking
        /// 15 - Co-op vs Bots
        /// 16 - Captains Draft
        /// 18 - Ability Draft
        /// 20 - All Random Deathmatch
        /// 21 - 1v1 Mid Only
        /// 22 - Ranked Matchmaking
        /// 23 - Turbo Mode
        /// </summary>
        public int game_mode;

        internal MatchDetails(KeyValue kv)
        {
            var playersNode = kv["players"];
            if (playersNode.Name != null)
            {
                players = playersNode.Children.Select(ch => new Player(ch)).ToArray();
            }

            var winNode = kv["radiant_win"];
            if (winNode.Name != null)
            {
                radiant_win = winNode.Value != "0";
            }
            else
            {
                radiant_win = null;
            }

            var errorNode = kv["error"];
            if (errorNode.Name != null)
            {
                error = errorNode.Value;
            }

            var startTimeNode = kv["start_time"];
            if (startTimeNode.Name != null)
            {
                start_time = startTimeNode.AsLong();
            }

            lobby_type = kv["lobby_type"].AsInteger();
            game_mode = kv["game_mode"].AsInteger();
        }
    }
}