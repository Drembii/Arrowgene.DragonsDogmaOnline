using System.Collections.Generic;
using System.Linq;
using Arrowgene.Buffers;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.LoginServer.Handler
{
    public class GetCharacterListHandler : PacketHandler<LoginClient>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(GetCharacterListHandler));


        public GetCharacterListHandler(DdonLoginServer server) : base(server)
        {
        }

        public override PacketId Id => PacketId.C2L_GET_CHARACTER_LIST_REQ;

        public override void Handle(LoginClient client, IPacket packet)
        {
            IBuffer buffer = new StreamBuffer();
            buffer.WriteInt32(0, Endianness.Big); // error
            buffer.WriteInt32(0, Endianness.Big); // result

            List<CDataCharacterListInfo> characterListResponse = new List<CDataCharacterListInfo>();
            List<Character> characters = Database.SelectCharactersByAccountId(client.Account.Id);
            Logger.Info(client, $"Found: {characters.Count} Characters");
            foreach (Character c in characters)
            {
                CDataCharacterListInfo cResponse = new CDataCharacterListInfo();
                cResponse.CharacterListElement.CommunityCharacterBaseInfo.CharacterId = (uint)c.CharacterId;
                cResponse.CharacterListElement.CommunityCharacterBaseInfo.CharacterName.FirstName = c.FirstName;
                cResponse.CharacterListElement.CommunityCharacterBaseInfo.CharacterName.LastName = c.LastName;
                cResponse.CharacterListElement.CurrentJobBaseInfo.Job = c.Job;
                cResponse.CharacterListElement.CurrentJobBaseInfo.Level = (byte) c.ActiveCharacterJobData.Lv;
                // maybe?
                //cResponse.CharacterListElement.CurrentJobBaseInfo.Job = c.CharacterInfo.MatchingProfile.CurrentJob;
                //cResponse.CharacterListElement.CurrentJobBaseInfo.Level = (byte) c.CharacterInfo.MatchingProfile.CurrentJobLevel;
                //cResponse.CharacterListElement.EntryJobBaseInfo.Job = c.CharacterInfo.MatchingProfile.EntryJob;
                //cResponse.CharacterListElement.EntryJobBaseInfo.Level = (byte) c.CharacterInfo.MatchingProfile.EntryJobLevel;
                cResponse.EditInfo = c.EditInfo;
                cResponse.MatchingProfile = c.MatchingProfile;
                cResponse.EquipItemInfo = c.Equipment.getEquipmentAsCDataEquipItemInfo(c.Job, EquipType.Performance)
                    .Union(c.Equipment.getEquipmentAsCDataEquipItemInfo(c.Job, EquipType.Visual))
                    .ToList();                    

                characterListResponse.Add(cResponse);
            }
            
            EntitySerializer.Get<CDataCharacterListInfo>().WriteList(buffer, characterListResponse);
            Packet response = new Packet(PacketId.L2C_GET_CHARACTER_LIST_RES, buffer.GetAllBytes());
            client.Send(response);
        }
    }
}
