using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.Database.Sql.Core
{
    public abstract partial class DdonSqlDb<TCon, TCom> : SqlDb<TCon, TCom>
        where TCon : DbConnection
        where TCom : DbCommand
    {
        private static readonly string[] CharacterCommonFields = new string[]
        {
            "job", "hide_equip_head", "hide_equip_lantern", "jewelry_slot_num"
        };

        private static readonly string[] CDataEditInfoFields = new string[]
        {
            "character_common_id", "sex", "voice", "voice_pitch", "personality", "speech_freq", "body_type", "hair", "beard", "makeup", "scar",
            "eye_preset_no", "nose_preset_no", "mouth_preset_no", "eyebrow_tex_no", "color_skin", "color_hair",
            "color_beard", "color_eyebrow", "color_r_eye", "color_l_eye", "color_makeup", "sokutobu", "hitai",
            "mimi_jyouge", "kannkaku", "mabisasi_jyouge", "hanakuchi_jyouge", "ago_saki_haba", "ago_zengo",
            "ago_saki_jyouge", "hitomi_ookisa", "me_ookisa", "me_kaiten", "mayu_kaiten", "mimi_ookisa", "mimi_muki",
            "elf_mimi", "miken_takasa", "miken_haba", "hohobone_ryou", "hohobone_jyouge", "hohoniku", "erahone_jyouge",
            "erahone_haba", "hana_jyouge", "hana_haba", "hana_takasa", "hana_kakudo", "kuchi_haba", "kuchi_atsusa",
            "eyebrow_uv_offset_x", "eyebrow_uv_offset_y", "wrinkle", "wrinkle_albedo_blend_rate",
            "wrinkle_detail_normal_power", "muscle_albedo_blend_rate", "muscle_detail_normal_power", "height",
            "head_size", "neck_offset", "neck_scale", "upper_body_scale_x", "belly_size", "teat_scale", "tekubi_size",
            "koshi_offset", "koshi_size", "ankle_offset", "fat", "muscle", "motion_filter"
        };

        // Im not convinced most of these fields have to be stored in DB
        private static readonly string[] CDataStatusInfoFields = new string[]
        {
            "character_common_id", "hp", "stamina", "revive_point", "max_hp", "max_stamina", "white_hp", "gain_hp", "gain_stamina",
            "gain_attack", "gain_defense", "gain_magic_attack", "gain_magic_defense"
        };


        private readonly string SqlInsertCharacterCommon = $"INSERT INTO `ddon_character_common` ({BuildQueryField(CharacterCommonFields)}) VALUES ({BuildQueryInsert(CharacterCommonFields)});";
        private readonly string SqlUpdateCharacterCommon = $"UPDATE `ddon_character_common` SET {BuildQueryUpdate(CharacterCommonFields)} WHERE `character_common_id` = @character_common_id;";

        private readonly string SqlInsertEditInfo = $"INSERT INTO `ddon_edit_info` ({BuildQueryField(CDataEditInfoFields)}) VALUES ({BuildQueryInsert(CDataEditInfoFields)});";
        private static readonly string SqlUpdateEditInfo = $"UPDATE `ddon_edit_info` SET {BuildQueryUpdate(CDataEditInfoFields)} WHERE `character_common_id` = @character_common_id;";
        private static readonly string SqlSelectEditInfo = $"SELECT {BuildQueryField(CDataEditInfoFields)} FROM `ddon_edit_info` WHERE `character_common_id` = @character_common_id;";
        private const string SqlDeleteEditInfo = "DELETE FROM `ddon_edit_info` WHERE `character_common_id`=@character_common_id;";

        private readonly string SqlInsertStatusInfo = $"INSERT INTO `ddon_status_info` ({BuildQueryField(CDataStatusInfoFields)}) VALUES ({BuildQueryInsert(CDataStatusInfoFields)});";
        private static readonly string SqlUpdateStatusInfo = $"UPDATE `ddon_status_info` SET {BuildQueryUpdate(CDataStatusInfoFields)} WHERE `character_common_id` = @character_common_id;";
        private static readonly string SqlSelectStatusInfo = $"SELECT {BuildQueryField(CDataStatusInfoFields)} FROM `ddon_status_info` WHERE `character_common_id` = @character_common_id;";
        private const string SqlDeleteStatusInfo = "DELETE FROM `ddon_status_info` WHERE `character_common_id`=@character_common_id;";

        public bool UpdateCharacterCommonBaseInfo(CharacterCommon common)
        {
            return UpdateCharacterCommonBaseInfo(null, common);
        }

        public bool UpdateCharacterCommonBaseInfo(TCon conn, CharacterCommon common)
        {
            int commonUpdateRowsAffected = ExecuteNonQuery(conn, SqlUpdateCharacterCommon, command =>
            {
                AddParameter(command, "@character_common_id", common.CommonId);
                AddParameter(command, common);
            });

            return commonUpdateRowsAffected > NoRowsAffected;
        }

        public bool UpdateEditInfo(CharacterCommon common)
        {
            return UpdateEditInfo(null, common);
        }

        public bool UpdateEditInfo(TCon conn, CharacterCommon common)
        {
            int commonUpdateRowsAffected = ExecuteNonQuery(conn, SqlUpdateEditInfo, command =>
            {
                AddParameter(command, "@character_common_id", common.CommonId);
                AddParameter(command, common);
            });

            return commonUpdateRowsAffected > NoRowsAffected;
        }

        public bool UpdateStatusInfo(CharacterCommon common)
        {
            return UpdateStatusInfo(null, common);
        }

        public bool UpdateStatusInfo(TCon conn, CharacterCommon common)
        {
            int commonUpdateRowsAffected = ExecuteNonQuery(conn, SqlUpdateStatusInfo, command =>
            {
                AddParameter(command, "@character_common_id", common.CommonId);
                AddParameter(command, common);
            });

            return commonUpdateRowsAffected > NoRowsAffected;
        }


        private void QueryCharacterCommonData(TCon conn, CharacterCommon common)
        {
            // Job data
            ExecuteReader(conn, SqlSelectCharacterJobDataByCharacter,
                command => { AddParameter(command, "@character_common_id", common.CommonId); }, reader =>
                {
                    while (reader.Read())
                    {
                        common.CharacterJobDataList.Add(ReadCharacterJobData(reader));
                    }
                });

            // Equips
            ExecuteReader(conn, SqlSelectEquipItemByCharacter,
                command => { AddParameter(command, "@character_common_id", common.CommonId); }, 
                reader =>
                {
                    while (reader.Read())
                    {
                        string UId = GetString(reader, "item_uid");
                        JobId job = (JobId) GetByte(reader, "job");
                        EquipType equipType = (EquipType) GetByte(reader, "equip_type");
                        byte equipSlot = GetByte(reader, "equip_slot");
                        ExecuteReader(conn, SqlSelectItem,
                            command2 => { AddParameter(command2, "@uid", UId); },
                            reader2 => 
                            {
                                if(reader2.Read())
                                {
                                    Item item = ReadItem(reader2);
                                    common.Equipment.setEquipItem(item, job, equipType, equipSlot);
                                }
                            });
                    }
                });            

            // Job Items
            ExecuteReader(conn, SqlSelectEquipJobItemByCharacter,
                command => { AddParameter(command, "@character_common_id", common.CommonId); }, 
                reader =>
                {
                    while (reader.Read())
                    {
                        JobId job = (JobId) GetByte(reader, "job");
                        CDataEquipJobItem equipJobItem = ReadEquipJobItem(reader);
                        if(!common.CharacterEquipJobItemListDictionary.ContainsKey(job))
                        {
                            common.CharacterEquipJobItemListDictionary.Add(job, new List<CDataEquipJobItem>());
                        }
                        common.CharacterEquipJobItemListDictionary[job].Add(equipJobItem);
                    }
                });

            // Normal Skills
            ExecuteReader(conn, SqlSelectNormalSkillParam,
                command => { AddParameter(command, "@character_common_id", common.CommonId); },
                reader =>
                {
                    while (reader.Read())
                    {
                        common.NormalSkills.Add(ReadNormalSkillParam(reader));
                    }
                });

            // Custom Skills
            ExecuteReader(conn, SqlSelectEquippedCustomSkills,
                command => { AddParameter(command, "@character_common_id", common.CommonId); },
                reader =>
                {
                    while (reader.Read())
                    {
                        common.CustomSkills.Add(ReadCustomSkill(reader));
                    }
                });

            // Abilities
            ExecuteReader(conn, SqlSelectEquippedAbilities,
                command => { AddParameter(command, "@character_common_id", common.CommonId); },
                reader =>
                {
                    while (reader.Read())
                    {
                        common.Abilities.Add(ReadAbility(reader));
                    }
                });
        }

        private void StoreCharacterCommonData(TCon conn, CharacterCommon common)
        {
            foreach(CDataCharacterJobData characterJobData in common.CharacterJobDataList)
            {
                ExecuteNonQuery(conn, SqlReplaceCharacterJobData, command =>
                {
                    AddParameter(command, common.CommonId, characterJobData);
                });
            }

            foreach(KeyValuePair<JobId, List<CDataEquipJobItem>> characterEquipJobItemListByJob in common.CharacterEquipJobItemListDictionary)
            {
                foreach(CDataEquipJobItem equipJobItem in characterEquipJobItemListByJob.Value)
                {
                    ExecuteNonQuery(conn, SqlReplaceEquipJobItem, command =>
                    {
                        AddParameter(command, common.CommonId, characterEquipJobItemListByJob.Key, equipJobItem);
                    });
                }
            }

            foreach(CDataNormalSkillParam normalSkillParam in common.NormalSkills)
            {
                ExecuteNonQuery(conn, SqlReplaceNormalSkillParam, command =>
                {
                    AddParameter(command, common.CommonId, normalSkillParam);
                });
            }

            foreach(CustomSkill skill in common.CustomSkills)
            {
                ExecuteNonQuery(conn, SqlReplaceEquippedCustomSkill, command =>
                {
                    AddParameter(command, common.CommonId, skill);
                });
            }

            foreach(Ability ability in common.Abilities)
            {
                ExecuteNonQuery(conn, SqlReplaceEquippedAbility, command =>
                {
                    AddParameter(command, common.CommonId, ability);
                });
            }
        }

        private void ReadAllCharacterCommonData(DbDataReader reader, CharacterCommon common)
        {
            common.CommonId = GetUInt32(reader, "character_common_id");
            common.Job = (JobId) GetByte(reader, "job");
            common.JewelrySlotNum = GetByte(reader, "jewelry_slot_num");
            common.HideEquipHead = GetBoolean(reader, "hide_equip_head");
            common.HideEquipLantern = GetBoolean(reader, "hide_equip_lantern");

            common.EditInfo.Sex = GetByte(reader, "sex");
            common.EditInfo.Voice = GetByte(reader, "voice");
            common.EditInfo.VoicePitch = GetUInt16(reader, "voice_pitch");
            common.EditInfo.Personality = GetByte(reader, "personality");
            common.EditInfo.SpeechFreq = GetByte(reader, "speech_freq");
            common.EditInfo.BodyType = GetByte(reader, "body_type");
            common.EditInfo.Hair = GetByte(reader, "hair");
            common.EditInfo.Beard = GetByte(reader, "beard");
            common.EditInfo.Makeup = GetByte(reader, "makeup");
            common.EditInfo.Scar = GetByte(reader, "scar");
            common.EditInfo.EyePresetNo = GetByte(reader, "eye_preset_no");
            common.EditInfo.NosePresetNo = GetByte(reader, "nose_preset_no");
            common.EditInfo.MouthPresetNo = GetByte(reader, "mouth_preset_no");
            common.EditInfo.EyebrowTexNo = GetByte(reader, "eyebrow_tex_no");
            common.EditInfo.ColorSkin = GetByte(reader, "color_skin");
            common.EditInfo.ColorHair = GetByte(reader, "color_hair");
            common.EditInfo.ColorBeard = GetByte(reader, "color_beard");
            common.EditInfo.ColorEyebrow = GetByte(reader, "color_eyebrow");
            common.EditInfo.ColorREye = GetByte(reader, "color_r_eye");
            common.EditInfo.ColorLEye = GetByte(reader, "color_l_eye");
            common.EditInfo.ColorMakeup = GetByte(reader, "color_makeup");
            common.EditInfo.Sokutobu = GetUInt16(reader, "sokutobu");
            common.EditInfo.Hitai = GetUInt16(reader, "hitai");
            common.EditInfo.MimiJyouge = GetUInt16(reader, "mimi_jyouge");
            common.EditInfo.Kannkaku = GetUInt16(reader, "kannkaku");
            common.EditInfo.MabisasiJyouge = GetUInt16(reader, "mabisasi_jyouge");
            common.EditInfo.HanakuchiJyouge = GetUInt16(reader, "hanakuchi_jyouge");
            common.EditInfo.AgoSakiHaba = GetUInt16(reader, "ago_saki_haba");
            common.EditInfo.AgoZengo = GetUInt16(reader, "ago_zengo");
            common.EditInfo.AgoSakiJyouge = GetUInt16(reader, "ago_saki_jyouge");
            common.EditInfo.HitomiOokisa = GetUInt16(reader, "hitomi_ookisa");
            common.EditInfo.MeOokisa = GetUInt16(reader, "me_ookisa");
            common.EditInfo.MeKaiten = GetUInt16(reader, "me_kaiten");
            common.EditInfo.MayuKaiten = GetUInt16(reader, "mayu_kaiten");
            common.EditInfo.MimiOokisa = GetUInt16(reader, "mimi_ookisa");
            common.EditInfo.MimiMuki = GetUInt16(reader, "mimi_muki");
            common.EditInfo.ElfMimi = GetUInt16(reader, "elf_mimi");
            common.EditInfo.MikenTakasa = GetUInt16(reader, "miken_takasa");
            common.EditInfo.MikenHaba = GetUInt16(reader, "miken_haba");
            common.EditInfo.HohoboneRyou = GetUInt16(reader, "hohobone_ryou");
            common.EditInfo.HohoboneJyouge = GetUInt16(reader, "hohobone_jyouge");
            common.EditInfo.Hohoniku = GetUInt16(reader, "hohoniku");
            common.EditInfo.ErahoneJyouge = GetUInt16(reader, "erahone_jyouge");
            common.EditInfo.ErahoneHaba = GetUInt16(reader, "erahone_haba");
            common.EditInfo.HanaJyouge = GetUInt16(reader, "hana_jyouge");
            common.EditInfo.HanaHaba = GetUInt16(reader, "hana_haba");
            common.EditInfo.HanaTakasa = GetUInt16(reader, "hana_takasa");
            common.EditInfo.HanaKakudo = GetUInt16(reader, "hana_kakudo");
            common.EditInfo.KuchiHaba = GetUInt16(reader, "kuchi_haba");
            common.EditInfo.KuchiAtsusa = GetUInt16(reader, "kuchi_atsusa");
            common.EditInfo.EyebrowUVOffsetX = GetUInt16(reader, "eyebrow_uv_offset_x");
            common.EditInfo.EyebrowUVOffsetY = GetUInt16(reader, "eyebrow_uv_offset_y");
            common.EditInfo.Wrinkle = GetUInt16(reader, "wrinkle");
            common.EditInfo.WrinkleAlbedoBlendRate = GetUInt16(reader, "wrinkle_albedo_blend_rate");
            common.EditInfo.WrinkleDetailNormalPower = GetUInt16(reader, "wrinkle_detail_normal_power");
            common.EditInfo.MuscleAlbedoBlendRate = GetUInt16(reader, "muscle_albedo_blend_rate");
            common.EditInfo.MuscleDetailNormalPower = GetUInt16(reader, "muscle_detail_normal_power");
            common.EditInfo.Height = GetUInt16(reader, "height");
            common.EditInfo.HeadSize = GetUInt16(reader, "head_size");
            common.EditInfo.NeckOffset = GetUInt16(reader, "neck_offset");
            common.EditInfo.NeckScale = GetUInt16(reader, "neck_scale");
            common.EditInfo.UpperBodyScaleX = GetUInt16(reader, "upper_body_scale_x");
            common.EditInfo.BellySize = GetUInt16(reader, "belly_size");
            common.EditInfo.TeatScale = GetUInt16(reader, "teat_scale");
            common.EditInfo.TekubiSize = GetUInt16(reader, "tekubi_size");
            common.EditInfo.KoshiOffset = GetUInt16(reader, "koshi_offset");
            common.EditInfo.KoshiSize = GetUInt16(reader, "koshi_size");
            common.EditInfo.AnkleOffset = GetUInt16(reader, "ankle_offset");
            common.EditInfo.Fat = GetUInt16(reader, "fat");
            common.EditInfo.Muscle = GetUInt16(reader, "muscle");
            common.EditInfo.MotionFilter = GetUInt16(reader, "motion_filter");

            common.StatusInfo.HP = GetUInt32(reader, "hp");
            common.StatusInfo.Stamina = GetUInt32(reader, "stamina");
            common.StatusInfo.RevivePoint = GetByte(reader, "revive_point");
            common.StatusInfo.MaxHP = GetUInt32(reader, "max_hp");
            common.StatusInfo.MaxStamina = GetUInt32(reader, "max_stamina");
            common.StatusInfo.WhiteHP = GetUInt32(reader, "white_hp");
            common.StatusInfo.GainHP = GetUInt32(reader, "gain_hp");
            common.StatusInfo.GainStamina = GetUInt32(reader, "gain_stamina");
            common.StatusInfo.GainAttack = GetUInt32(reader, "gain_attack");
            common.StatusInfo.GainDefense = GetUInt32(reader, "gain_defense");
            common.StatusInfo.GainMagicAttack = GetUInt32(reader, "gain_magic_attack");
            common.StatusInfo.GainMagicDefense = GetUInt32(reader, "gain_magic_defense");
        }

        private void AddParameter(TCom command, CharacterCommon common)
        {
            // CharacterCommonFields
            AddParameter(command, "@character_common_id", common.CommonId);
            AddParameter(command, "@job", (byte) common.Job);
            AddParameter(command, "@jewelry_slot_num", common.JewelrySlotNum);
            AddParameter(command, "@hide_equip_head", common.HideEquipHead);
            AddParameter(command, "@hide_equip_lantern", common.HideEquipLantern);
            // CDataEditInfoFields
            AddParameter(command, "@sex", common.EditInfo.Sex);
            AddParameter(command, "@voice", common.EditInfo.Voice);
            AddParameter(command, "@voice_pitch", common.EditInfo.VoicePitch);
            AddParameter(command, "@personality", common.EditInfo.Personality);
            AddParameter(command, "@speech_freq", common.EditInfo.SpeechFreq);
            AddParameter(command, "@body_type", common.EditInfo.BodyType);
            AddParameter(command, "@hair", common.EditInfo.Hair);
            AddParameter(command, "@beard", common.EditInfo.Beard);
            AddParameter(command, "@makeup", common.EditInfo.Makeup);
            AddParameter(command, "@scar", common.EditInfo.Scar);
            AddParameter(command, "@eye_preset_no", common.EditInfo.EyePresetNo);
            AddParameter(command, "@nose_preset_no", common.EditInfo.NosePresetNo);
            AddParameter(command, "@mouth_preset_no", common.EditInfo.MouthPresetNo);
            AddParameter(command, "@eyebrow_tex_no", common.EditInfo.EyebrowTexNo);
            AddParameter(command, "@color_skin", common.EditInfo.ColorSkin);
            AddParameter(command, "@color_hair", common.EditInfo.ColorHair);
            AddParameter(command, "@color_beard", common.EditInfo.ColorBeard);
            AddParameter(command, "@color_eyebrow", common.EditInfo.ColorEyebrow);
            AddParameter(command, "@color_r_eye", common.EditInfo.ColorREye);
            AddParameter(command, "@color_l_eye", common.EditInfo.ColorLEye);
            AddParameter(command, "@color_makeup", common.EditInfo.ColorMakeup);
            AddParameter(command, "@sokutobu", common.EditInfo.Sokutobu);
            AddParameter(command, "@hitai", common.EditInfo.Hitai);
            AddParameter(command, "@mimi_jyouge", common.EditInfo.MimiJyouge);
            AddParameter(command, "@kannkaku", common.EditInfo.Kannkaku);
            AddParameter(command, "@mabisasi_jyouge", common.EditInfo.MabisasiJyouge);
            AddParameter(command, "@hanakuchi_jyouge", common.EditInfo.HanakuchiJyouge);
            AddParameter(command, "@ago_saki_haba", common.EditInfo.AgoSakiHaba);
            AddParameter(command, "@ago_zengo", common.EditInfo.AgoZengo);
            AddParameter(command, "@ago_saki_jyouge", common.EditInfo.AgoSakiJyouge);
            AddParameter(command, "@hitomi_ookisa", common.EditInfo.HitomiOokisa);
            AddParameter(command, "@me_ookisa", common.EditInfo.MeOokisa);
            AddParameter(command, "@me_kaiten", common.EditInfo.MeKaiten);
            AddParameter(command, "@mayu_kaiten", common.EditInfo.MayuKaiten);
            AddParameter(command, "@mimi_ookisa", common.EditInfo.MimiOokisa);
            AddParameter(command, "@mimi_muki", common.EditInfo.MimiMuki);
            AddParameter(command, "@elf_mimi", common.EditInfo.ElfMimi);
            AddParameter(command, "@miken_takasa", common.EditInfo.MikenTakasa);
            AddParameter(command, "@miken_haba", common.EditInfo.MikenHaba);
            AddParameter(command, "@hohobone_ryou", common.EditInfo.HohoboneRyou);
            AddParameter(command, "@hohobone_jyouge", common.EditInfo.HohoboneJyouge);
            AddParameter(command, "@hohoniku", common.EditInfo.Hohoniku);
            AddParameter(command, "@erahone_jyouge", common.EditInfo.ErahoneJyouge);
            AddParameter(command, "@erahone_haba", common.EditInfo.ErahoneHaba);
            AddParameter(command, "@hana_jyouge", common.EditInfo.HanaJyouge);
            AddParameter(command, "@hana_haba", common.EditInfo.HanaHaba);
            AddParameter(command, "@hana_takasa", common.EditInfo.HanaTakasa);
            AddParameter(command, "@hana_kakudo", common.EditInfo.HanaKakudo);
            AddParameter(command, "@kuchi_haba", common.EditInfo.KuchiHaba);
            AddParameter(command, "@kuchi_atsusa", common.EditInfo.KuchiAtsusa);
            AddParameter(command, "@eyebrow_uv_offset_x", common.EditInfo.EyebrowUVOffsetX);
            AddParameter(command, "@eyebrow_uv_offset_y", common.EditInfo.EyebrowUVOffsetY);
            AddParameter(command, "@wrinkle", common.EditInfo.Wrinkle);
            AddParameter(command, "@wrinkle_albedo_blend_rate", common.EditInfo.WrinkleAlbedoBlendRate);
            AddParameter(command, "@wrinkle_detail_normal_power", common.EditInfo.WrinkleDetailNormalPower);
            AddParameter(command, "@muscle_albedo_blend_rate", common.EditInfo.MuscleAlbedoBlendRate);
            AddParameter(command, "@muscle_detail_normal_power", common.EditInfo.MuscleDetailNormalPower);
            AddParameter(command, "@height", common.EditInfo.Height);
            AddParameter(command, "@head_size", common.EditInfo.HeadSize);
            AddParameter(command, "@neck_offset", common.EditInfo.NeckOffset);
            AddParameter(command, "@neck_scale", common.EditInfo.NeckScale);
            AddParameter(command, "@upper_body_scale_x", common.EditInfo.UpperBodyScaleX);
            AddParameter(command, "@belly_size", common.EditInfo.BellySize);
            AddParameter(command, "@teat_scale", common.EditInfo.TeatScale);
            AddParameter(command, "@tekubi_size", common.EditInfo.TekubiSize);
            AddParameter(command, "@koshi_offset", common.EditInfo.KoshiOffset);
            AddParameter(command, "@koshi_size", common.EditInfo.KoshiSize);
            AddParameter(command, "@ankle_offset", common.EditInfo.AnkleOffset);
            AddParameter(command, "@fat", common.EditInfo.Fat);
            AddParameter(command, "@muscle", common.EditInfo.Muscle);
            AddParameter(command, "@motion_filter", common.EditInfo.MotionFilter);
            // CDataStatusInfoFields
            AddParameter(command, "@hp", common.StatusInfo.HP);
            AddParameter(command, "@stamina", common.StatusInfo.Stamina);
            AddParameter(command, "@revive_point", common.StatusInfo.RevivePoint);
            AddParameter(command, "@max_hp", common.StatusInfo.MaxHP);
            AddParameter(command, "@max_stamina", common.StatusInfo.MaxStamina);
            AddParameter(command, "@white_hp", common.StatusInfo.WhiteHP);
            AddParameter(command, "@gain_hp", common.StatusInfo.GainHP);
            AddParameter(command, "@gain_stamina", common.StatusInfo.GainStamina);
            AddParameter(command, "@gain_attack", common.StatusInfo.GainAttack);
            AddParameter(command, "@gain_defense", common.StatusInfo.GainDefense);
            AddParameter(command, "@gain_magic_attack", common.StatusInfo.GainMagicAttack);
            AddParameter(command, "@gain_magic_defense", common.StatusInfo.GainMagicDefense);
        }
    }
}