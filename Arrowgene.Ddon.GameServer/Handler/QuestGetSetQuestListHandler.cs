using Arrowgene.Buffers;
using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.GameServer.Dump;
using Arrowgene.Ddon.GameServer.Quests;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Asset;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model.Quest;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class QuestGetSetQuestListHandler : GameStructurePacketHandler<C2SQuestGetSetQuestListReq>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(QuestGetQuestPartyBonusListHandler));

        public QuestGetSetQuestListHandler(DdonGameServer server) : base(server)
        {
        }

        public override void Handle(GameClient client, StructurePacket<C2SQuestGetSetQuestListReq> packet)
        {
            // client.Send(GameFull.Dump_132);

            S2CQuestGetSetQuestListRes res = new S2CQuestGetSetQuestListRes();
            foreach (var questId in client.Party.QuestState.GetActiveQuestIds())
            {
                var quest = QuestManager.GetQuest(questId);
                if (quest.QuestType == QuestType.World)
                {
                    /**
                     * World quests get added here instead of QuestGetWorldManageQuestListHandler because
                     * "World Manage Quests" are different from "World Quests". World manage quests appear
                     * to control the state of the game world (doors, paths, gates, etc.). World quests
                     * are random fetch, deliver and kill type quests.
                     */
                    res.SetQuestList.Add(new CDataSetQuestList()
                    {
                        Detail = new CDataSetQuestDetail() { IsDiscovery = quest.IsDiscoverable },
                        Param = quest.ToCDataQuestList(),
                    });
                }
            }

            client.Send(res);
        }
    }
}
