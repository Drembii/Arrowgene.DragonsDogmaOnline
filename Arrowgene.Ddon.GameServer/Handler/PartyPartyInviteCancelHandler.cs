using Arrowgene.Ddon.GameServer.Party;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class PartyPartyInviteCancelHandler : GameStructurePacketHandler<C2SPartyPartyInviteCancelReq>
    {
        private static readonly ServerLogger Logger =
            LogProvider.Logger<ServerLogger>(typeof(PartyPartyChangeLeaderHandler));

        public PartyPartyInviteCancelHandler(DdonGameServer server) : base(server)
        {
        }

        
        /// <summary>
        /// There seems to be no way to
        /// </summary>
        /// <param name="client"></param>
        /// <param name="packet"></param>
        public override void Handle(GameClient client, StructurePacket<C2SPartyPartyInviteCancelReq> packet)
        {
            Logger.Info(client, $"C2SPartyPartyInviteCancelReq -> ServerId:{packet.Structure.ServerId} PartyId:{packet.Structure.PartyId}");
            S2CPartyPartyInviteCancelRes res = new S2CPartyPartyInviteCancelRes();
            
            PartyGroup party = client.Party;
            if (party == null)
            {
                Logger.Error(client,
                    $"can not cancel invite, no party assigned");
                // TODO error response
                return;
            }
            
            //Server.PartyManager.GetPartyInvitation()

            S2CPartyPartyInviteCancelNtc ntc = new S2CPartyPartyInviteCancelNtc();
            party.SendToAll(ntc);

            //party.CancelInvite();


            client.Send(res);
            Logger.Info(client, $"cancel invite for PartyId:{party.Id}");
        }
    }
}
