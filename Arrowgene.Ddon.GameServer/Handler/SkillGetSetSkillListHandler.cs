using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class SkillGetSetSkillListHandler : StructurePacketHandler<GameClient, C2SSkillGetSetSkillListReq>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(SkillGetSetSkillListHandler));

        public SkillGetSetSkillListHandler(DdonGameServer server) : base(server)
        {
        }

        public override void Handle(GameClient client, StructurePacket<C2SSkillGetSetSkillListReq> packet)
        {
            // This response should return skills set for every job.
            client.Send(new S2CSkillGetSetSkillListRes() {
                SetAcquierementParam = client.Character.CustomSkills
            });
        }
    }
}