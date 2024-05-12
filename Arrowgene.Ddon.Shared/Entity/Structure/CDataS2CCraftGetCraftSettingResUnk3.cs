using Arrowgene.Buffers;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataS2CCraftGetCraftSettingResUnk3
    {
        public uint Unk0 { get; set; }
        public uint Unk1 { get; set; }
        public uint Unk2 { get; set; }
        public uint Unk3 { get; set; }
        public uint Unk4 { get; set; }
        public bool Unk5 { get; set; }
    
        public class Serializer : EntitySerializer<CDataS2CCraftGetCraftSettingResUnk3>
        {
            public override void Write(IBuffer buffer, CDataS2CCraftGetCraftSettingResUnk3 obj)
            {
                WriteUInt32(buffer, obj.Unk0);
                WriteUInt32(buffer, obj.Unk1);
                WriteUInt32(buffer, obj.Unk2);
                WriteUInt32(buffer, obj.Unk3);
                WriteUInt32(buffer, obj.Unk4);
                WriteBool(buffer, obj.Unk5);
            }
        
            public override CDataS2CCraftGetCraftSettingResUnk3 Read(IBuffer buffer)
            {
                CDataS2CCraftGetCraftSettingResUnk3 obj = new CDataS2CCraftGetCraftSettingResUnk3();
                obj.Unk0 = ReadUInt32(buffer);
                obj.Unk1 = ReadUInt32(buffer);
                obj.Unk2 = ReadUInt32(buffer);
                obj.Unk3 = ReadUInt32(buffer);
                obj.Unk4 = ReadUInt32(buffer);
                obj.Unk5 = ReadBool(buffer);
                return obj;
            }
        }
    }
}