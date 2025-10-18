using Microsoft.Extensions.Logging;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;
using SteamKit2.Internal;
using SteamKitDota2.More;

namespace SteamKitDota2;

// Здесь лежат вещи, которые не нужны для жизни хендлера.

// я не виноват
class RequestMatchJobHand(ulong matchId, AsyncJob<SteamDota.MatchDetailsCallback> job)
{
    public ulong MatchId { get; } = matchId;
    public AsyncJob<SteamDota.MatchDetailsCallback> Job { get; } = job;
}

public partial class SteamDota
{
    // Эти протобафы не поддерживают установку JobId
    // Они всегда будет возвращать дефолтное значение - 18446744073709551615
    // Но AsyncJob нуждает в JobId. Поэтому при старте назначу им айди.
    // При этом RequestSpecificSourceTvGames возвращает 2 ответа. Один нужный и один общий.
    readonly JobID SpecificSourceTvGamesJobId;
    readonly JobID SourceTvGamesJobId;

    // мы живём кансером, мы управляем кансер, мы и есть кансер
    private ulong? currentHistoryJobId = null;

    /// <summary>
    /// Я НЕ ЗНАЮ, Я ЗАБЫЛ ПРОВЕРИТЬ, КАК ЭТОТ АЙДИ РАБОТАЕТ.
    /// </summary>
    private uint requestId = 0;

    private void ResetRequestId() => requestId = 0;

    private readonly List<RequestMatchJobHand> _requestMatchJobHands = [];

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
        var protobuf =
            new ClientGCMsgProtobuf<CMsgClientToGCFindTopSourceTVGames>(
                (uint)EDOTAGCMsg.k_EMsgClientToGCFindTopSourceTVGames);

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
        var protobuf =
            new ClientGCMsgProtobuf<CMsgClientToGCFindTopSourceTVGames>(
                (uint)EDOTAGCMsg.k_EMsgClientToGCFindTopSourceTVGames);
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

    /// <summary>
    /// Просит 20 матчей из истории игрока.
    /// Игрок должен быть в списке друзей бота.
    /// Если друга не будет, команда просто уйдёт в таймаут через какое то время.
    /// Алсо не поддерживает несколько одновременных запросов, потому что стимкит момент.
    /// <param name="accountId">steamid3 так называемый. [U:1:87654571] 87654571 отсюда</param>
    /// </summary>
    public AsyncJob<DotaPlayerHistoryCallback> RequestMatchHistory(uint accountId, ulong startAtMatchId = 0,
        bool includePracticeMatches = false,
        bool includeCustomGames = true, bool includeEventGames = true)
    {
        var protobuf =
            new ClientGCMsgProtobuf<CMsgDOTAGetPlayerMatchHistory>((uint)EDOTAGCMsg.k_EMsgDOTAGetPlayerMatchHistory)
            {
                SourceJobID = Client.GetNextJobID(),
            };
        protobuf.Body.account_id = accountId;
        protobuf.Body.start_at_match_id = startAtMatchId;
        protobuf.Body.matches_requested = 20;
        protobuf.Body.request_id = requestId++;
        protobuf.Body.include_practice_matches = includePracticeMatches;
        protobuf.Body.include_custom_games = includeCustomGames;
        protobuf.Body.include_event_games = includeEventGames;

        // в дота клиенте реалм 1, по дефолту ноль. и так работает...
        // protobuf.Header.Proto.realm = 1;

        // по какой то нахуй причине просто создание этого джоба приведёт к вылету бота когда придёт ответ
        // если не создавать жоб и всё делать также, всё будет нормально))) здорово
        // алсо дота клиент не юзает сурсжоб айди в этом сообщении, но поддерживает
        var job = new AsyncJob<DotaPlayerHistoryCallback>(Client, protobuf.SourceJobID);

        currentHistoryJobId = protobuf.SourceJobID.Value;

        // если в ЭТОМ моменте заменить сурс жоб айди, то создание жоба не вылетит бота. вот так.
        // но тогда сурс будет дефолтным значением и он не сможет при ловле протобафа респонса жоб тригернуть
        // я проста не понимаю нахуй
        // я просто не понимаю
        // если сделать это присваивание ПЕРЕД созданием жоба, вылеты будут. ну это типа дефолтное значение, наверн должно быть
        // то есть он крашит, если получает ответ с нестандартым жоб айди?
        protobuf.ProtoHeader.job_id_source = protobuf.ProtoHeader.job_id_target;

        gameCoordinator.Send(protobuf, dotaAppId);

        return job;
    }

