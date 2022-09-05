using System.Collections.Generic;
using Arrowgene.Buffers;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataContextEquipData
    {
        public CDataContextEquipData(CDataEquipItemInfo equipItemInfo)
        {
            ItemId = (ushort) equipItemInfo.ItemId;
            ColorNo = equipItemInfo.Color;
            // QualityParam?
            WeaponCrestDataList = equipItemInfo.WeaponCrestDataList;
            ArmorCrestDataList = equipItemInfo.ArmorCrestDataList;
        }

        public CDataContextEquipData()
        {
            ItemId=0;
            ColorNo=0;
            QualityParam=0;
            WeaponCrestDataList=new List<CDataWeaponCrestData>();
            ArmorCrestDataList=new List<CDataArmorCrestData>();
        }

        public ushort ItemId { get; set; }
        public byte ColorNo { get; set; }
        public uint QualityParam { get; set; }
        public List<CDataWeaponCrestData> WeaponCrestDataList { get; set; }
        public List<CDataArmorCrestData> ArmorCrestDataList { get; set; }

        public class Serializer : EntitySerializer<CDataContextEquipData>
        {
            public override void Write(IBuffer buffer, CDataContextEquipData obj)
            {
                WriteUInt16(buffer, obj.ItemId);
                WriteByte(buffer, obj.ColorNo);
                WriteUInt32(buffer, obj.QualityParam);
                WriteEntityList<CDataWeaponCrestData>(buffer, obj.WeaponCrestDataList);
                WriteEntityList<CDataArmorCrestData>(buffer, obj.ArmorCrestDataList);
            }

            public override CDataContextEquipData Read(IBuffer buffer)
            {
                CDataContextEquipData obj = new CDataContextEquipData();
                obj.ItemId = ReadUInt16(buffer);
                obj.ColorNo = ReadByte(buffer);
                obj.QualityParam = ReadUInt32(buffer);
                obj.WeaponCrestDataList = ReadEntityList<CDataWeaponCrestData>(buffer);
                obj.ArmorCrestDataList = ReadEntityList<CDataArmorCrestData>(buffer);
                return obj;
            }
        }
    }
}
