using System;
using System.Collections.Generic;
using System.Linq;
using Arrowgene.Ddon.Shared.Entity.Structure;

namespace Arrowgene.Ddon.Shared.Model
{
    #nullable enable
    public class Equipment
    {
        private static readonly byte EQUIP_SLOT_NUMBER = 15;

        private Dictionary<JobId, Dictionary<EquipType, List<Item?>>> equipment;

        public Equipment(Dictionary<JobId, Dictionary<EquipType, List<Item?>>> equipment)
        {
            this.equipment = equipment;
        }
        
        public Equipment()
        {
            equipment = new Dictionary<JobId, Dictionary<EquipType, List<Item?>>>();
            foreach (JobId job in Enum.GetValues(typeof(JobId)))
            {
                equipment[job] = new Dictionary<EquipType, List<Item?>>();
                foreach (EquipType equipType in Enum.GetValues(typeof(EquipType)))
                {
                    equipment[job][equipType] = Enumerable.Repeat<Item?>(null, EQUIP_SLOT_NUMBER).ToList();
                }
            }
        }

        public Dictionary<JobId, Dictionary<EquipType, List<Item?>>> getAllEquipment()
        {
            return equipment;
        }

        public List<Item?> getEquipment(JobId job, EquipType equipType)
        {
            return equipment[job][equipType];
        }

        public Item? getEquipItem(JobId job, EquipType equipType, byte slot) {
            return equipment[job][equipType][slot-1];
        }

        public Item? setEquipItem(Item newItem, JobId job, EquipType equipType, byte slot) {
            Item? oldItem = getEquipItem(job, equipType, slot);
            equipment[job][equipType][slot-1] = newItem;
            return oldItem;
        }


        public List<CDataContextEquipData> getEquipmentAsCDataContextEquipData(JobId job, EquipType equipType)
        {
            // In the context equipment lists, the index is the slot. An element with all info set to 0 has to be in place if a slot is not filled
            return getEquipment(job, equipType)
                .Select(x => x == null ? new CDataContextEquipData() : new CDataContextEquipData()
                {
                    ItemId = (ushort) x.ItemId,
                    ColorNo = x.Color,
                    QualityParam = x.Unk3,
                    WeaponCrestDataList = x.WeaponCrestDataList,
                    ArmorCrestDataList = x.ArmorCrestDataList
                })
                .ToList();
        }

        public List<CDataEquipItemInfo> getEquipmentAsCDataEquipItemInfo(JobId job, EquipType equipType)
        {
            return getEquipment(job, equipType)
                .Select((x, index) => new {item = x, slot = (byte)(index+1)})
                .Select(tuple => new CDataEquipItemInfo()
                {
                    ItemId = tuple.item?.ItemId ?? 0,
                    Unk0 = tuple.item?.Unk3 ?? 0,
                    EquipType = equipType,
                    EquipSlot = tuple.slot,
                    Color = tuple.item?.Color ?? 0,
                    PlusValue = tuple.item?.PlusValue ?? 0,
                    WeaponCrestDataList = tuple.item?.WeaponCrestDataList ?? new List<CDataWeaponCrestData>(),
                    ArmorCrestDataList = tuple.item?.ArmorCrestDataList ?? new List<CDataArmorCrestData>(),
                    EquipElementParamList = tuple.item?.EquipElementParamList ?? new List<CDataEquipElementParam>()
                })
                .ToList();
        }

        public List<CDataCharacterEquipInfo> getEquipmentAsCDataCharacterEquipInfo(JobId job, EquipType equipType)
        {
            return getEquipment(job, equipType)
                .Select((x, index) => new {item = x, slot = (byte)(index+1)})
                .Where(tuple => tuple.item != null)
                .Select(tuple => new CDataCharacterEquipInfo()
                {
                    EquipItemUId = tuple.item.UId,
                    EquipType = (byte) equipType,
                    EquipCategory = tuple.slot
                })
                .ToList();
        }
    }

    public enum EquipType : byte {
        Performance = 1,
        Visual = 2
    }
}