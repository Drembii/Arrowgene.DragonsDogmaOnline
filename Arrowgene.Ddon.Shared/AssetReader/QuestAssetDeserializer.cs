using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using Arrowgene.Ddon.Shared.Asset;
using Arrowgene.Ddon.Shared.Entity.Structure;
using System.Text.Json.Serialization;
using System;
using System.Collections;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using static Arrowgene.Ddon.Shared.Csv.GmdCsv;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Arrowgene.Ddon.Shared.Model.Quest;

namespace Arrowgene.Ddon.Shared.Csv
{
    public class QuestAssetDeserializer : IAssetDeserializer<QuestAsset>
    {
        private static readonly ILogger Logger = LogProvider.Logger(typeof(QuestAssetDeserializer));

        public QuestAsset ReadPath(string path)
        {
            Logger.Info($"Reading {path}");

            var questAssets = new QuestAsset();

            string json = File.ReadAllText(path);
            JsonDocument document = JsonDocument.Parse(json);

            if (!Enum.TryParse(document.RootElement.GetProperty("state_machine").GetString(), true, out QuestStateMachineType questStateMachineType))
            {
                Logger.Error($"Expected key 'state_machine' not in the root of the document. Unable to parse {path}.");
                return questAssets;
            }

            if (questStateMachineType != QuestStateMachineType.GenericStateMachine)
            {
                Logger.Error($"Unsupported QuestStateMachineType '{questStateMachineType}'. Unable to parse {path}.");
                return questAssets;
            }

            foreach (var jQuest in document.RootElement.GetProperty("quests").EnumerateArray())
            {
                QuestAssetData assetData = new QuestAssetData();

                if (!ParseQuest(assetData, jQuest))
                {
                    continue;
                }

                questAssets.Quests.Add(assetData);
            }

            return questAssets;
        }

        private bool ParseQuest(QuestAssetData assetData, JsonElement jQuest)
        {
            if (!Enum.TryParse(jQuest.GetProperty("type").GetString(), true, out QuestType questType))
            {
                Logger.Error($"Unable to parse the quest type. Skipping.");
                return false;
            }

            assetData.Type = questType;
            assetData.QuestId = (QuestId)jQuest.GetProperty("quest_id").GetUInt32();
            assetData.BaseLevel = jQuest.GetProperty("base_level").GetUInt16();
            assetData.MinimumItemRank = jQuest.GetProperty("minimum_item_rank").GetByte();
            assetData.Discoverable = jQuest.GetProperty("discoverable").GetBoolean();

            assetData.NextQuestId = 0;
            if (jQuest.TryGetProperty("next_quest", out JsonElement jNextQuest))
            {
                assetData.NextQuestId = (QuestId)jNextQuest.GetUInt32();
            }

            if (jQuest.TryGetProperty("quest_layout_set_info_flags", out JsonElement jLayoutSetInfoFlags))
            {
                foreach (var layoutFlag in jLayoutSetInfoFlags.EnumerateArray())
                {
                    assetData.QuestLayoutSetInfoFlags.Add(QuestLayoutFlagSetInfo.FromQuestAssetJson(layoutFlag));
                }
            }

            if (jQuest.TryGetProperty("quest_layout_flags", out JsonElement jLayoutFlags))
            {
                foreach (var layoutFlag in jLayoutFlags.EnumerateArray())
                {
                    assetData.QuestLayoutFlags.Add(new QuestLayoutFlag() { FlagId = layoutFlag.GetUInt32() });
                }
            }

            ParseRewards(assetData, jQuest);

            if (!ParseEnemyGroups(assetData, jQuest))
            {
                Logger.Error($"Unable to create the quest '{assetData.QuestId}'. Skipping.");
                return false;
            }

            if (jQuest.TryGetProperty("blocks", out JsonElement jBlocksV1))
            {
                QuestProcess questProcess = new QuestProcess();
                if (!ParseBlocks(questProcess, jBlocksV1))
                {
                    Logger.Error($"Unable to create the quest '{assetData.QuestId}'. Skipping.");
                    return false;
                }
                assetData.Processes.Add(questProcess);
            }
            else if (jQuest.TryGetProperty("processes", out JsonElement jProcesses))
            {
                ushort ProcessNo = 0;
                foreach (var jProcess in jProcesses.EnumerateArray())
                {
                    QuestProcess questProcess = new QuestProcess()
                    {
                        ProcessNo = ProcessNo
                    };

                    var jBlocks = jProcess.GetProperty("blocks");
                    if (!ParseBlocks(questProcess, jBlocks))
                    {
                        Logger.Error($"Unable to create the quest '{assetData.QuestId}'. Skipping.");
                        return false;
                    }
                    assetData.Processes.Add(questProcess);
                    ProcessNo += 1;
                }
            }

            return true;
        }

