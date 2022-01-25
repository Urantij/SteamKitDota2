using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;
using SteamKit2.Internal;

namespace SteamKitDota2
{
    /* Для удобства вынес отдельно методы, которые просто отправляю сообщения */
    public partial class SteamDota
    {
        internal void SendHello()
        {
            var clientHello = new ClientGCMsgProtobuf<CMsgClientHello>((uint)EGCBaseClientMsg.k_EMsgGCClientHello);
            clientHello.Body.engine = ESourceEngine.k_ESE_Source2;
            clientHello.Body.client_session_need = (uint)EDOTAGCSessionNeed.k_EDOTAGCSessionNeed_UserInUINeverConnected;
            gameCoordinator.Send(clientHello, dotaAppId);
        }

        internal void SendPong()
        {
            var pingResponse = new ClientGCMsgProtobuf<CMsgGCClientPing>((uint)EGCBaseClientMsg.k_EMsgGCPingResponse);
            gameCoordinator.Send(pingResponse, dotaAppId);
        }

        internal void SendPlay()
        {
            var playGame = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);

            playGame.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
            {
                game_id = new GameID(dotaAppId),
            });

            Client.Send(playGame);
        }
    }
}