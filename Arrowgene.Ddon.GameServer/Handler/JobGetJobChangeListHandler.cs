﻿using Arrowgene.Ddon.GameServer.Dump;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;
using Arrowgene.Ddon.Shared;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class JobGetJobChangeListHandler : PacketHandler<GameClient>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(JobGetJobChangeListHandler));


        public JobGetJobChangeListHandler(DdonGameServer server) : base(server)
        {
        }

        public override PacketId Id => PacketId.C2S_JOB_GET_JOB_CHANGE_LIST_REQ;

        public override void Handle(GameClient client, IPacket packet)
        {
            S2CJobGetJobChangeListRes jobChangeList = EntitySerializer.Get<S2CJobGetJobChangeListRes>().Read(InGameDump.Dump_52.AsBuffer());
            // Add Hunter info so you can see the hunter job on the skill change menu.
            jobChangeList.JobReleaseInfo.Add(new CDataJobChangeInfo() {
                JobId = JobId.Hunter
            });
            jobChangeList.JobChangeInfo.Add(new CDataJobChangeInfo() {
                JobId = JobId.Hunter
            });
            client.Send(jobChangeList);
        }
    }
}