        private void ParseRewards(QuestAssetData assetData, JsonElement quest)
        {
            foreach (var reward in quest.GetProperty("rewards").EnumerateArray())
            {
                var rewardType = reward.GetProperty("type").GetString();
                switch (rewardType)
                {
                    case "fixed":
                    case "random":
                    case "select":
                        if (!Enum.TryParse(reward.GetProperty("type").GetString(), true, out QuestRewardType questRewardType))
                        {
                            continue;
                        }

                        QuestRewardItem rewardItem = null;
                        if (questRewardType == QuestRewardType.Random)
                        {
                            rewardItem = new QuestRandomRewardItem();
                            foreach (var item in reward.GetProperty("loot_pool").EnumerateArray())
                            {
                                rewardItem.LootPool.Add(new RandomLootPoolItem()
                                {
                                    ItemId = item.GetProperty("item_id").GetUInt32(),
                                    Num = item.GetProperty("num").GetUInt16(),
                                    Chance = item.GetProperty("chance").GetDouble()
                                });
                            }
                        }
                        else if (questRewardType == QuestRewardType.Select)
                        {
                            rewardItem = new QuestSelectRewardItem();
                            foreach (var item in reward.GetProperty("loot_pool").EnumerateArray())
                            {
                                rewardItem.LootPool.Add(new SelectLootPoolItem()
                                {
                                    ItemId = item.GetProperty("item_id").GetUInt32(),
                                    Num = item.GetProperty("num").GetUInt16(),
                                });
                            }
                        }
                        else if (questRewardType == QuestRewardType.Fixed)
                        {
                            var item = reward.GetProperty("loot_pool").EnumerateArray().ToList()[0];
                            rewardItem = new QuestFixedRewardItem()
                            {
                                LootPool = new List<LootPoolItem>()
                                        {
                                            new FixedLootPoolItem()
                                            {
                                                ItemId = item.GetProperty("item_id").GetUInt32(),
                                                Num = item.GetProperty("num").GetUInt16(),
                                            }
                                        }
                            };
                        }
                        else
                        {
                            Logger.Error($"The reward type '{rewardType}' is not implemented. Skipping.");
                        }

                        if (rewardItem != null)
                        {
                            assetData.RewardItems.Add(rewardItem);
                        }
                        break;
                    case "exp":
                        assetData.ExpType = ExpType.ExperiencePoints;
                        assetData.ExpReward = reward.GetProperty("amount").GetUInt32();
                        break;
                    case "pp":
                        assetData.ExpType = ExpType.PlayPoints;
                        assetData.ExpReward = reward.GetProperty("amount").GetUInt32();
                        break;
                    case "wallet":
                        if (!Enum.TryParse(reward.GetProperty("wallet_type").GetString(), true, out WalletType walletType))
                        {
                            continue;
                        }
                        assetData.RewardCurrency.Add(new QuestRewardCurrency()
                        {
                            WalletType = walletType,
                            Amount = reward.GetProperty("amount").GetUInt32()
                        });
                        break;
                    default:
                        /* NOT IMPLEMENTED */
                        break;
                }
            }
        }