    /// <summary>
    /// не запрашивай уже запрошенный матч, пока первый жоб не закончит работу.
    /// </summary>
    /// <param name="matchId"></param>
    /// <returns></returns>
    public AsyncJob<MatchDetailsCallback> RequestMatchDetails(ulong matchId)
    {
        var protobuf =
            new ClientGCMsgProtobuf<CMsgGCMatchDetailsRequest>((uint)EDOTAGCMsg.k_EMsgGCMatchDetailsRequest)
            {
                // клиент использует жоб айди, но местный десериализатор ломается, если респонс имеет сурс жоб айди.
                // менять там его без рефлексии нельзя, так что вот
                // SourceJobID = Client.GetNextJobID(),
            };

        protobuf.Body.match_id = matchId;

        // в дота клиенте реалм 1, по дефолту ноль. и так работает...
        // protobuf.Header.Proto.realm = 1;

        // Там какой то дефолтный жоб айди, и если так юзать, то он смешивается с другими колбеками...
        JobID jobId = Client.GetNextJobID(); // protobuf.SourceJobID

        var job = new AsyncJob<MatchDetailsCallback>(Client, jobId);

        var hand = new RequestMatchJobHand(matchId, job);

        job.ToTask().ContinueWith(_ =>
        {
            lock (_requestMatchJobHands)
            {
                _requestMatchJobHands.Remove(hand);
            }
        }, TaskContinuationOptions.ExecuteSynchronously); // просто так синхронность

        lock (_requestMatchJobHands)
        {
            _requestMatchJobHands.Add(hand);
        }

        gameCoordinator.Send(protobuf, dotaAppId);

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

    private void GetPlayerMatchHistoryResponseHandler(IPacketGCMsg payloadMessage)
    {
        ulong jobId = currentHistoryJobId.Value;
        currentHistoryJobId = null;

        var response = new ClientGCMsgProtobuf<CMsgDOTAGetPlayerMatchHistoryResponse>(payloadMessage);

        var callback = new DotaPlayerHistoryCallback(response.Body)
        {
            JobID = jobId
        };
        Client.PostCallback(callback);
    }

    private void MatchDetailsResponseHandler(IPacketGCMsg payloadMessage)
    {
        var response = new ClientGCMsgProtobuf<CMsgGCMatchDetailsResponse>(payloadMessage);

        RequestMatchJobHand? hand = null;

        lock (_requestMatchJobHands)
        {
            hand = _requestMatchJobHands.FirstOrDefault(h => h.MatchId == response.Body.match.match_id);
            if (hand != null)
                _requestMatchJobHands.Remove(hand);
        }

        if (hand == null)
        {
            _logger?.LogWarning("Пришёл мач дитейлс который не ждали {id}", response.Body.match.match_id);
            return;
        }

        var callback = new MatchDetailsCallback(response.Body)
        {
            JobID = hand.Job.JobID
        };
        Client.PostCallback(callback);
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

    private void ClientPersonaStateHandler(IPacketMsg packetMsg)
    {
        // Всё это необходимо, потому что похожий колбек в SteamFriends даёт не всю возможную информацию.
        // https://github.com/SteamRE/SteamKit/blob/0d321f232cc10f2755d9a21028ab2160f18c040f/SteamKit2/SteamKit2/Steam/Handlers/SteamFriends/SteamFriends.cs#L880
        var protobuf = new ClientMsgProtobuf<CMsgClientPersonaState>(packetMsg);

        foreach (var friend in protobuf.Body.friends)
        {
            if (friend.gameid != dotaAppId)
                continue;

            DotaRichPresenceInfo? rpInfo;
            if (friend.rich_presence.Count > 0)
            {
                rpInfo = new DotaRichPresenceInfo(friend.rich_presence);
            }
            else rpInfo = null;

            var callback = new DotaPersonaStateCallback(friend.friendid, friend.player_name, rpInfo, friend);
            Client.PostCallback(callback);
        }
    }
}