using System.Linq;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class PawnGetMyPawnListHandler : StructurePacketHandler<GameClient, C2SPawnGetMypawnListReq>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(PawnGetMyPawnListHandler));


        public PawnGetMyPawnListHandler(DdonGameServer server) : base(server)
        {
        }

        public override void Handle(GameClient client, StructurePacket<C2SPawnGetMypawnListReq> packet)
        {
            client.Send(new S2CPawnGetMypawnListRes() {
                PawnList = client.Character.Pawns.Select((pawn, index) => new CDataPawnList() {
                    PawnId = (int) pawn.PawnId,
                    SlotNo = (uint) (index+1),
                    Name = pawn.Name,
                    Sex = pawn.EditInfo.Sex,
                    PawnListData = new CDataPawnListData() 
                    {
                        Job = pawn.Job,
                        Level = pawn.ActiveCharacterJobData.Lv
                        // TODO: CraftRank, PawnCraftSkillList, CommentSize, LatestReturnDate
                    }
                    // TODO: Unk0, Unk1, Unk2
                }).ToList(),
                // TODO: PartnerInfo
                PartnerInfo = new CDataPartnerPawnInfo() {
                    PawnId = client.Character.Pawns.FirstOrDefault()?.PawnId ?? 0,
                    Likability = 1,
                    Personality = 1
                },
            });
        }
    }
}