        private bool ParseBlocks(QuestProcess questProcess, JsonElement jBlocks)
        {
            ushort blockIndex = 1;
            foreach (var jblock in jBlocks.EnumerateArray())
            {
                QuestBlock questBlock = new QuestBlock();

                if (!Enum.TryParse(jblock.GetProperty("type").GetString(), true, out QuestBlockType questBlockType))
                {
                    Logger.Error($"Unable to parse the quest block type @ index {blockIndex - 1}.");
                    return false;
                }

                questBlock.ProcessNo = questProcess.ProcessNo;
                questBlock.BlockType = questBlockType;
                questBlock.BlockNo = blockIndex;
                questBlock.AnnounceType = QuestAnnounceType.None;

                if (jblock.TryGetProperty("announce_type", out JsonElement jUpdateAnnounce))
                {
                    if (!Enum.TryParse(jUpdateAnnounce.GetString(), true, out QuestAnnounceType announceType))
                    {
                        Logger.Error($"Unable to parse the quest announce type @ index {blockIndex - 1}.");
                        return false;
                    }
                    questBlock.AnnounceType = announceType;
                }
                
                if (jblock.TryGetProperty("stage_id", out JsonElement jStageId))
                {
                    questBlock.StageId = ParseStageId(jStageId);
                }

                questBlock.SubGroupId = 0;
                if (jblock.TryGetProperty("subgroup_no", out JsonElement jSubGroupNo))
                {
                    questBlock.SubGroupId = jSubGroupNo.GetUInt16();
                }

                if (jblock.TryGetProperty("layout_flags_on", out JsonElement jLayoutFlagsOn))
                {
                    foreach (var jLayoutFlag in jLayoutFlagsOn.EnumerateArray())
                    {
                        questBlock.QuestLayoutFlagsOn.Add(jLayoutFlag.GetUInt32());
                    }
                }

                if (jblock.TryGetProperty("layout_flags_off", out JsonElement jLayoutFlagsOff))
                {
                    foreach (var jLayoutFlag in jLayoutFlagsOff.EnumerateArray())
                    {
                        questBlock.QuestLayoutFlagsOff.Add(jLayoutFlag.GetUInt32());
                    }
                }

                switch (questBlockType)
                {
                    case QuestBlockType.IsStageNo:
                        break;
                    case QuestBlockType.NpcTalkAndOrder:
                        {
                            if (!Enum.TryParse(jblock.GetProperty("npc_id").GetString(), true, out NpcId npcId))
                            {
                                Logger.Error($"Unable to parse the npc_id in block @ index {blockIndex - 1}.");
                                return false;
                            }
                            questBlock.NpcOrderDetails.Add(new QuestNpcOrder()
                            {
                                NpcId = npcId,
                                MsgId = jblock.GetProperty("message_id").GetInt32(),
                                StageId = ParseStageId(jblock.GetProperty("stage_id"))
                            });
                        }
                        break;
                    case QuestBlockType.DiscoverEnemy:
                    case QuestBlockType.SeekOutEnemiesAtMarkedLocation:
                    case QuestBlockType.KillGroup:
                        {
                            questBlock.ResetGroup = true;
                            if (jblock.TryGetProperty("reset_group", out JsonElement jResetGroup))
                            {
                                questBlock.ResetGroup = jResetGroup.GetBoolean();
                            }

                            foreach (var groupId in jblock.GetProperty("groups").EnumerateArray())
                            {
                                questBlock.EnemyGroupIds.Add(groupId.GetUInt32());
                            }
                        }
                        break;
                    case QuestBlockType.TalkToNpc:
                        {
                            if (!Enum.TryParse(jblock.GetProperty("npc_id").GetString(), true, out NpcId npcId))
                            {
                                Logger.Error($"Unable to parse the npc_id in block @ index {blockIndex - 1}.");
                                return false;
                            }
                            questBlock.NpcOrderDetails.Add(new QuestNpcOrder()
                            {
                                NpcId = npcId,
                                MsgId = jblock.GetProperty("message_id").GetInt32(),
                                StageId = ParseStageId(jblock.GetProperty("stage_id"))
                            });
                        }
                        break;
                    case QuestBlockType.IsQuestOrdered:
                        {
                            if (!Enum.TryParse(jblock.GetProperty("quest_type").GetString(), true, out QuestType questType))
                            {
                                Logger.Error($"Unable to parse the quest type in block @ index {blockIndex - 1}.");
                                return false;
                            }

                            questBlock.QuestOrderDetails.QuestType = questType;
                            questBlock.QuestOrderDetails.QuestId = (QuestId) jblock.GetProperty("quest_id").GetUInt32();
                        }
                        break;
                    case QuestBlockType.MyQstFlags:
                        {
                            if (jblock.TryGetProperty("set_flags", out JsonElement jSetFlags))
                            {
                                foreach (var jMyQstFlag in jSetFlags.EnumerateArray())
                                {
                                    questBlock.MyQstSetFlags.Add(jMyQstFlag.GetUInt32());
                                }
                            }

                            if (jblock.TryGetProperty("check_flags", out JsonElement jCheckFlags))
                            {
                                foreach (var jMyQstFlag in jCheckFlags.EnumerateArray())
                                {
                                    questBlock.MyQstCheckFlags.Add(jMyQstFlag.GetUInt32());
                                }
                            }
                        }
                        break;
                    case QuestBlockType.CollectItem:
                        {
                            questBlock.ShowMarker = true;
                            if (jblock.TryGetProperty("show_marker", out JsonElement jShowMarker))
                            {
                                questBlock.ShowMarker = jShowMarker.GetBoolean();
                            }
                        }
                        break;
                    case QuestBlockType.DeliverItems:
                        {
                            if (!Enum.TryParse(jblock.GetProperty("npc_id").GetString(), true, out NpcId npcId))
                            {
                                Logger.Error($"Unable to parse the npc_id in block @ index {blockIndex - 1}.");
                                return false;
                            }

                            questBlock.NpcOrderDetails.Add(new QuestNpcOrder()
                            {
                                NpcId = npcId,
                                MsgId = jblock.GetProperty("message_id").GetInt32(),
                                StageId = ParseStageId(jblock.GetProperty("stage_id"))
                            });

                            foreach (var item in jblock.GetProperty("items").EnumerateArray())
                            {
                                questBlock.DeliveryRequests.Add(new QuestDeliveryItem()
                                {
                                    ItemId = item.GetProperty("id").GetUInt32(),
                                    Amount = item.GetProperty("amount").GetUInt32()
                                });
                            }
                        }
                        break;
                    case QuestBlockType.Raw:
                        if (!ParseRawBlock(jblock, questBlock))
                        {
                            Logger.Error($"Unable to parse RawBlock commands in block @ index {blockIndex - 1}.");
                            return false;
                        }
                        break;
                    case QuestBlockType.DummyBlock:
                        /* Filler block which might do some meta things like announce or set flags */
                        break;
                    default:
                        Logger.Error($"Unsupported QuestBlockType {questBlockType} @ index {blockIndex - 1}.");
                        return false;
                }

                questProcess.Blocks.Add(questBlock);

                blockIndex += 1;
            }

            if (questProcess.ProcessNo == 0)
            {
                // Add an implicit EndBlock
                questProcess.Blocks.Add(new QuestBlock()
                {
                    BlockType = QuestBlockType.End,
                    ProcessNo = questProcess.ProcessNo,
                    BlockNo = blockIndex,
                    SequenceNo = 1,
                });
            }
            else
            {
                // Add a block which does nothing
                questProcess.Blocks.Add(new QuestBlock()
                {
                    ProcessNo = questProcess.ProcessNo,
                    BlockType = QuestBlockType.None,
                    BlockNo = blockIndex,
                    SequenceNo = 1,
                });
            }

            return true;
        }

