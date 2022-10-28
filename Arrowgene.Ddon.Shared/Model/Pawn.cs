using System.Collections.Generic;
using Arrowgene.Ddon.Shared.Entity.Structure;

namespace Arrowgene.Ddon.Shared.Model
{
    public class Pawn
    {
        public Pawn(uint ownerCharacterId)
        {
            CharacterId = ownerCharacterId;
            Character = new Character();
            OnlineStatus = OnlineStatus.None;
            PawnReactionList = new List<CDataPawnReaction>();
            SpSkillList = new List<CDataSpSkill>();
        }

        /// <summary>
        /// Id of Pawn
        /// </summary>
        public uint Id  { get; set; }
        
        /// <summary>
        /// Id of character who this pawn belongs to
        /// </summary>
        public uint CharacterId { get; set; }
        
        public Character Character { get; set; }
        public byte HmType { get; set; }
        public byte PawnType { get; set; }
        public OnlineStatus OnlineStatus { get; set; }

        public List<CDataPawnReaction> PawnReactionList { get; set; }
        public List<CDataSpSkill> SpSkillList { get; set; }
    }
}
