using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;
using SteamKit2.Internal;

namespace SteamKitDota2
{
    public partial class SteamDota
    {
        //по какой то причине запрос сурс тв во первых даёт 2 ответа, один нужный один просто 10 игр, а во вторых жоб айди там непонятно какой
        //но ловить то хочется
        JobID? specificSourceTvJobId = null;
        JobID? sourceTvJobId = null;

        /// <summary>
        /// Максимум 10 результатов?
        /// 9 ммр бот подаёт 20.
        /// Если вызвать таких несколько, пизда случится
        /// </summary>
        /// <param name="lobbyIds"></param>
        /// <returns></returns>
        public AsyncJob<SpecificSourceTvGamesCallback>? RequestSpecificSourceTvGames(params ulong[] lobbyIds)
        {
            if (!Ready)
                return null;

            var protobuf = new ClientGCMsgProtobuf<CMsgClientToGCFindTopSourceTVGames>((uint)EDOTAGCMsg.k_EMsgClientToGCFindTopSourceTVGames);
            //protobuf.SourceJobID = client.GetNextJobID(); не работает с этим протобафом. рыли.
            protobuf.Body.lobby_ids.AddRange(lobbyIds);
            protobuf.Body.start_game = 0;

            var job = new AsyncJob<SpecificSourceTvGamesCallback>(Client, Client.GetNextJobID());
            specificSourceTvJobId = job.JobID;

            gameCoordinator.Send(protobuf, dotaAppId);

            return job;
        }

        public AsyncJob<SourceTvGamesCallback>? RequestSourceTvGames()
        {
            if (!Ready)
                return null;

            var protobuf = new ClientGCMsgProtobuf<CMsgClientToGCFindTopSourceTVGames>((uint)EDOTAGCMsg.k_EMsgClientToGCFindTopSourceTVGames);
            //protobuf.SourceJobID = client.GetNextJobID(); не работает с этим протобафом. рыли.
            protobuf.Body.start_game = 0;

            var job = new AsyncJob<SourceTvGamesCallback>(Client, Client.GetNextJobID());
            sourceTvJobId = job.JobID;

            gameCoordinator.Send(protobuf, dotaAppId);

            return job;
        }

        /// <summary>
        /// Spectate
        /// </summary>
        /// <param name="steamId">steamID64</param>
        /// <param name="live">Dota plus feature to watch the game without 5min delay</param>
        public AsyncJob<SpectateCallback>? Spectate(ulong steamId, bool live)
        {
            if (!Ready)
                return null;

            var protobuf = new ClientGCMsgProtobuf<CMsgSpectateFriendGame>((uint)EDOTAGCMsg.k_EMsgGCSpectateFriendGame)
            {
                SourceJobID = Client.GetNextJobID()
            };
            protobuf.Body.steam_id = steamId;
            protobuf.Body.live = live;

            gameCoordinator.Send(protobuf, dotaAppId);

            return new AsyncJob<SpectateCallback>(Client, protobuf.SourceJobID);
        }

        public AsyncJob<RichPresenceInfoCallback>? RequestRichPresence(params ulong[] steamIds)
        {
            if (!Ready)
                return null;

            var protobuf = new ClientMsgProtobuf<CMsgClientRichPresenceRequest>(EMsg.ClientRichPresenceRequest)
            {
                SourceJobID = Client.GetNextJobID()
            };
            protobuf.Body.steamid_request.AddRange(steamIds);

            protobuf.ProtoHeader.routing_appid = dotaAppId;

            this.Client.Send(protobuf);

            return new AsyncJob<RichPresenceInfoCallback>(Client, protobuf.SourceJobID);
        }
    }
}