        private StageId ParseStageId(JsonElement jStageId)
        {
            uint id = jStageId.GetProperty("id").GetUInt32();

            byte layerNo = 0;
            if (jStageId.TryGetProperty("layer_no", out JsonElement jLayerNo))
            {
                layerNo = jLayerNo.GetByte();
            }
            
            uint groupId = jStageId.GetProperty("group_id").GetUInt32();
            return new StageId(id, layerNo, groupId);
        }

        private bool ParseEnemyGroups(QuestAssetData assetData, JsonElement quest)
        {
            if (!quest.TryGetProperty("enemy_groups", out JsonElement jGroups))
            {
                // No Enemy groups to parse
                return true;
            }

            uint groupId = 0;
            foreach (var jGroup in jGroups.EnumerateArray())
            {
                QuestEnemyGroup enemyGroup = new QuestEnemyGroup() { GroupId = groupId };

                if (!jGroup.TryGetProperty("stage_id", out JsonElement jStageId))
                {
                    Logger.Info("Required stage_id field for enemy group not found.");
                    return false;
                }

                enemyGroup.StageId = ParseStageId(jStageId);

                if (jGroup.TryGetProperty("starting_index", out JsonElement jStartingIndex))
                {
                    enemyGroup.StartingIndex = jStartingIndex.GetUInt32();
                }

                foreach (var enemy in jGroup.GetProperty("enemies").EnumerateArray())
                {
                    bool IsBoss = enemy.GetProperty("is_boss").GetBoolean();
                    var questEnemy = new Enemy()
                    {
                        EnemyId = Convert.ToUInt32(enemy.GetProperty("enemy_id").GetString(), 16),
                        Lv = enemy.GetProperty("level").GetUInt16(),
                        Experience = enemy.GetProperty("exp").GetUInt32(),
                        IsBossBGM = IsBoss,
                        IsBossGauge = IsBoss,
                        Scale = 100,
                        EnemyTargetTypesId = 4
                    };

                    ApplyOptionalEnemyKeys(enemy, questEnemy);
                    // ApplyEnemyDropTable

                    enemyGroup.Enemies.Add(questEnemy);
                }

                assetData.EnemyGroups[groupId++] = enemyGroup;
            }

            return true;
        }

