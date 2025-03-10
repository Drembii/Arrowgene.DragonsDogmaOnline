using Arrowgene.Buffers;
using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.GameServer.Dump;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;
using Arrowgene.Networking.Tcp.Consumer.BlockingQueueConsumption;
using System.Collections.Generic;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class QuestGetRewardBoxItemHandler : GameRequestPacketHandler<C2SQuestGetRewardBoxItemReq, S2CQuestGetRewardBoxItemRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(QuestGetRewardBoxItemHandler));

        public QuestGetRewardBoxItemHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CQuestGetRewardBoxItemRes Handle(GameClient client, C2SQuestGetRewardBoxItemReq packet)
        {
            // client.Send(GameFull.Dump_902);

            var rewardIndex = packet.ListNo;
            if (rewardIndex == 0 || rewardIndex > client.Character.QuestRewards.Count)
            {
                Logger.Error($"Illegal reward request sent to server.");
                return new S2CQuestGetRewardBoxItemRes() { Error = 1};
            }

            // Make zero based index
            var boxRewards = client.Character.QuestRewards[(int)(rewardIndex - 1)];

            S2CItemUpdateCharacterItemNtc updateCharacterItemNtc = new S2CItemUpdateCharacterItemNtc()
            {
                UpdateType = 0
            };

            foreach (var boxReward in packet.GetRewardBoxItemList)
            {
                var reward = boxRewards.Rewards[boxReward.UID];

                if (Server.ItemManager.IsItemWalletPoint(reward.ItemId))
                {
                    (WalletType walletType, uint amount) = Server.ItemManager.ItemToWalletPoint(reward.ItemId);
                    var result = Server.WalletManager.AddToWallet(client.Character, walletType, amount);
                    updateCharacterItemNtc.UpdateWalletList.Add(result);
                }
                else
                {
                    var result = Server.ItemManager.AddItem(Server, client.Character, StorageType.StorageBoxNormal, reward.ItemId, reward.Num);
                    updateCharacterItemNtc.UpdateItemList.AddRange(result);
                }
            }
            client.Send(updateCharacterItemNtc);

            // Remove this reward from the list
            client.Character.QuestRewards.RemoveAt((int)(rewardIndex - 1));

            return new S2CQuestGetRewardBoxItemRes();
        }
    }
}
