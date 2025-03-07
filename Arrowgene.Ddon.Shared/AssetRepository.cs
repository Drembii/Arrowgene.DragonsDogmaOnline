using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Arrowgene.Ddon.Shared.Csv;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using Arrowgene.Ddon.Shared.Json;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Asset;

namespace Arrowgene.Ddon.Shared
{
    public class AssetRepository
    {
        // Client data
        public const string ClientErrorCodesKey = "ClientErrorCodes.csv";
        public const string ItemListKey = "itemlist.csv";

        // Server data
        public const string EnemySpawnsKey = "EnemySpawn.json";
        public const string GatheringItemsKey = "GatheringItem.csv";
        public const string MyPawnAssetKey = "MyPawn.csv";
        public const string MyRoomAssetKey = "MyRoom.csv";
        public const string ArisenAssetKey = "Arisen.csv";
        public const string StorageKey = "Storage.csv";
        public const string StorageItemKey = "StorageItem.csv";
        public const string ShopKey = "Shop.json";
        public const string ServerListKey = "GameServerList.csv";
        public const string WarpPointsKey = "WarpPoints.csv";
        public const string CraftingRecipesKey = "CraftingRecipes.json";
        public const string LearnedNormalSkillsKey = "LearnedNormalSkills.json";
        public const string GPCourseInfoKey = "GpCourseInfo.json";
        public const string SecretAbilityKey = "DefaultSecretAbilities.json";
        public const string WorldQuestAssetKey = "world_quests.json";
        public const string MainQuestAssetKey = "main_quests.json";

        private static readonly ILogger Logger = LogProvider.Logger(typeof(AssetRepository));

        public event EventHandler<AssetChangedEventArgs> AssetChanged;

        private readonly DirectoryInfo _directory;
        private readonly Dictionary<string, FileSystemWatcher> _fileSystemWatchers;

        public AssetRepository(string folder)
        {
            _directory = new DirectoryInfo(folder);
            if (!_directory.Exists)
            {
                Logger.Error($"Could not initialize repository, '{folder}' does not exist");
                return;
            }

            _fileSystemWatchers = new Dictionary<string, FileSystemWatcher>();

            ClientErrorCodes = new List<CDataErrorMessage>();
            ClientItemInfos = new List<ClientItemInfo>();
            EnemySpawnAsset = new EnemySpawnAsset();
            GatheringItems = new Dictionary<(StageId, uint), List<GatheringItem>>();
            ServerList = new List<CDataGameServerListInfo>();
            MyPawnAsset = new List<MyPawnCsv>();
            MyRoomAsset = new List<MyRoomCsv>();
            ArisenAsset = new List<ArisenCsv>();
            StorageAsset = new List<CDataCharacterItemSlotInfo>();
            StorageItemAsset = new List<Tuple<StorageType, uint, Item>>();
            ShopAsset = new List<Shop>();
            WarpPoints = new List<WarpPoint>();
            CraftingRecipesAsset = new List<S2CCraftRecipeGetCraftRecipeRes>();
            LearnedNormalSkillsAsset = new LearnedNormalSkillsAsset();
            GPCourseInfoAsset = new GPCourseInfoAsset();
            SecretAbilitiesAsset = new SecretAbilityAsset();
            WorldQuestAsset = new QuestAsset();
            MainQuestAsset = new QuestAsset();
        }

        public List<CDataErrorMessage> ClientErrorCodes { get; private set; }
        public List<ClientItemInfo> ClientItemInfos { get; private set; } // May be incorrect, or incomplete
        public EnemySpawnAsset EnemySpawnAsset { get; private set; }
        public Dictionary<(StageId, uint), List<GatheringItem>> GatheringItems { get; private set; }
        public List<CDataGameServerListInfo> ServerList { get; private set; }
        public List<MyPawnCsv> MyPawnAsset { get; private set; }
        public List<MyRoomCsv> MyRoomAsset { get; private set; }
        public List<ArisenCsv> ArisenAsset { get; private set; }
        public List<CDataCharacterItemSlotInfo> StorageAsset { get; private set; }
        public List<Tuple<StorageType, uint, Item>> StorageItemAsset { get; private set; }
        public List<Shop> ShopAsset { get; private set; }
        public List<WarpPoint> WarpPoints { get; private set; }
        public List<S2CCraftRecipeGetCraftRecipeRes> CraftingRecipesAsset { get; private set; }
        public LearnedNormalSkillsAsset LearnedNormalSkillsAsset { get; set; }
        public GPCourseInfoAsset GPCourseInfoAsset { get; private set; }
        public SecretAbilityAsset SecretAbilitiesAsset { get; private set; }
        
        // Place Holders to use the asset system to detect hot reloads
        public QuestAsset WorldQuestAsset { get; set; }
        public QuestAsset MainQuestAsset { get; set; }