        private void ApplyOptionalEnemyKeys(JsonElement enemy, Enemy questEnemey)
        {
            if (enemy.TryGetProperty("named_enemy_params_id", out JsonElement jNamedEnemyParamsId))
            {
                questEnemey.NamedEnemyParamsId = jNamedEnemyParamsId.GetUInt32();
            }

            if (enemy.TryGetProperty("raid_boss_id", out JsonElement jRaidBossId))
            {
                questEnemey.RaidBossId = jRaidBossId.GetUInt32();
            }

            if (enemy.TryGetProperty("scale", out JsonElement jScale))
            {
                questEnemey.Scale = jScale.GetUInt16();
            }

            if (enemy.TryGetProperty("hm_present_no", out JsonElement jHmPresetNo))
            {
                questEnemey.HmPresetNo = jHmPresetNo.GetUInt16();
            }

            if (enemy.TryGetProperty("start_think_tbl_no", out JsonElement jStartThinkTblNo))
            {
                questEnemey.StartThinkTblNo = jStartThinkTblNo.GetByte();
            }

            if (enemy.TryGetProperty("repop_num", out JsonElement jRepopNum))
            {
                questEnemey.RepopNum = jRepopNum.GetByte();
            }

            if (enemy.TryGetProperty("repop_count", out JsonElement jRepopCount))
            {
                questEnemey.RepopCount = jRepopCount.GetByte();
            }

            if (enemy.TryGetProperty("enemy_target_types_id", out JsonElement jEnemyTargetTypesId))
            {
                questEnemey.EnemyTargetTypesId = jEnemyTargetTypesId.GetByte();
            }

            if (enemy.TryGetProperty("mondatge_fix_no", out JsonElement jMontageFixNo))
            {
                questEnemey.MontageFixNo = jMontageFixNo.GetByte();
            }

            if (enemy.TryGetProperty("set_type", out JsonElement jSetType))
            {
                questEnemey.SetType = jSetType.GetByte();
            }

            if (enemy.TryGetProperty("infection_type", out JsonElement jInfectionType))
            {
                questEnemey.InfectionType = jInfectionType.GetByte();
            }

            if (enemy.TryGetProperty("is_boss_gauge", out JsonElement jIsBossGauge))
            {
                questEnemey.IsBossGauge = jIsBossGauge.GetBoolean();
            }

            if (enemy.TryGetProperty("is_boss_bgm", out JsonElement jIsBossBGM))
            {
                questEnemey.IsBossBGM = jIsBossBGM.GetBoolean();
            }

            if (enemy.TryGetProperty("is_manual_set", out JsonElement jIsManualSet))
            {
                questEnemey.IsManualSet = jIsManualSet.GetBoolean();
            }

            if (enemy.TryGetProperty("is_area_boss", out JsonElement jIsAreaBoss))
            {
                questEnemey.IsAreaBoss = jIsAreaBoss.GetBoolean();
            }

            if (enemy.TryGetProperty("blood_orbs", out JsonElement jBloodOrbs))
            {
                questEnemey.BloodOrbs = jBloodOrbs.GetUInt32();
            }

            if (enemy.TryGetProperty("high_orbs", out JsonElement jHighOrbs))
            {
                questEnemey.HighOrbs = jHighOrbs.GetUInt32();
            }

            if (enemy.TryGetProperty("spawn_time_start", out JsonElement jSpawnTimeStart))
            {
                questEnemey.SpawnTimeStart = jSpawnTimeStart.GetUInt32();
            }

            if (enemy.TryGetProperty("spawn_time_end", out JsonElement jSpawnTimeEnd))
            {
                questEnemey.SpawnTimeEnd = jSpawnTimeEnd.GetUInt32();
            }
        }

