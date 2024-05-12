using System.Collections.Generic;
using System.Linq;
using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class InstanceGetDropItemHandler : GameStructurePacketHandler<C2SInstanceGetDropItemReq>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(InstanceGetDropItemHandler));
        
        private readonly ItemManager _itemManager;

        public InstanceGetDropItemHandler(DdonGameServer server) : base(server)
        {
            this._itemManager = server.ItemManager;
        }

        public override void Handle(GameClient client, StructurePacket<C2SInstanceGetDropItemReq> packet)
        {
            List<InstancedGatheringItem> items = client.InstanceDropItemManager.GetAssets(packet.Structure.LayoutId, packet.Structure.SetId);
            
            S2CInstanceGetDropItemRes res = new S2CInstanceGetDropItemRes();
            res.LayoutId = packet.Structure.LayoutId;
            res.SetId = packet.Structure.SetId;
            res.GatheringItemGetRequestList = packet.Structure.GatheringItemGetRequestList;
            client.Send(res);

            S2CItemUpdateCharacterItemNtc ntc = new S2CItemUpdateCharacterItemNtc();
            ntc.UpdateType = 2;
            foreach (CDataGatheringItemGetRequest gatheringItemRequest in packet.Structure.GatheringItemGetRequestList)
            {
                InstancedGatheringItem dropItem = items[(int) gatheringItemRequest.SlotNo];
                this._itemManager.GatherItem(Server, client.Character, ntc, dropItem, gatheringItemRequest.Num);
            }

            client.Send(ntc);
        }
    }
}