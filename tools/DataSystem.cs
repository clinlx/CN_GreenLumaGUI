using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Models;
using CommunityToolkit.Mvvm.Messaging;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.IO;
using System.Linq;

namespace CN_GreenLumaGUI.tools
{
    public class DataSystem
    {
        private static readonly DataSystem instance = new();
        public static DataSystem Instance { get { return instance; } }
        public static bool isLoaded = false;
        public static bool isLoadedEnd = false;
        public static bool isError = false;
        //Settings Data
        public bool NotNullConfig { get; set; }//日志中发现有无论如何，配置都是false的情况，写来测试
        public string LastVersion { get; set; } = "null";
        public long StartSuccessTimes { get; set; }

        private string languageCode = string.Empty;
        public string LanguageCode
        {
            get
            {
                // 如果用戶未設定語言（首次啟動），則根據系統地區自動選擇
                if (string.IsNullOrWhiteSpace(languageCode))
                {
                    return LocalizationService.GetSystemLanguageCode();
                }
                return languageCode;
            }
            set
            {
                var targetCode = string.IsNullOrWhiteSpace(value) ? LocalizationService.DefaultLanguageCode : value;
                if (languageCode == targetCode)
                    return;
                languageCode = targetCode;
                LocalizationService.ApplyLanguage(languageCode);
                LocalizationService.SaveLanguageToSystem(languageCode);
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(LanguageCode)));
            }
        }

        private string? steamPath;
        public string? SteamPath
        {
            get => steamPath;
            set
            {
                steamPath = value;
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(SteamPath)));
            }
        }
        private bool isDarkTheme;
        public bool DarkMode
        {
            get => isDarkTheme;
            set
            {
                isDarkTheme = value;
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();
                theme.SetBaseTheme(isDarkTheme ? Theme.Dark : Theme.Light);
                paletteHelper.SetTheme(theme);
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(DarkMode)));
            }
        }
        private bool hidePromptText;
        public bool HidePromptText
        {
            get => hidePromptText;
            set
            {
                hidePromptText = value;
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(HidePromptText)));
            }
        }

        private bool startWithBak;
        public bool StartWithBak
        {
            get => startWithBak;
            set
            {
                startWithBak = value;
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(StartWithBak)));
            }
        }

        public bool HaveTriedBak { get; set; }//内部隐藏变量不用发送消息

        private bool scrollBarEcho;
        public bool ScrollBarEcho
        {
            get => scrollBarEcho;
            set
            {
                scrollBarEcho = value;
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(ScrollBarEcho)));
            }
        }
        private bool modifySteamDNS;
        public bool ModifySteamDNS
        {
            get => modifySteamDNS;
            set
            {
                modifySteamDNS = value;
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(ModifySteamDNS)));
            }
        }
        private bool runSteamWithAdmin;
        public bool RunSteamWithAdmin
        {
            get => runSteamWithAdmin;
            set
            {
                runSteamWithAdmin = value;
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(RunSteamWithAdmin)));
            }
        }

        private bool newFamilyModel;
        public bool NewFamilyModel
        {
            get => newFamilyModel;
            set
            {
                newFamilyModel = value;
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(NewFamilyModel)));
            }
        }

        private bool clearSteamAppCache;
        public bool ClearSteamAppCache
        {
            get => clearSteamAppCache;
            set
            {
                clearSteamAppCache = value;
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(ClearSteamAppCache)));
            }
        }

        private bool tryGetAppNameOnline;
        public bool TryGetAppNameOnline
        {
            get => tryGetAppNameOnline;
            set
            {
                tryGetAppNameOnline = value;
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(TryGetAppNameOnline)));
            }
        }

        private bool getDepotOnlyKey;
        public bool GetDepotOnlyKey
        {
            get => getDepotOnlyKey;
            set
            {
                getDepotOnlyKey = value;
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(GetDepotOnlyKey)));
            }
        }

        private bool singleConfigFileMode;
        public bool SingleConfigFileMode
        {
            get => singleConfigFileMode;
            set
            {
                singleConfigFileMode = value;
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(SingleConfigFileMode)));
            }
        }

        private bool getManifestInfoFromApi;
        public bool GetManifestInfoFromApi
        {
            get => getManifestInfoFromApi;
            set
            {
                getManifestInfoFromApi = value;
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(GetManifestInfoFromApi)));
            }
        }

        private bool echoStartSteamNormalButton;
        public bool EchoStartSteamNormalButton
        {
            get => echoStartSteamNormalButton;
            set
            {
                echoStartSteamNormalButton = value;
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(EchoStartSteamNormalButton)));
            }
		}

		private bool skipSteamUpdate;
		public bool SkipSteamUpdate
		{
			get => skipSteamUpdate;
			set
			{
				skipSteamUpdate = value;
				WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(SkipSteamUpdate)));
			}
		}

		//添加完字段后记得看看LoadData()和SettingsPageViewModel第34行

		private DataSystem()
        {
            gameDatas = new();
            gameExist = new();
            depotUnlockSet = new();
            dlcExist = new();
        }
        public readonly static string gameInfoCacheFile = $"{OutAPI.TempDir}\\gameInfoCache.json";
        public readonly static string apiSteamAppInfoCacheFile = $"{OutAPI.TempDir}\\apiSteamAppInfoCache.json";
        public readonly static string depotMapCacheFile = $"{OutAPI.TempDir}\\depotMapCacheFile.json";
        public readonly static string manifestListCacheFile = $"{OutAPI.TempDir}\\manifestListCache.json";
        private bool showManifestDownloadButton;
        public bool ShowManifestDownloadButton
        {
            get => showManifestDownloadButton;
            set
            {
                showManifestDownloadButton = value;
                WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(ShowManifestDownloadButton)));
            }
        }

        private readonly static string configFile = $"{OutAPI.TempDir}\\config.json";
        private readonly static string unlockListFile = $"{OutAPI.TempDir}\\unlocklist.json";
        private readonly static string depotUnlockListFile = $"{OutAPI.TempDir}\\unlocklist_depot.json";
        public void LoadData()
        {
            OutAPI.PrintLog("isLoaded");
            isLoaded = true;
            //读取软件配置文件
            dynamic? readConfig = null;
            if (File.Exists(configFile))
            {
                try
                {
                    readConfig = File.ReadAllText(configFile).FromJSON<dynamic>();
                }
                catch
                { }
            }
            //读取成功则设置，否则设为默认配置
            NotNullConfig = readConfig?.NotNullConfig ?? true;
            if (!NotNullConfig)
            {
                OutAPI.PrintLog("isError");
                isError = true;
                NotNullConfig = true;
                readConfig = null;
            }
            LastVersion = readConfig?.LastVersion ?? "null";
            StartSuccessTimes = readConfig?.StartSuccessTimes ?? 0;
            // 優先保留 App.OnStartup 中從註冊表讀取的語言設置
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                LanguageCode = readConfig?.LanguageCode ?? string.Empty;
            }
            SteamPath = readConfig?.SteamPath;
            DarkMode = readConfig?.DarkMode ?? false;
            HidePromptText = readConfig?.HidePromptText ?? false;
            StartWithBak = readConfig?.StartWithBak ?? true;
            HaveTriedBak = readConfig?.HaveTriedBak ?? false;
            ScrollBarEcho = readConfig?.ScrollBarEcho ?? true;
            ModifySteamDNS = readConfig?.ModifySteamDNS ?? false;
            RunSteamWithAdmin = readConfig?.RunSteamWithAdmin ?? true;
            //NewFamilyModel = readConfig?.NewFamilyModel ?? false;
            ClearSteamAppCache = readConfig?.ClearSteamAppCache ?? true;
            TryGetAppNameOnline = readConfig?.TryGetAppNameOnline ?? false;
            GetDepotOnlyKey = readConfig?.GetDepotOnlyKey ?? false;
            SingleConfigFileMode = readConfig?.SingleConfigFileMode ?? false;
            GetManifestInfoFromApi = readConfig?.GetManifestInfoFromApi ?? true;
            EchoStartSteamNormalButton = readConfig?.EchoStartSteamNormalButton ?? false;
			SkipSteamUpdate = readConfig?.SkipSteamUpdate ?? true;
            ShowManifestDownloadButton = readConfig?.ShowManifestDownloadButton ?? false;
			//读取游戏列表文件
			string gameDataText = "[]";
            if (File.Exists(unlockListFile))
            {
                gameDataText = File.ReadAllText(unlockListFile);
            }
            string depotUnlockDataText = "[]";
            if (File.Exists(depotUnlockListFile))
            {
                depotUnlockDataText = File.ReadAllText(depotUnlockListFile);
            }
            try
            {
                List<GameObj>? readGameDatas = gameDataText.FromJSON<List<GameObj>>();
                checkedNum = 0;
                if (readGameDatas is not null)
                {
                    foreach (var i in readGameDatas)
                    {
                        AddGame(i.GameName, i.GameId, i.IsSelected, i.DlcsList, true);
                    }
                }
            }
            catch
            {
                // ignored
            }
            try
            {
                List<long>? readDepotUnlockData = depotUnlockDataText.FromJSON<List<long>>();
                if (readDepotUnlockData is not null)
                {
                    foreach (var i in readDepotUnlockData)
                    {
                        depotUnlockSet.Add(i);
                    }
                }
            }
            catch
            {
                // ignored
            }
            OutAPI.PrintLog($"isLoadedEnd gameDatas.Count={gameDatas.Count} depotUnlockSet.Count={depotUnlockSet.Count}");
            isLoadedEnd = true;
            WeakReferenceMessenger.Default.Send(new LoadFinishedMessage("DataSystem"));
        }
        public void SaveData()
        {
            //写入软件配置文件
            File.WriteAllText(configFile, this.ToJSON(true));
            //写入游戏列表至文件
            File.WriteAllText(unlockListFile, gameDatas.ToJSON(true));
            File.WriteAllText(depotUnlockListFile, depotUnlockSet.ToJSON(true));
        }
        public void AddGame(string gameName, long gameId, bool isSelected, ObservableCollection<DlcObj> dlcsList, bool ignoreSave = false)
        {
            lock (gameExist)
            {
                foreach (var dlc in dlcsList)
                    if (dlc.IsSelected) CheckedNumInc(dlc.DlcId);
                if (IsGameExist(gameId))
                {
                    GameObj? obj = gameExist[gameId];
                    if (obj is not null)
                    {
                        foreach (var dlc in obj.DlcsList)
                            if (dlc.IsSelected) CheckedNumDec(dlc.DlcId);
                        obj.GameName = gameName;
                        obj.DlcsList = dlcsList;
                        obj.IsSelected = isSelected;
                        return;
                    }
                }
                var theGame = new GameObj(gameName, gameId, isSelected, dlcsList);
                foreach (var dlc in dlcsList)
                    dlc.Master = theGame;
                gameExist[gameId] = theGame;
                gameDatas.Add(theGame);
                theGame?.UpdateCheckNum();
            }
            WeakReferenceMessenger.Default.Send(new GameListChangedMessage(gameId));
            if (!ignoreSave) SaveData();
        }
        public void RemoveGame(GameObj game)
        {
            lock (gameExist)
            {
                if (game.IsSelected) CheckedNumDec(game.GameId);
                foreach (var dlc in game.DlcsList)
                {
                    UnregisterDlc(dlc);
                    if (dlc.IsSelected) CheckedNumDec(dlc.DlcId);
                }
                gameExist[game.GameId] = null;
                gameDatas.Remove(game);
            }
            WeakReferenceMessenger.Default.Send(new GameListChangedMessage(game.GameId));
            DataSystem.Instance.SaveData();

        }
        public bool IsGameExist(long gameId)
        {
            lock (gameExist)
            {
                if (!gameExist.TryGetValue(gameId, out GameObj? value))
                    return false;
                return value != null;
            }
        }
        public GameObj? GetGameObjFromId(long id)
        {
            lock (gameExist)
            {
                return gameExist.GetValueOrDefault(id);
            }
        }
        public ObservableCollection<GameObj> GetGameDatas()
        {
            return gameDatas;
        }

        private long checkedNum = 0;
        [JsonIgnore]
        public long CheckedNum => checkedNum + GetDepotUnlockCount();

        public void CheckedNumInc(long updateFrom)
        {
            checkedNum++;
            WeakReferenceMessenger.Default.Send(new CheckedNumChangedMessage(updateFrom, false));
        }
        public void CheckedNumDec(long updateFrom)
        {
            checkedNum--;
            WeakReferenceMessenger.Default.Send(new CheckedNumChangedMessage(updateFrom, true));
        }
        private readonly ObservableCollection<GameObj> gameDatas;
        private readonly Dictionary<long, GameObj?> gameExist;
        private readonly HashSet<long> depotUnlockSet;
        private readonly Dictionary<long, HashSet<DlcObj>> dlcExist;
        public void RegisterDlc(DlcObj dlc)
        {
            lock (dlcExist)
            {
                if (!dlcExist.TryGetValue(dlc.DlcId, out HashSet<DlcObj>? value))
                    dlcExist[dlc.DlcId] = value = new HashSet<DlcObj>();
                value.Add(dlc);
                WeakReferenceMessenger.Default.Send(new DlcListChangedMessage(dlc.DlcId));
            }
        }
        public void UnregisterDlc(DlcObj dlc)
        {
            lock (dlcExist)
            {
                if (dlcExist.TryGetValue(dlc.DlcId, out HashSet<DlcObj>? value))
                    value.Remove(dlc);
                WeakReferenceMessenger.Default.Send(new DlcListChangedMessage(dlc.DlcId));
            }
        }
        public bool IsDlcExist(long dlcId)
        {
            lock (dlcExist)
            {
                if (!dlcExist.TryGetValue(dlcId, out HashSet<DlcObj>? value))
                    return false;
                return value.Count > 0;
            }
        }
        public DlcObj? GetDlcObjFromId(long id)
        {
            lock (dlcExist)
            {
                if (!dlcExist.TryGetValue(id, out HashSet<DlcObj>? value))
                    return null;
                foreach (var dlc in value)
                    if (dlc.Master is not null)
                        return dlc;
                return null;
            }
        }
        public int GetDepotUnlockCount() => depotUnlockSet.Count;
        public List<long> GetUnlockDepotList() => depotUnlockSet.ToList();
        public bool IsDepotUnlock(long depotId) => depotUnlockSet.Contains(depotId);
        public void CheckDepotUnlockItem(long id)
        {
            if (depotUnlockSet.Remove(id))
                WeakReferenceMessenger.Default.Send(new CheckedNumChangedMessage(id, true));
        }
        public void UpdateDepotUnlockSet(HashSet<long> set)
        {
            var useless = depotUnlockSet.Where(i => !set.Contains(i)).ToList();
            foreach (var i in useless)
            {
                depotUnlockSet.Remove(i);
                WeakReferenceMessenger.Default.Send(new CheckedNumChangedMessage(i, true));
            }
        }
        public void SetDepotUnlock(long depotId, bool isUnlock)
        {
            if (isUnlock)
                depotUnlockSet.Add(depotId);
            else
                depotUnlockSet.Remove(depotId);
            WeakReferenceMessenger.Default.Send(new CheckedNumChangedMessage(depotId, !isUnlock));
        }
    }
}
