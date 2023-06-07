﻿using Arrowgene.Buffers;
using Arrowgene.Ddon.GameServer.Dump;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class QuestQuestProgressHandler : PacketHandler<GameClient>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(QuestQuestProgressHandler));


        public QuestQuestProgressHandler(DdonGameServer server) : base(server)
        {
        }

        public override PacketId Id => PacketId.C2S_QUEST_QUEST_PROGRESS_REQ;

        public override void Handle(GameClient client, IPacket packet)
        {
            IBuffer inBuffer = new StreamBuffer(packet.Data);
            inBuffer.SetPositionStart();
            uint data0 = inBuffer.ReadUInt32(Endianness.Big);
            uint data1 = inBuffer.ReadUInt32(Endianness.Big);
            uint data2 = inBuffer.ReadUInt32(Endianness.Big);

            IBuffer outBuffer = new StreamBuffer();
            outBuffer.WriteInt32(0, Endianness.Big);
            outBuffer.WriteInt32(0, Endianness.Big);
            outBuffer.WriteByte(0); // QuestProgressResult
            outBuffer.WriteUInt32(data2, Endianness.Big); // QuestScheduleId
            outBuffer.WriteUInt32(0, Endianness.Big); // QuestProgressStateList
            //client.Send(new Packet(PacketId.S2C_QUEST_QUEST_PROGRESS_RES, outBuffer.GetAllBytes()));

            client.Send(GameFull.Dump_166);
            client.Send(GameFull.Dump_168);
            client.Send(GameFull.Dump_170);
            client.Send(GameFull.Dump_172);
            client.Send(GameFull.Dump_175);
            client.Send(GameFull.Dump_177);
            client.Send(GameFull.Dump_179);
            client.Send(GameFull.Dump_181);
            client.Send(GameFull.Dump_185);
            client.Send(GameFull.Dump_188);
            client.Send(GameFull.Dump_190);
            client.Send(GameFull.Dump_294);
            client.Send(GameFull.Dump_297);
            client.Send(GameFull.Dump_299);
            client.Send(GameFull.Dump_524);
        }
    }
}