        private bool ParseRawBlock(JsonElement jBlock, QuestBlock questBlock)
        {
            foreach (var jCheckCommand in jBlock.GetProperty("check_commands").EnumerateArray())
            {
                CDataQuestCommand command = new CDataQuestCommand();

                if (!Enum.TryParse(jCheckCommand.GetProperty("type").GetString(), true, out QuestCommandCheckType type))
                {
                    return false;
                }

                command.Command = (ushort)type;
                ParseCommandParams(jCheckCommand, command);

                questBlock.CheckCommands.Add(command);
            }

            foreach (var jResultCommand in jBlock.GetProperty("result_commands").EnumerateArray())
            {
                CDataQuestCommand command = new CDataQuestCommand();

                if (!Enum.TryParse(jResultCommand.GetProperty("type").GetString(), true, out QuestResultCommand type))
                {
                    return false;
                }

                command.Command = (ushort)type;
                ParseCommandParams(jResultCommand, command);

                questBlock.ResultCommands.Add(command);
            }

            return true;
        }

        private void ParseCommandParams(JsonElement jCommand, CDataQuestCommand command)
        {
            List<string> commandParams = new List<string>() { "Param1", "Param2", "Param3", "Param4" };
            for (int i = 0; i < commandParams.Count; i++)
            {
                int paramValue = 0;
                if (jCommand.TryGetProperty(commandParams[i], out JsonElement jParam))
                {
                    paramValue = jParam.GetInt32();
                }

                switch (i)
                {
                    case 0:
                        command.Param01 = paramValue;
                        break;
                    case 1:
                        command.Param02 = paramValue;
                        break;
                    case 2:
                        command.Param03 = paramValue;
                        break;
                    case 3:
                        command.Param04 = paramValue;
                        break;
                }
            }
        }
    }
}
