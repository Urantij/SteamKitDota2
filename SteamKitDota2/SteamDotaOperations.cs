using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;
using SteamKit2.Internal;

namespace SteamKitDota2;

// Здесь лежат вещи, которые не нужны для жизни хендлера.

public partial class SteamDota
{
    // Эти протобафы не поддерживают установку JobId
    // Они всегда будет возвращать дефолтное значение - 18446744073709551615
    // Но AsyncJob нуждает в JobId. Поэтому при старте назначу им айди.
    // При этом RequestSpecificSourceTvGames возвращает 2 ответа. Один нужный и один общий.
    readonly JobID SpecificSourceTvGamesJobId;
    readonly JobID SourceTvGamesJobId;

    /// <summary>
    /// Максимум 10 результатов?
    /// 9 ммр бот подаёт 20.
    /// Нельзя вызывать несколько таких методов одновременно.
    /// <see cref="RequestSpecificSourceTvGames"/>
    /// <see cref="RequestSourceTvGames"/>
    /// </summary>
    /// <param name="lobbyIds">SteamId64</param>
    /// <returns></returns>
    public AsyncJob<SourceTvGamesCallback> RequestSpecificSourceTvGames(params ulong[] lobbyIds)
    {
        var protobuf = new ClientGCMsgProtobuf<CMsgClientToGCFindTopSourceTVGames>((uint)EDOTAGCMsg.k_EMsgClientToGCFindTopSourceTVGames);

        protobuf.Body.lobby_ids.AddRange(lobbyIds);
        protobuf.Body.start_game = 0;

        var job = new AsyncJob<SourceTvGamesCallback>(Client, SpecificSourceTvGamesJobId);

        gameCoordinator.Send(protobuf, dotaAppId);

        return job;
    }

    /// <summary>
    /// Нельзя вызывать несколько таких методов одновременно.
    /// <see cref="RequestSpecificSourceTvGames"/>
    /// <see cref="RequestSourceTvGames"/>
    /// </summary>
    /// <returns></returns>
    public AsyncJob<SourceTvGamesCallback> RequestSourceTvGames()
    {
        var protobuf = new ClientGCMsgProtobuf<CMsgClientToGCFindTopSourceTVGames>((uint)EDOTAGCMsg.k_EMsgClientToGCFindTopSourceTVGames);
        protobuf.Body.start_game = 0;

        var job = new AsyncJob<SourceTvGamesCallback>(Client, SourceTvGamesJobId);

        gameCoordinator.Send(protobuf, dotaAppId);

        return job;
    }

    /// <param name="steamId">SteamID64</param>
    /// <param name="live">Dota plus feature to watch the game without 5min delay</param>
    public AsyncJob<SpectateCallback> RequestSpectateFriendGame(ulong steamId, bool live)
    {
        var protobuf = new ClientGCMsgProtobuf<CMsgSpectateFriendGame>((uint)EDOTAGCMsg.k_EMsgGCSpectateFriendGame)
        {
            SourceJobID = Client.GetNextJobID()
        };
        protobuf.Body.steam_id = steamId;
        protobuf.Body.live = live;

        var job = new AsyncJob<SpectateCallback>(Client, protobuf.SourceJobID);

        gameCoordinator.Send(protobuf, dotaAppId);

        return job;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="steamIds">SteamId64</param>
    /// <returns></returns>
    public AsyncJob<RichPresenceInfoCallback> RequestRichPresence(params ulong[] steamIds)
    {
        var protobuf = new ClientMsgProtobuf<CMsgClientRichPresenceRequest>(EMsg.ClientRichPresenceRequest)
        {
            SourceJobID = Client.GetNextJobID()
        };
        protobuf.Body.steamid_request.AddRange(steamIds);

        protobuf.ProtoHeader.routing_appid = dotaAppId;

        var job = new AsyncJob<RichPresenceInfoCallback>(Client, protobuf.SourceJobID);

        this.Client.Send(protobuf);

        return job;
    }

    private void SpectateFriendGameResponseHandler(IPacketGCMsg payloadMessage)
    {
        var response = new ClientGCMsgProtobuf<CMsgSpectateFriendGameResponse>(payloadMessage);
        var callback = new SpectateCallback(response.Body)
        {
            JobID = response.TargetJobID
        };

        Client.PostCallback(callback);
    }

    private void FindTopSourceTvGamesHandler(IPacketGCMsg payloadMessage)
    {
        var response = new ClientGCMsgProtobuf<CMsgGCToClientFindTopSourceTVGamesResponse>(payloadMessage);

        // Кансер, но что поделать.
        if (response.Body.specific_games)
        {
            var callback = new SourceTvGamesCallback(response.Body)
            {
                JobID = SpecificSourceTvGamesJobId
            };
            Client.PostCallback(callback);
        }
        else
        {
            var callback = new SourceTvGamesCallback(response.Body)
            {
                JobID = SourceTvGamesJobId
            };
            Client.PostCallback(callback);
        }
    }

    private void ClientRichPresenceInfoHandler(IPacketMsg payloadMessage)
    {
        var response = new ClientMsgProtobuf<CMsgClientRichPresenceInfo>(payloadMessage);
        var callback = new RichPresenceInfoCallback(response.Body)
        {
            JobID = response.TargetJobID
        };
        Client.PostCallback(callback);
    }
}