        public void Initialize()
        {
            RegisterAsset(value => ClientErrorCodes = value, ClientErrorCodesKey, new ClientErrorCodeCsv());
            RegisterAsset(value => ClientItemInfos = value, ItemListKey, new ClientItemInfoCsv());
            RegisterAsset(value => EnemySpawnAsset = value, EnemySpawnsKey, new EnemySpawnAssetDeserializer());
            RegisterAsset(value => GatheringItems = value, GatheringItemsKey, new GatheringItemCsv());
            RegisterAsset(value => MyPawnAsset = value, MyPawnAssetKey, new MyPawnCsvReader());
            RegisterAsset(value => MyRoomAsset = value, MyRoomAssetKey, new MyRoomCsvReader());
            RegisterAsset(value => ArisenAsset = value, ArisenAssetKey, new ArisenCsvReader());
            RegisterAsset(value => ServerList = value, ServerListKey, new GameServerListInfoCsv());
            RegisterAsset(value => StorageAsset = value, StorageKey, new StorageCsv());
            RegisterAsset(value => StorageItemAsset = value, StorageItemKey, new StorageItemCsv());
            RegisterAsset(value => ShopAsset = value, ShopKey, new JsonReaderWriter<List<Shop>>());
            RegisterAsset(value => WarpPoints = value, WarpPointsKey, new WarpPointCsv());
            RegisterAsset(value => CraftingRecipesAsset = value, CraftingRecipesKey, new JsonReaderWriter<List<S2CCraftRecipeGetCraftRecipeRes>>());
            RegisterAsset(value => LearnedNormalSkillsAsset = value, LearnedNormalSkillsKey, new LearnedNormalSkillsDeserializer());
            RegisterAsset(value => GPCourseInfoAsset = value, GPCourseInfoKey, new GPCourseInfoDeserializer());
            RegisterAsset(value => SecretAbilitiesAsset = value, SecretAbilityKey, new SecretAbilityDeserializer());
            RegisterAsset(value => WorldQuestAsset = value, WorldQuestAssetKey, new QuestAssetDeserializer());
            RegisterAsset(value => MainQuestAsset = value, MainQuestAssetKey, new QuestAssetDeserializer());
        }

        private void RegisterAsset<T>(Action<T> onLoadAction, string key, IAssetDeserializer<T> readerWriter)
        {
            Load(onLoadAction, key, readerWriter);
            RegisterFileSystemWatcher(onLoadAction, key, readerWriter);
        }

        private void Load<T>(Action<T> onLoadAction, string key, IAssetDeserializer<T> readerWriter)
        {
            string path = Path.Combine(_directory.FullName, key);
            FileInfo file = new FileInfo(path);
            if (!file.Exists)
            {
                Logger.Error($"Could not load '{key}', file does not exist");
            }

            try {
                T asset = readerWriter.ReadPath(file.FullName);
                onLoadAction.Invoke(asset);
                OnAssetChanged(key, asset);
            }
            catch (Exception e)
            {
                Logger.Error($"Could not load '{key}', error reading the file contents");
                Logger.Exception(e);
            }
        }

        private void RegisterFileSystemWatcher<T>(Action<T> onLoadAction, string key, IAssetDeserializer<T> readerWriter)
        {
            if (_fileSystemWatchers.ContainsKey(key))
            {
                return;
            }

            FileSystemWatcher watcher = new FileSystemWatcher(_directory.FullName, key);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += (object sender, FileSystemEventArgs e) =>
            {
                Logger.Debug($"Reloading assets from file '{e.FullPath}'");
                // Try reloading file
                int attempts = 0;
                while (true)
                {
                    try
                    {
                        Load(onLoadAction, key, readerWriter);
                        break;
                    }
                    catch (IOException ex)
                    {
                        // File isn't ready yet, so we need to keep on waiting until it is.
                        attempts++;
                        Logger.Write(LogLevel.Error, $"Failed to reload {e.FullPath}. {attempts} attempts", ex);

                        if (attempts > 10)
                        {
                            Logger.Write(LogLevel.Error,
                                $"Failed to reload {e.FullPath} after {attempts} attempts. Giving up.", ex);
                            break;
                        }
                    }

                    Thread.Sleep(1000);
                }
            };
            watcher.EnableRaisingEvents = true;
            _fileSystemWatchers.Add(key, watcher);
        }

        private void OnAssetChanged(string key, object asset)
        {
            EventHandler<AssetChangedEventArgs> assetChanged = AssetChanged;
            if (assetChanged != null)
            {
                AssetChangedEventArgs assetChangedEventArgs = new AssetChangedEventArgs(key, asset);
                assetChanged(this, assetChangedEventArgs);
            }
        }
    }
}
