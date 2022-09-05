using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class PartyPartyInvitePrepareAcceptHandler : StructurePacketHandler<GameClient, C2SPartyPartyInvitePrepareAcceptReq>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(PartyPartyInvitePrepareAcceptHandler));

        public PartyPartyInvitePrepareAcceptHandler(DdonGameServer server) : base(server)
        {
        }

        public override void Handle(GameClient client, StructurePacket<C2SPartyPartyInvitePrepareAcceptReq> packet)
        {
            Party newParty = client.PendingInvitedParty; // In case some other thread changes the value
            client.PendingInvitedParty = null;

            client.Send(new S2CPartyPartyInvitePrepareAcceptRes());

            // The invited player doesn't move to the new party leader's server until this packet is sent
            // Why this wasn't included in the Response packet directly beats me
            S2CPartyPartyInviteAcceptNtc inviteAcceptNtc = new S2CPartyPartyInviteAcceptNtc();
            inviteAcceptNtc.ServerId = Server.AssetRepository.ServerList[0].Id; // TODO: Get from config, or from DdonGameServer instance
            inviteAcceptNtc.PartyId = newParty.Id;
            inviteAcceptNtc.StageId = newParty.Leader.Stage.Id;
            inviteAcceptNtc.PositionId = 0; // TODO: Figure what this is about
            inviteAcceptNtc.MemberIndex = (byte) newParty.Members.Count;
            client.Send(inviteAcceptNtc);

            // Notify party leader of the accepted invitation
            S2CPartyPartyInviteJoinMemberNtc inviteJoinMemberNtc = new S2CPartyPartyInviteJoinMemberNtc();
            CDataPartyMemberMinimum newMemberMinimum = new CDataPartyMemberMinimum();
            newMemberMinimum.CommunityCharacterBaseInfo.CharacterId = client.Character.Id;
            newMemberMinimum.CommunityCharacterBaseInfo.CharacterName.FirstName = client.Character.CharacterInfo.FirstName;
            newMemberMinimum.CommunityCharacterBaseInfo.CharacterName.LastName = client.Character.CharacterInfo.LastName;
            newMemberMinimum.CommunityCharacterBaseInfo.ClanName = "SEX";
            newMemberMinimum.IsLeader = client == newParty.Leader; // This could probably be just always false
            newMemberMinimum.MemberIndex = inviteAcceptNtc.MemberIndex;
            newMemberMinimum.MemberType = 1;
            newMemberMinimum.PawnId = 0;
            inviteJoinMemberNtc.MemberMinimumList.Add(newMemberMinimum);
            newParty.Leader.Send(inviteJoinMemberNtc);
        }
    }
}