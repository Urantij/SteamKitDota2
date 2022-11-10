using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;
using SteamKit2.Internal;

namespace SteamKitDota2;

public class SteamDotaSender
{
    readonly SteamClient client;
    readonly SteamGameCoordinator gameCoordinator;

    public SteamDotaSender(SteamClient client, SteamGameCoordinator gameCoordinator)
    {
        this.client = client;
        this.gameCoordinator = gameCoordinator;
    }

    internal void Hello()
    {
        var clientHello = new ClientGCMsgProtobuf<CMsgClientHello>((uint)EGCBaseClientMsg.k_EMsgGCClientHello);
        clientHello.Body.engine = ESourceEngine.k_ESE_Source2;
        clientHello.Body.client_session_need = (uint)EDOTAGCSessionNeed.k_EDOTAGCSessionNeed_UserInUINeverConnected;

        gameCoordinator.Send(clientHello, SteamDota.dotaAppId);
    }

    internal void Pong()
    {
        var pingResponse = new ClientGCMsgProtobuf<CMsgGCClientPing>((uint)EGCBaseClientMsg.k_EMsgGCPingResponse);

        gameCoordinator.Send(pingResponse, SteamDota.dotaAppId);
    }

    internal void Play()
    {
        var playGame = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
        playGame.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
        {
            game_id = new GameID(SteamDota.dotaAppId),
        });

        client.Send(playGame);
    }
}