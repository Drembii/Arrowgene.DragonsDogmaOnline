#nullable enable
using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class ItemUseJobItemsHandler : GameStructurePacketHandler<C2SItemUseJobItemsReq>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(ItemUseJobItemsHandler));
        
        private readonly ItemManager _itemManager;

        public ItemUseJobItemsHandler(DdonGameServer server) : base(server)
        {
            _itemManager = server.ItemManager;
        }

        public override void Handle(GameClient client, StructurePacket<C2SItemUseJobItemsReq> packet)
        {
            S2CItemUpdateCharacterItemNtc ntc = new();
            ntc.UpdateType = 0x121;
            foreach (CDataItemUIdList itemUIdListElement in packet.Structure.ItemUIdList)
            {
                _itemManager.ConsumeItemByUId(Server, client.Character, StorageType.ItemBagJob, itemUIdListElement.UId, itemUIdListElement.Num);
            }

            client.Send(ntc);
            client.Send(new S2CItemUseJobItemsRes());
        }
    }
}