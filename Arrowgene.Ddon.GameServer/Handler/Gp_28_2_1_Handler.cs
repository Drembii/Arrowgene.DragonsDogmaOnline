using Arrowgene.Ddon.GameServer.Dump;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class Gp_28_2_1_Handler : PacketHandler<GameClient>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(Gp_28_2_1_Handler));


        public Gp_28_2_1_Handler(DdonGameServer server) : base(server)
        {
        }

        public override PacketId Id => PacketId.C2S_GP_28_2_1_REQ;

        public override void Handle(GameClient client, IPacket packet)
        {
            //client.Send(InGameDump.Dump_46);
            //client.Send(SelectedDump.Dump_46_A);
            client.Send(new Packet(PacketId.S2C_GP_28_2_2_RES, new byte[12]));
        }
    }
}
