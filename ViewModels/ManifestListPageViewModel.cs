using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace CN_GreenLumaGUI.ViewModels
{
    public class ManifestListPageViewModel : ObservableObject
    {
        readonly ManifestListPage page;
        public ManifestListPageViewModel(ManifestListPage page)
        {
            this.page = page;
            manifestList = null;
            searchBarText = "";
            ScanManifestListCmd = new RelayCommand(ScanManifestButton);
            ShowMoreInfoCmd = new RelayCommand(ShowMoreInfoButton);
            SearchBarButtonCmd = new RelayCommand(SearchBarButton);
            SearchButtonCmd = new RelayCommand(SearchButtonClick);
            EscButtonCmd = new RelayCommand(EscButtonCmdPress);
            ImportFileSelectCmd = new RelayCommand(ImportFileSelect);
            DisableSysUACCmd = new RelayCommand(DisableSysUAC);
            WeakReferenceMessenger.Default.Register<ManifestListChangedMessage>(this, (r, m) =>
            {
                IsFilteredOrderOutdated = true;
                OnPropertyChanged(nameof(PageEndText));
            });
            WeakReferenceMessenger.Default.Register<PageChangedMessage>(this, (r, m) =>
            {
                if (m.toPageIndex == 3)
                {
                    if (!isProcess && ManifestList is null && !string.IsNullOrEmpty(DataSystem.Instance.SteamPath))
                    {
                        if (File.Exists(DataSystem.manifestListCacheFile))
                            ReadManifestList();
                        else
                            ScanManifestList();
                    }
                    else if (IsFilteredOrderOutdated)
                        OnPropertyChanged(nameof(FilteredManifestList));
                }
            });
            WeakReferenceMessenger.Default.Register<ConfigChangedMessage>(this, (r, m) =>
            {
                if (m.kind == nameof(DataSystem.Instance.TryGetAppNameOnline))
                {
                    OnPropertyChanged(nameof(ViewModels.ManifestListPageViewModel.TryGetAppNameOnline));
                }
                if (m.kind == nameof(DataSystem.Instance.GetDepotOnlyKey))
                {
                    OnPropertyChanged(nameof(ViewModels.ManifestListPageViewModel.GetDepotOnlyKey));
                    isFilteredVisOutdated = true;
                    if (!string.IsNullOrEmpty(FilterText))
                        isFilteredTextOutdated = true;
                    OnPropertyChanged(nameof(FilteredManifestList));
                }
                if (m.kind == nameof(DataSystem.Instance.LanguageCode))
                {
                    // 當語言變更時，更新所有使用資源的動態文字
                    OnPropertyChanged(nameof(LoadingBarText));
                    OnPropertyChanged(nameof(PageEndText));
                    OnPropertyChanged(nameof(SelectPageAllDepotText));
                }
            });
            WeakReferenceMessenger.Default.Register<MouseDropFileMessage>(this, (r, m) =>
            {
                var steamPath = Path.GetDirectoryName(DataSystem.Instance.SteamPath);
                if (string.IsNullOrEmpty(steamPath))
                {
                    ManagerViewModel.Inform(LocalizationService.GetString("Msg_SteamPathNotSet"));
                    return;
                }
                if (Directory.Exists(m.path))
                {
                    var successNum = ImportDir(m.path);
                    var msgTemplate = LocalizationService.GetString("Msg_ImportFromDir");
                    ManagerViewModel.Inform(string.Format(msgTemplate, successNum));
                    if (successNum > 0 && ManifestList is not null) ScanManifestList();
                    return;
                }
                if (m.path.EndsWith(".zip"))
                {
                    try
                    {
                        // 解压到临时目录
                        string tempDir = OutAPI.SystemTempDir;
                        if (!Directory.Exists(tempDir))
                        {
                            Directory.CreateDirectory(tempDir);
                            OutAPI.AddSecurityControll2Folder(tempDir);
                        }
                        string tempZip = Path.Combine(tempDir, Path.GetFileName(m.path));
                        if (File.Exists(tempZip))
                        {
                            OutAPI.AddSecurityControll2File(tempZip);
                            File.Delete(tempZip);
                        }
                        using (var source = new FileStream(m.path, FileMode.Open, FileAccess.Read))
                        using (var destination = new FileStream(tempZip, FileMode.Create, FileAccess.Write))
                        {
                            source.CopyTo(destination);
                        }
                        string tempUnzip = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(m.path));
                        // 使用C#自带的压缩解压功能
                        using (var zip = ZipFile.OpenRead(tempZip))
                        {
                            zip.ExtractToDirectory(tempUnzip);
                        }
                        // 导入
                        var successNum = ImportDir(tempUnzip);
                        var msgTemplate = LocalizationService.GetString("Msg_ImportFromZip");
                        ManagerViewModel.Inform(string.Format(msgTemplate, successNum));
                        if (successNum > 0 && ManifestList is not null) ScanManifestList();
                        // 删除临时文件
                        Directory.Delete(tempUnzip, true);
                        File.Delete(tempZip);
                    }
                    catch (Exception ex)
                    {
                        var msgTemplate = LocalizationService.GetString("Msg_ImportFailed");
                        ManagerViewModel.Inform(string.Format(msgTemplate, ex.Message));
                    }
                    return;
                }
                if (m.path.EndsWith(".manifest"))
                {
                    string manifestPath = Path.Combine(steamPath, "depotcache", Path.GetFileName(m.path));
                    if (File.Exists(manifestPath))
                    {
                        ManagerViewModel.Inform(LocalizationService.GetString("Msg_ManifestExists"));
                        return;
                    }
                    File.Copy(m.path, manifestPath, true);
                    ManagerViewModel.Inform(LocalizationService.GetString("Msg_ManifestImported"));
                    if (ManifestList is not null) ScanManifestList();
                    return;
                }
                if (m.path.EndsWith(".lua") || m.path.EndsWith(".vdf"))
                {
                    SteamVdfHandler vdfHandler = new();
                    return;
                }
                if (!ImportFile(m.name, m.path, true)) return;
                if (ManifestList is not null) ScanManifestList();
            });
        }
        // Window
        public static string ScrollBarEchoState
        {
            get
            {
                if (DataSystem.Instance.ScrollBarEcho)
                    return "Visible";
                return "Hidden";
            }
        }
        // Add File
        public static int ImportDir(string path)
        {
            var readyFiles = Directory.GetFiles(path).ToList();
            foreach (var dir in Directory.GetDirectories(path))
            {
                readyFiles.AddRange(Directory.GetFiles(dir));
            }
            return readyFiles.Count(file => ImportFile(Path.GetFileNameWithoutExtension(file), file, false));
        }
        public static bool ImportFile(string name, string path, bool hasInform)
        {
            var steamPath = Path.GetDirectoryName(DataSystem.Instance.SteamPath);
            if (string.IsNullOrEmpty(steamPath)) return false;
            if (path.EndsWith(".zip") || path.EndsWith(".rar") || path.EndsWith(".7z"))
            {
                if (hasInform) ManagerViewModel.Inform(LocalizationService.GetString("Msg_UnsupportedFormat"));
                return false;
            }
            if (path.EndsWith("info.json"))
            {
                if (File.Exists(path))
                {
                    var infoJson = File.ReadAllText(path);
                    var info = JsonConvert.DeserializeObject<Dictionary<long, (string, long)>>(infoJson);
                    if (info is not null)
                    {
                        Dictionary<long, AppModelLite?>? searchAppCache = null;
                        try
                        {
                            if (File.Exists(DataSystem.gameInfoCacheFile))
                            {
                                var cacheStr = File.ReadAllText(DataSystem.gameInfoCacheFile);
                                searchAppCache = cacheStr.FromJSON<Dictionary<long, AppModelLite?>>();
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                        searchAppCache ??= [];
                        foreach (var pair in info)
                        {
                            searchAppCache.TryAdd(pair.Key, new AppModelLite(pair.Value.Item1, pair.Key, "", "", pair.Value.Item2 <= 0, pair.Value.Item2));
                            DataSystem.Instance.SetDepotUnlock(pair.Key, true);
                        }
                        File.WriteAllText(DataSystem.gameInfoCacheFile, JsonConvert.SerializeObject(searchAppCache));
                        if (hasInform) ManagerViewModel.Inform(LocalizationService.GetString("Msg_InfoImported"));
                        return true;
                    }
                }
            }
            if (path.EndsWith(".manifest"))
            {
                var cuts = name.Split('_');
                if (cuts.Length == 2)
                {
                    var depotIdStr = cuts[0];
                    if (long.TryParse(depotIdStr, out var _))
                    {
                        // 复制文件
                        string depotCachePath = Path.Combine(steamPath, "depotcache");
                        if (!Directory.Exists(depotCachePath)) Directory.CreateDirectory(depotCachePath);
                        string manifestPath = Path.Combine(depotCachePath, name + ".manifest");
                        if (hasInform)
                        {
                            var msgKey = File.Exists(manifestPath) ? "Msg_ManifestOverwritten" : "Msg_ManifestImported";
                            ManagerViewModel.Inform(LocalizationService.GetString(msgKey));
                        }
                        File.Copy(path, manifestPath, true);
                        // 修改vdf文件
                        var manifestIdStr = cuts[1];
                        SteamVdfHandler vdfHandler = new();
                        var res = vdfHandler.MergeManifestItem(depotIdStr, manifestIdStr);
                        if (!res)
                        {
                            OutAPI.PrintLog("[Import] Failed to merge manifest item: " + depotIdStr + " " + manifestIdStr);
                        }
                        vdfHandler.Save();
                        return true;
                    }
                }
                if (hasInform) ManagerViewModel.Inform(LocalizationService.GetString("Msg_InvalidFilename"));
                return false;
            }
            if (path.EndsWith(".st") || path.EndsWith(".lua") || path.EndsWith(".vdf"))
            {
                SteamVdfHandler vdfHandler = new();
                var res = vdfHandler.MergeFile(path);
                vdfHandler.Save();
                switch (res)
                {
                    case -99:
                        break;
                    case < 0:
                        if (hasInform) ManagerViewModel.Inform(vdfHandler.Err);
                        break;
                    case 0:
                        if (hasInform) ManagerViewModel.Inform(LocalizationService.GetString("Msg_NoKeysFound"));
                        break;
                    default:
                        if (hasInform)
                        {
                            var msgTemplate = LocalizationService.GetString("Msg_KeysMerged");
                            ManagerViewModel.Inform(string.Format(msgTemplate, res));
                        }
                        return true;
                }
                return false;
            }
            if (hasInform) ManagerViewModel.Inform(LocalizationService.GetString("Msg_UnsupportedFileType"));
            return false;
        }
        // Scan
        private bool isProcess = false;
        private int fileTotal = -1;
        public int FileTotal
        {
            get => fileTotal;
            set
            {
                fileTotal = value;
                OnPropertyChanged(nameof(LoadingBarValue));
                OnPropertyChanged(nameof(LoadingBarText));
            }
        }
        private int fileDone = 0;
        public int FileDone
        {
            get => fileDone;
            set
            {
                fileDone = value;
                OnPropertyChanged(nameof(LoadingBarValue));
                OnPropertyChanged(nameof(LoadingBarText));
            }
        }
        private int pageItemCount = 0;
        private async void ReadManifestList(bool checkNetWork = true)
        {
            bool result = true;
            string reason = string.Empty;
            var res = await Task.Run(async () =>
            {
                List<ManifestGameObj> save = [];
                ObservableCollection<ManifestGameObj> newList = [];
                try
                {
                    if (File.Exists(DataSystem.gameInfoCacheFile))
                    {
                        HashSet<long> usedUnlockIds = [];
                        var cacheStr = await File.ReadAllTextAsync(DataSystem.manifestListCacheFile);
                        save = (cacheStr!.FromJSON<List<ManifestGameObj>?>()) ?? save;
                        foreach (var item in save)
                        {
                            var game = new ManifestGameObj(item.GameName, item.GameId)
                            {
                                Installed = item.Installed
                            };
                            if (item.HasKey && SteamAppFinder.Instance.DepotDecryptionKeys.TryGetValue(item.GameId, out var _))
                            {
                                game.HasKey = true;
                            }
                            if (File.Exists(item.ManifestPath))
                                game.ManifestPath = item.ManifestPath;
                            foreach (var subItem in item.DepotList)
                            {
                                var dlc = new DepotObj(subItem.Name, subItem.DepotId, game, "");
                                if (subItem.HasKey && SteamAppFinder.Instance.DepotDecryptionKeys.TryGetValue(subItem.DepotId, out var _))
                                {
                                    dlc.HasKey = true;
                                }
                                if (File.Exists(subItem.ManifestPath))
                                    dlc.ManifestPath = subItem.ManifestPath;
                                game.DepotList.Add(dlc);
                                usedUnlockIds.Add(dlc.DepotId);
                            }
                            newList.Add(game);
                            usedUnlockIds.Add(game.GameId);
                        }
                        DataSystem.Instance.UpdateDepotUnlockSet(usedUnlockIds);
                    }
                }
                catch (Exception ex)
                {
                    result = false;
                    reason = ex.Message;
                }
                return newList;
            });
            if (!result)
            {
                var msgTemplate = LocalizationService.GetString("Msg_ScanStarting");
                ManagerViewModel.Inform(string.Format(msgTemplate, reason));
                ScanManifestList(checkNetWork);
            }
            else
            {
                ManifestList?.Clear();
                ManifestList = res;
                HashSet<long> usedUnlockIds = [];
                foreach (var game in ManifestList)
                {
                    pageItemCount++;
                    if (DataSystem.Instance.IsDepotUnlock(game.GameId))
                    {
                        game.IsSelected = true;
                        usedUnlockIds.Add(game.GameId);
                    }
                    foreach (var depot in game.DepotList)
                    {
                        pageItemCount++;
                        if (DataSystem.Instance.IsDepotUnlock(depot.DepotId))
                        {
                            depot.IsSelected = true;
                            usedUnlockIds.Add(depot.DepotId);
                        }
                    }
                }
                DataSystem.Instance.UpdateDepotUnlockSet(usedUnlockIds);
                LoadingBarVis = Visibility.Collapsed;
                FileTotal = 0;
                FileDone = 0;
                isProcess = false;
                OnPropertyChanged(nameof(SelectPageAll));
                OnPropertyChanged(nameof(SelectPageAllDepotText));
                isFilteredListItemOutdated = true;
                OnPropertyChanged(nameof(FilteredManifestList));
                WeakReferenceMessenger.Default.Send(new ManifestListChangedMessage(-1));
            }
        }
        public RelayCommand ScanManifestListCmd { get; set; }
        private void ScanManifestButton() => ScanManifestList();
        private async void ScanManifestList(bool checkNetWork = true)
        {
            if (isProcess) return;
            OnPropertyChanged(nameof(PageEndText));
            pageItemCount = 0;
            isProcess = true;
            FileTotal = -1;
            FileDone = 0;
            LoadingBarVis = Visibility.Visible;
            (bool result, string reason) = await ScanFromSteam(checkNetWork);
            LoadingBarVis = Visibility.Collapsed;
            FileTotal = 0;
            FileDone = 0;
            isProcess = false;
            OnPropertyChanged(nameof(SelectPageAll));
            OnPropertyChanged(nameof(SelectPageAllDepotText));
            isFilteredListItemOutdated = true;
            OnPropertyChanged(nameof(FilteredManifestList));
            if (!result)
            {
                var msgTemplate = LocalizationService.GetString("Msg_GetManifestFailed");
                ManagerViewModel.Inform(string.Format(msgTemplate, reason));
            }
            else
                WeakReferenceMessenger.Default.Send(new ManifestListChangedMessage(-1));
        }
        private async Task<(bool, string)> ScanFromSteam(bool checkNetWork = true)
        {
            if (string.IsNullOrEmpty(DataSystem.Instance.SteamPath))
            {
                return (false, LocalizationService.GetString("Msg_SteamPathNotSetDesc"));
            }
            var steamPath = Path.GetDirectoryName(DataSystem.Instance.SteamPath);
            if (string.IsNullOrEmpty(steamPath))
                return (false, LocalizationService.GetString("Msg_SteamPathError"));
            var res = await Task.Run(async () =>
            {
                await Task.Yield();
                SteamAppFinder.Instance.Scan();
                // 检查网络
                bool hasNetWork = checkNetWork;
                if (TryGetAppNameOnline && hasNetWork)
                {
                    (_, SteamWebData.GetAppInfoState searchState) = await SteamWebData.Instance.GetAppNameSimpleAsync(0);
                    if (searchState == SteamWebData.GetAppInfoState.WrongNetWork) hasNetWork = false;
                }
                bool hasNetWorkToApi = checkNetWork && DataSystem.Instance.GetManifestInfoFromApi;
                if (TryGetAppNameOnline && hasNetWorkToApi)
                {
                    if (await SteamWebData.Instance.GetServerStatusFromApi() != true) hasNetWorkToApi = false;
                }
                // 获取游戏列表
                var gameData = DataSystem.Instance.GetGameDatas();
                // 联网搜索缓存
                Dictionary<long, AppModelLite?>? searchAppCache = null;
                try
                {
                    if (File.Exists(DataSystem.gameInfoCacheFile))
                    {
                        var cacheStr = await File.ReadAllTextAsync(DataSystem.gameInfoCacheFile);
                        searchAppCache = cacheStr.FromJSON<Dictionary<long, AppModelLite?>>();
                    }
                }
                catch
                {
                    // ignored
                }
                searchAppCache ??= [];
                // Api缓存
                Dictionary<long, ApiCacheLine>? apiQueryAppCache = null;
                try
                {
                    if (File.Exists(DataSystem.apiSteamAppInfoCacheFile))
                    {
                        var cacheStr = File.ReadAllText(DataSystem.apiSteamAppInfoCacheFile);
                        apiQueryAppCache = cacheStr.FromJSON<Dictionary<long, ApiCacheLine>>();
                    }
                }
                catch
                {
                    // ignored
                }
                apiQueryAppCache ??= [];
                // Depot缓存
                Dictionary<long, DepotCacheLine>? queryDepotCache = null;
                try
                {
                    if (File.Exists(DataSystem.depotMapCacheFile))
                    {
                        var cacheStr = File.ReadAllText(DataSystem.depotMapCacheFile);
                        queryDepotCache = cacheStr.FromJSON<Dictionary<long, DepotCacheLine>>();
                    }
                }
                catch
                {
                    // ignored
                }
                queryDepotCache ??= [];
                // 获取主解锁列表中的游戏信息
                Dictionary<long, object> dict = [];
                foreach (var game in gameData)
                {
                    var gameCopy = new ManifestGameObj(game.GameName, game.GameId);
                    dict.Add(game.GameId, gameCopy);
                    foreach (var dlc in game.DlcsList)
                    {
                        dict.Add(dlc.DlcId, dlc);
                        queryDepotCache[dlc.DlcId] = new(dlc.DlcId, dlc.DlcName, game.GameId, false, DateTime.Now);
                    }
                }
                // 读取磁盘中已安装游戏
                foreach (var gamePair in SteamAppFinder.Instance.GameInstall)
                {
                    var gameAddName = gamePair.Value;
                    if (dict.Remove(gamePair.Key, out var obj))
                    {
                        if (obj is ManifestGameObj game)
                            gameAddName = game.GameName;
                    }
                    var gameCopy = new ManifestGameObj(gameAddName, gamePair.Key)
                    {
                        FindSelf = true,
                        Installed = true
                    };
                    dict.Add(gamePair.Key, gameCopy);
                }
                // 磁盘搜索清单文件
                string depotCachePath = Path.Combine(steamPath, "depotcache");
                if (Directory.Exists(depotCachePath))
                {
                    var files = Directory.GetFiles(depotCachePath);
                    lock (this)
                    {
                        FileTotal = files.Length + SteamAppFinder.Instance.DepotDecryptionKeys.Count;
                    }
                    // 入队函数
                    int realFileTotal = 1;
                    HashSet<long> isInQueue = [];
                    HashSet<long> isTailInQueue = [];
                    var priorityQueue = new PriorityQueue<(long, string, bool, bool), long>();
                    void PutToQueue(long localDepotId, string mPath = "", bool withNetwork = true, bool toTail = false)
                    {
                        if (!toTail)
                        {
                            if (isInQueue.Contains(localDepotId)) return;
                        }
                        else
                        {
                            if (isTailInQueue.Contains(localDepotId)) return;
                        }
                        realFileTotal++;
                        if (!toTail)
                        {
                            priorityQueue.Enqueue((localDepotId, mPath, withNetwork, false), localDepotId);
                            isInQueue.Add(localDepotId);
                        }
                        else
                        {
                            priorityQueue.Enqueue((localDepotId, mPath, withNetwork, true), long.MaxValue / 2 + localDepotId);
                            isTailInQueue.Add(localDepotId);
                        }
                    }
                    // 入队
                    foreach (string file in files)
                    {
                        if (file.EndsWith(".manifest"))
                        {
                            var name = Path.GetFileNameWithoutExtension(file);
                            var cuts = name.Split('_');
                            if (cuts.Length != 2) continue;
                            var depotId = long.Parse(cuts[0]);
                            PutToQueue(depotId, file, TryGetAppNameOnline);
                        }
                    }
                    foreach (var keyPair in SteamAppFinder.Instance.DepotDecryptionKeys)
                    {
                        PutToQueue(keyPair.Key, string.Empty, TryGetAppNameOnline && GetDepotOnlyKey);
                    }
                    // 处理队列
                    await Task.Run(async () =>
                    {
                        List<(long, string)> errorsList = [];
                        int apiErrTimes = 0;
                        int searchErrTimes = 0;
                        while (priorityQueue.Count > 0)
                        {
                            // 登记完成了一个文件
                            lock (this)
                            {
                                FileDone++;
                                if (realFileTotal > FileTotal) FileTotal = realFileTotal;
                            }
                            // 获取参数
                            (long localDepotId, string mPath, bool withNetwork, bool inTail) = priorityQueue.Dequeue();
                            try
                            {
                                long mayBeParentId = localDepotId / 10 * 10;
                                // 过滤
                                if (localDepotId < 230000)
                                {
                                    errorsList.Add((localDepotId, "depotId过小"));
                                    continue;
                                }
                                if (SteamAppFinder.Instance.Excluded.Contains(localDepotId))
                                {
                                    errorsList.Add((localDepotId, "depot为排除项"));
                                    continue;
                                }
                                if (SteamAppFinder.Instance.Excluded.Contains(mayBeParentId))
                                {
                                    errorsList.Add((localDepotId, "depot父节点为排除项"));
                                    continue;
                                }
                                // 临时参数
                                string echoName = "";
                                long parentId = 0;
                                bool isGame = false;
                                bool isTemp = true;
                                ManifestGameObj? gameObj = null;
                                // 获取信息
                                /// 从本地列表中
                                if (isTemp && dict.TryGetValue(localDepotId, out var dictObj))
                                {
                                    if (dictObj is ManifestGameObj mObj && mObj.GameId == localDepotId)
                                    {
                                        gameObj = mObj;
                                        echoName = mObj.GameName;
                                        parentId = 0;
                                        isGame = true;
                                        isTemp = false;
                                    }
                                    else if (dictObj is DlcObj dlcObj && dlcObj.DlcId == localDepotId)
                                    {
                                        echoName = dlcObj.DlcName;
                                        parentId = dlcObj.Master?.GameId ?? 0;
                                        isGame = false;
                                        isTemp = false;
                                    }
                                }
                                /// 从Depot缓存中
                                // bool isFromDepotCache = false;
                                if (isTemp)
                                {
                                    if (queryDepotCache.TryGetValue(localDepotId, out var depotCacheLine) && depotCacheLine.Parent != 0 && localDepotId != depotCacheLine.Parent)
                                    {
                                        echoName = depotCacheLine.Name;
                                        parentId = depotCacheLine.Parent;
                                        isGame = false;
                                        isTemp = depotCacheLine.IsTemp;
                                        // isFromDepotCache = true;
                                    }
                                }
                                /// 从网络中获取
                                if (withNetwork)
                                {
                                    // Api查找
                                    if (isTemp)
                                    {
                                        ApiSimpleApp? appInfo = null;
                                        // 本地缓存
                                        bool hasNullCache = false;
                                        if (appInfo is null && apiQueryAppCache.TryGetValue(localDepotId, out var appCacheLine))
                                        {
                                            if (appCacheLine.IsOutDate())
                                            {
                                                apiQueryAppCache.Remove(localDepotId);
                                            }
                                            else
                                            {
                                                if (appCacheLine.HasContent())
                                                {
                                                    if (appCacheLine.App is not null)
                                                    {
                                                        hasNullCache = false;
                                                        appInfo = appCacheLine.App;
                                                    }
                                                    else if (appCacheLine.FromId != 0 && apiQueryAppCache.TryGetValue(appCacheLine.FromId, out var fromLine))
                                                    {
                                                        hasNullCache = fromLine.App is null;
                                                        appInfo = fromLine.App;
                                                    }
                                                }
                                                else
                                                {
                                                    hasNullCache = true;
                                                    appInfo = null;
                                                }
                                            }
                                        }
                                        // 网络查找
                                        if (appInfo is null && hasNetWorkToApi && !hasNullCache)
                                        {
                                            (appInfo, var state) = await SteamWebData.Instance.GetAppInformFromApi(localDepotId);
                                            if (state == SteamWebData.GetAppInfoState.WrongNetWork)
                                            {
                                                apiErrTimes += 1;
                                                if (apiErrTimes >= 3)
                                                {
                                                    hasNetWorkToApi = false;
                                                }
                                            }
                                            else apiErrTimes = 0;
                                            // 更新缓存
                                            if (state == SteamWebData.GetAppInfoState.Success && appInfo is not null && appInfo.IsGame())
                                            {
                                                int dlcIdx = 0;
                                                foreach (var dlcId in appInfo.ListOfDlc)
                                                {
                                                    dlcIdx++;
                                                    if (searchAppCache.ContainsKey(dlcId) && searchAppCache[dlcId] is not null) continue;
                                                    if (apiQueryAppCache.ContainsKey(dlcId) && apiQueryAppCache[dlcId].HasContent()) continue;
                                                    if (queryDepotCache.ContainsKey(dlcId)) continue;
                                                    queryDepotCache.TryAdd(dlcId, new(dlcId, $"{appInfo.GetName()} DLC-{dlcIdx}", appInfo.Id, true, DateTime.Now));
                                                }
                                                foreach (var depot in appInfo.Depots)
                                                {
                                                    if (searchAppCache.ContainsKey(depot.Id) && searchAppCache[depot.Id] is not null) continue;
                                                    if (apiQueryAppCache.ContainsKey(depot.Id) && apiQueryAppCache[depot.Id].HasContent()) continue;
                                                    if (queryDepotCache.ContainsKey(depot.Id)) continue;
                                                    queryDepotCache.TryAdd(depot.Id, new(depot.Id, appInfo.GetName(), appInfo.Id, true, DateTime.Now));
                                                }
                                            }
                                            if (state != SteamWebData.GetAppInfoState.WrongNetWork)
                                            {
                                                if (appInfo is null)
                                                {
                                                    if (!apiQueryAppCache.ContainsKey(localDepotId))
                                                        apiQueryAppCache[localDepotId] = ApiCacheLine.Create(null);
                                                }
                                                else
                                                {
                                                    apiQueryAppCache[appInfo.Id] = ApiCacheLine.Create(appInfo);
                                                    if (localDepotId != appInfo.Id)
                                                        apiQueryAppCache[localDepotId] = ApiCacheLine.Create(appInfo.Id);
                                                }
                                            }
                                        }
                                        // 判断和写入
                                        if (appInfo is not null)
                                        {
                                            if (appInfo.Id != localDepotId && appInfo.IsGame())
                                            {
                                                echoName = appInfo.GetName();
                                                parentId = appInfo.Id;
                                                isGame = false;
                                                isTemp = false;
                                            }
                                            else
                                            {
                                                echoName = appInfo.GetName();
                                                parentId = appInfo.Parent ?? 0;
                                                isGame = appInfo.IsGame();
                                                isTemp = false;
                                            }
                                        }
                                    }
                                    // 商店查找
                                    if (isTemp)
                                    {
                                        AppModelLite? appInfo = null;
                                        // 本地缓存
                                        bool hasNullCache = false;
                                        if (appInfo is null && searchAppCache.TryGetValue(localDepotId, out appInfo))
                                        {
                                            hasNullCache = appInfo is null;
                                        }
                                        // 网络查找
                                        if (appInfo is null && hasNetWork && !hasNullCache)
                                        {
                                            var storeUrl = $"https://store.steampowered.com/app/{localDepotId}/";
                                            (AppModel? oriInfo, SteamWebData.GetAppInfoState err) = await SteamWebData.Instance.GetAppInformAsync(storeUrl);
                                            if (err == SteamWebData.GetAppInfoState.WrongNetWork)
                                            {
                                                searchErrTimes += 1;
                                                if (searchErrTimes >= 3)
                                                {
                                                    hasNetWork = false;
                                                }
                                            }
                                            else searchErrTimes = 0;
                                            appInfo = oriInfo?.ToLite();
                                            // 更新缓存
                                            if (err != SteamWebData.GetAppInfoState.WrongUrl && err != SteamWebData.GetAppInfoState.WrongNetWork)
                                                searchAppCache.TryAdd(localDepotId, appInfo);
                                        }
                                        // 判断和写入
                                        if (appInfo is not null)
                                        {
                                            if (appInfo.AppId != localDepotId && appInfo.IsGame)
                                            {
                                                echoName = appInfo.AppName;
                                                parentId = appInfo.AppId;
                                                isGame = false;
                                                isTemp = false;
                                            }
                                            else
                                            {
                                                echoName = appInfo.AppName;
                                                parentId = appInfo.ParentId;
                                                isGame = appInfo.IsGame;
                                                isTemp = false;
                                            }
                                        }
                                    }
                                }
                                /// 尽最大可能获取信息
                                if (isTemp)
                                {
                                    /// 对于未知的对象，也没获取到信息，如果是整十，假定是游戏
                                    if (mayBeParentId == localDepotId) isGame = true;
                                    /// 如果未获取到信息，从扫描acf文件的缓存尝试获取，至少要知道父级ID
                                    if (SteamAppFinder.Instance.FindGameByDepotId.TryGetValue(localDepotId, out var gameId) && gameId != 0 && localDepotId != gameId)
                                    {
                                        parentId = gameId;
                                        isGame = false;
                                    }
                                }
                                // 构建对象
                                if (isGame)
                                {
                                    if (gameObj is not null)
                                    {
                                        if (!string.IsNullOrEmpty(mPath))
                                        {
                                            gameObj.ManifestPath = mPath;
                                        }
                                        continue;
                                    }
                                    var game = new ManifestGameObj(echoName, localDepotId)
                                    {
                                        ManifestPath = mPath,
                                        FindSelf = !isTemp && mayBeParentId == localDepotId,
                                        IsTemp = isTemp
                                    };
                                    dict.Add(localDepotId, game);
                                    continue;
                                }
                                else
                                {
                                    if (parentId == 0) parentId = mayBeParentId;
                                    if (dict.TryGetValue(parentId, out var parentObj))
                                    {
                                        if (parentObj is DlcObj dlcObj) parentObj = dlcObj.Master;
                                        if (parentObj is ManifestGameObj master)
                                        {
                                            if (string.IsNullOrEmpty(echoName)) echoName = master.GameName;
                                            DepotObj newDepotObj = new(echoName, localDepotId, master, mPath)
                                            {
                                                ManifestPath = mPath
                                            };
                                            master.DepotList!.Add(newDepotObj);
                                            // depot缓存
                                            if (!string.IsNullOrEmpty(echoName) && !isTemp && !master.IsTemp)
                                            {
                                                queryDepotCache[localDepotId] = new(localDepotId, echoName, master.GameId, false, DateTime.Now);
                                            }
                                        }
                                        continue;
                                    }
                                    else if (!inTail)
                                    {
                                        PutToQueue(parentId, mPath, withNetwork);
                                        PutToQueue(localDepotId, mPath, withNetwork, true);
                                        continue;
                                    }
                                    else
                                    {
                                        dict.Add(localDepotId, new ManifestGameObj(echoName, localDepotId)
                                        {
                                            ManifestPath = mPath,
                                            FindSelf = true,
                                            IsTemp = true
                                        });
                                        errorsList.Add((localDepotId, LocalizationService.GetString("Manifest_ParentNotFound")));
                                        continue;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                _ = OutAPI.MsgBox(e.Message, LocalizationService.GetString("Common_Error"));
                                errorsList.Add((localDepotId, LocalizationService.GetString("Common_Exception")));
                            }
                        }
                        // _ = OutAPI.MsgBox(errorsList.ToJSON());
                    });
                }
                // 写入更新后的缓存文件
                await File.WriteAllTextAsync(DataSystem.gameInfoCacheFile, JsonConvert.SerializeObject(searchAppCache));
                await File.WriteAllTextAsync(DataSystem.apiSteamAppInfoCacheFile, JsonConvert.SerializeObject(apiQueryAppCache));
                await File.WriteAllTextAsync(DataSystem.depotMapCacheFile, JsonConvert.SerializeObject(queryDepotCache));
                // 构建清单列表
                ObservableCollection<ManifestGameObj> newList = [];
                foreach (var item in dict.Values)
                {
                    if (item is not ManifestGameObj game) continue;
                    if (game is { FindSelf: false, DepotList.Count: 0 }) continue;
                    newList.Add(game);
                }
                // 写入当前的清单列表缓存文件
                await File.WriteAllTextAsync(DataSystem.manifestListCacheFile, JsonConvert.SerializeObject(newList));
                return newList;
            });
            ManifestList?.Clear();
            ManifestList = res;
            HashSet<long> usedUnlockIds = [];
            foreach (var game in ManifestList)
            {
                pageItemCount++;
                if (DataSystem.Instance.IsDepotUnlock(game.GameId))
                {
                    game.IsSelected = true;
                    usedUnlockIds.Add(game.GameId);
                }
                foreach (var depot in game.DepotList)
                {
                    pageItemCount++;
                    if (DataSystem.Instance.IsDepotUnlock(depot.DepotId))
                    {
                        depot.IsSelected = true;
                        usedUnlockIds.Add(depot.DepotId);
                    }
                }
            }
            DataSystem.Instance.UpdateDepotUnlockSet(usedUnlockIds);
            return (true, string.Empty);
        }

        //Binding

        private ObservableCollection<ManifestGameObj>? manifestList;

        public ObservableCollection<ManifestGameObj>? ManifestList
        {
            get
            {
                lock (this)
                {
                    return manifestList;
                }
            }
            set
            {
                lock (this)
                {
                    manifestList = value;
                }
                isFilteredListItemOutdated = true;
                OnPropertyChanged(nameof(FilteredManifestList));
            }
        }
        private string filterText = "";
        public string FilterText
        {
            get => filterText;
            set
            {
                filterText = value;
                isFilteredTextOutdated = true;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredManifestList));
                OnPropertyChanged(nameof(SearchBarButtonColor));
            }
        }
        private bool isFilteredVisOutdated = true;
        private bool isFilteredTextOutdated = true;
        private bool isFilteredListItemOutdated = true;
        public bool IsFilteredOrderOutdated { get; set; } = false;
        private readonly ObservableCollection<ManifestGameObj> filtered = [];
        private List<ManifestGameObj>? filteredListTemp;
        public ObservableCollection<ManifestGameObj> FilteredManifestList
        {
            get
            {
                if (ManifestList is null) return filtered;
                if (isFilteredListItemOutdated)
                {
                    filtered.Clear();
                    foreach (var game in ManifestList)
                    {
                        filtered.Add(game);
                    }
                }
                if (isFilteredVisOutdated || isFilteredListItemOutdated || isFilteredTextOutdated)
                {
                    foreach (var game in filtered)
                    {
                        if (game.Hide != false) game.Hide = false;
                        bool hideFromText = false;
                        bool hideFromOnltKey = false;
                        if (isFilteredTextOutdated)
                        {
                            var needFilter = true;
                            if (string.IsNullOrWhiteSpace(FilterText))
                                needFilter = false;
                            else
                            {
                                if (game.TitleText.Contains(FilterText, StringComparison.CurrentCultureIgnoreCase))
                                    needFilter = false;
                                else
                                {
                                    foreach (var depot in game.DepotList)
                                    {
                                        if (depot.DepotText.Contains(FilterText, StringComparison.CurrentCultureIgnoreCase))
                                            needFilter = false;
                                    }
                                }
                            }
                            hideFromText = needFilter;
                        }
                        if (string.IsNullOrWhiteSpace(FilterText))
                        {
                            bool needSkip = !game.HasManifest && !game.Installed;
                            foreach (var depot in game.DepotList)
                            {
                                if (!needSkip) break;
                                if (depot.HasManifest) needSkip = false;
                            }
                            if (needSkip && game.CheckItemCount == 0 && !GetDepotOnlyKey)
                            {
                                hideFromOnltKey = true;
                            }
                        }
                        if (hideFromText || hideFromOnltKey)
                        {
                            if (!game.Hide) game.Hide = true;
                        }
                        else
                        {
                            if (game.Hide) game.Hide = false;
                        }
                    }
                }
                if (!isFilteredTextOutdated || IsFilteredOrderOutdated)
                {
                    // 按照名称排序
                    IOrderedEnumerable<ManifestGameObj> manifestGame;
                    if (string.IsNullOrEmpty(FilterText))
                    {
                        manifestGame = filtered
                        .OrderBy(a => !(a.CheckItemCount > 0))
                        .ThenBy(a => a.GameId);
                    }
                    else
                    {
                        manifestGame = filtered
                            .OrderBy(a => !(a.CheckItemCount > 0))
                            .ThenBy(a => a.GameName);
                    }
                    filteredListTemp = [.. manifestGame];
                    filtered.Clear();
                    foreach (var item in filteredListTemp)
                        filtered.Add(item);
                    filteredListTemp.Clear();
                }
                isFilteredListItemOutdated = false;
                isFilteredVisOutdated = false;
                isFilteredTextOutdated = false;
                IsFilteredOrderOutdated = false;
                return filtered;
            }
        }
        private Visibility loadingBarVis = Visibility.Collapsed;
        public Visibility LoadingBarVis
        {
            get => loadingBarVis;
            set
            {
                loadingBarVis = value;
                OnPropertyChanged();
            }
        }
        public RelayCommand ShowMoreInfoCmd { get; set; }
        private void ShowMoreInfoButton()
        {
            ShowMoreInfo = !ShowMoreInfo;
            if (SearchBar) SearchBar = false;
        }
        private bool showMoreInfo = false;
        public bool ShowMoreInfo
        {
            get => showMoreInfo;
            set
            {
                showMoreInfo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MoreInfoButtonColor));
                OnPropertyChanged(nameof(ShowMoreInfoVisibility));
            }
        }
        public string MoreInfoButtonColor => ShowMoreInfo ? "Gray" : "Blue";
        public Visibility ShowMoreInfoVisibility => ShowMoreInfo ? Visibility.Visible : Visibility.Collapsed;

        public RelayCommand SearchBarButtonCmd { get; set; }
        private DateTime lastSearchBarButtonTime = DateTime.MinValue;
        private async void SearchBarButton()
        {
            // 内置冷却
            if (DateTime.Now - lastSearchBarButtonTime < TimeSpan.FromMilliseconds(100))
                return;
            lastSearchBarButtonTime = DateTime.Now;
            // 切换搜索条状态
            SearchBar = !SearchBar;
            if (ShowMoreInfo) ShowMoreInfo = false;
            await Task.Delay(10);
            if (SearchBar) page.searchBarTextBox.Focus();
            else page.manifestListPageBackground.Focus();
        }
        private bool searchBar = false;
        public bool SearchBar
        {
            get => searchBar;
            set
            {
                searchBar = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SearchBarButtonColor));
                OnPropertyChanged(nameof(SearchBarVisibility));
            }
        }
        public string SearchBarButtonColor => string.IsNullOrEmpty(FilterText) ? (SearchBar ? "DarkBlue" : "Gray") : "DarkGreen";
        public Visibility SearchBarVisibility => SearchBar ? Visibility.Visible : Visibility.Collapsed;

        public string LoadingBarText
        {
            get
            {
                if (FileTotal < 0) return LocalizationService.GetString("Msg_PreparingScan");
                if (FileTotal == 0) return LocalizationService.GetString("Msg_ScanComplete");
                var msgTemplate = LocalizationService.GetString("Msg_Scanning");
                return string.Format(msgTemplate, FileDone, FileTotal);
            }
        }
        public int LoadingBarValue
        {
            get
            {
                if (FileTotal < 0) return 0;
                if (FileTotal == 0) return 100;
                return (int)(FileDone * 100.0 / FileTotal);
            }
            set
            {

            }
        }

        public string PageEndText
        {
            get
            {
                if (DataSystem.Instance.SteamPath is null or "")
                {
                    return LocalizationService.GetString("Msg_SteamPathNotSetDesc");
                }
                int count = ManifestList?.Count ?? -1;
                if (count == -1)
                    return LocalizationService.GetString("Msg_ScanningManifests");
                if (count == 0)
                    return LocalizationService.GetString("Msg_NoManifestFound");
                if (count > 12)
                    return LocalizationService.GetString("Manifest_PageEnd");
                return "";
            }
        }
        private bool selectPageAll;
        public bool SelectPageAll
        {
            get => selectPageAll;
            set
            {
                selectPageAll = value;
                OnPropertyChanged();
                if (ManifestList is null) return;
                if (value)
                {
                    foreach (var game in ManifestList)
                    {
                        game.IsSelected = true;
                        foreach (var depot in game.DepotList)
                        {
                            depot.IsSelected = true;
                        }
                    }
                }
                else
                {
                    foreach (var game in ManifestList)
                    {
                        game.IsSelected = false;
                        foreach (var depot in game.DepotList)
                        {
                            depot.IsSelected = false;
                        }
                    }
                }
            }
        }
        public static bool TryGetAppNameOnline
        {
            get => DataSystem.Instance.TryGetAppNameOnline;
            set => DataSystem.Instance.TryGetAppNameOnline = value;
        }
        public static bool GetDepotOnlyKey
        {
            get => DataSystem.Instance.GetDepotOnlyKey;
            set => DataSystem.Instance.GetDepotOnlyKey = value;
        }
        public string SelectPageAllDepotText
        {
            get
            {
                var msgTemplate = LocalizationService.GetString("Msg_SelectAllDepots");
                return string.Format(msgTemplate, pageItemCount);
            }
        }
        private string searchBarText;

        public string SearchBarText
        {
            get => searchBarText;
            set
            {
                searchBarText = value;
                OnPropertyChanged();
            }
        }
        public RelayCommand SearchButtonCmd { get; set; }
        private void SearchButtonClick()
        {
            if (SearchBarText != FilterText)
            {
                FilterText = SearchBarText;
            }
        }
        public RelayCommand EscButtonCmd { get; set; }
        private void EscButtonCmdPress()
        {
            if (ShowMoreInfo)
                ShowMoreInfo = false;
            if (SearchBar)
                SearchBar = false;
            else
            {
                SearchBarText = "";
                FilterText = "";
            }
        }

        public RelayCommand ImportFileSelectCmd { get; set; }
        private void ImportFileSelect()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "All files (*.*)|*.*|Manifest files (*.manifest)|*.manifest|Zip files (*.zip)|*.zip|Vdf files (*.vdf)|*.vdf",
                Multiselect = false
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string fileName = openFileDialog.FileName;
                string itemName = Path.GetFileNameWithoutExtension(fileName);
                WeakReferenceMessenger.Default.Send(new MouseDropFileMessage(itemName, fileName));
            }
        }
        public RelayCommand DisableSysUACCmd { get; set; }
        private void DisableSysUAC()
        {
            // 先尝试获取当前UAC状态
            var isUACEnabled = false;
            try
            {
                RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
                if (key?.GetValue("EnableLUA") is int value)
                {
                    isUACEnabled = value == 1;
                }
                key?.Close();
            }
            catch (Exception)
            {
                // 忽略异常
            }
            string message;
            if (isUACEnabled)
            {
                message = LocalizationService.GetString("Msg_UACConfirm");
            }
            else
            {
                message = LocalizationService.GetString("Msg_UACAlreadyDisabled");
                var infoTitle2 = LocalizationService.GetString("Common_Information");
                MessageBox.Show(message, infoTitle2, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            // 使用C#Win系统MessageBox弹窗询问用户是否确认禁用UAC
            var infoTitle = LocalizationService.GetString("Common_Information");
            var accept = MessageBox.Show(message,
                infoTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
            if (!accept) return;
            try
            {
                // 打开注册表子键，如果不存在则创建
                RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
                // 设置DWORD值
                key.SetValue("EnableLUA", 0, RegistryValueKind.DWord);
                key.Close();
                var successMsg = LocalizationService.GetString("Msg_UACDisabled");
                _ = OutAPI.MsgBox(successMsg, infoTitle);
            }
            catch (Exception ex)
            {
                var errorTitle = LocalizationService.GetString("Common_Warning");
                _ = OutAPI.MsgBox(ex.Message, errorTitle);
            }
        }
    }
}
