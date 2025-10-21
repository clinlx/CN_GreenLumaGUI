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
                    OnPropertyChanged(nameof(TryGetAppNameOnline));
                }
                if (m.kind == nameof(DataSystem.Instance.GetDepotOnlyKey))
                {
                    OnPropertyChanged(nameof(GetDepotOnlyKey));
                }
            });
            WeakReferenceMessenger.Default.Register<MouseDropFileMessage>(this, (r, m) =>
            {
                var steamPath = Path.GetDirectoryName(DataSystem.Instance.SteamPath);
                if (string.IsNullOrEmpty(steamPath))
                {
                    ManagerViewModel.Inform("Steam path is not set");
                    return;
                }
                if (Directory.Exists(m.path))
                {
                    var successNum = ImportDir(m.path);
                    ManagerViewModel.Inform($"Imported {successNum} files from the directory.");
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
                        ManagerViewModel.Inform($"Imported {successNum} files from the compressed package");
                        if (successNum > 0 && ManifestList is not null) ScanManifestList();
                        // 删除临时文件
                        Directory.Delete(tempUnzip, true);
                        File.Delete(tempZip);
                    }
                    catch (Exception ex)
                    {
                        ManagerViewModel.Inform($"Import Failed({ex.Message})");
                    }
                    return;
                }
                if (m.path.EndsWith(".manifest"))
                {
                    string manifestPath = Path.Combine(steamPath, "depotcache", Path.GetFileName(m.path));
                    if (File.Exists(manifestPath))
                    {
                        ManagerViewModel.Inform("Manifest file already exists");
                        return;
                    }
                    File.Copy(m.path, manifestPath, true);
                    ManagerViewModel.Inform("Manifest file imported");
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
        public string ScrollBarEchoState
        {
            get
            {
                if (DataSystem.Instance.ScrollBarEcho)
                    return "Visible";
                return "Hidden";
            }
        }
        // Add File
        public int ImportDir(string path)
        {
            var readyFiles = Directory.GetFiles(path).ToList();
            foreach (var dir in Directory.GetDirectories(path))
            {
                readyFiles.AddRange(Directory.GetFiles(dir));
            }
            return readyFiles.Count(file => ImportFile(Path.GetFileNameWithoutExtension(file), file, false));
        }
        public bool ImportFile(string name, string path, bool hasInform)
        {
            var steamPath = Path.GetDirectoryName(DataSystem.Instance.SteamPath);
            if (string.IsNullOrEmpty(steamPath)) return false;
            if (path.EndsWith(".zip") || path.EndsWith(".rar") || path.EndsWith(".7z"))
            {
                if (hasInform) ManagerViewModel.Inform("Importing this compression format is currently not supported");
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
                        searchAppCache ??= new();
                        foreach (var pair in info)
                        {
                            searchAppCache.TryAdd(pair.Key, new AppModelLite(pair.Value.Item1, pair.Key, "", "", pair.Value.Item2 <= 0, pair.Value.Item2));
                            DataSystem.Instance.SetDepotUnlock(pair.Key, true);
                        }
                        File.WriteAllText(DataSystem.gameInfoCacheFile, JsonConvert.SerializeObject(searchAppCache));
                        if (hasInform) ManagerViewModel.Inform("Description information file imported");
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
                    if (long.TryParse(depotIdStr, out var depotId))
                    {
                        // 复制文件
                        string depotCachePath = Path.Combine(steamPath, "depotcache");
                        if (!Directory.Exists(depotCachePath)) Directory.CreateDirectory(depotCachePath);
                        string manifestPath = Path.Combine(depotCachePath, name + ".manifest");
                        if (hasInform) ManagerViewModel.Inform(File.Exists(manifestPath) ? "Manifest file overwritten" : "Manifest file imported");
                        File.Copy(path, manifestPath, true);
                        // 修改vdf文件
                        var manifestIdStr = cuts[1];
                        SteamVdfHandler vdfHandler = new();
                        var res = vdfHandler.MergeManifestItem(depotIdStr, manifestIdStr);
                        vdfHandler.Save();
                        return true;
                    }
                }
                if (hasInform) ManagerViewModel.Inform("Incorrect manifest file name format");
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
                        if (hasInform) ManagerViewModel.Inform("Not resolved to a valid manifest key");
                        break;
                    default:
                        if (hasInform) ManagerViewModel.Inform($"{res} manifest keys has been merged");
                        return true;
                }
                return false;
            }
            if (hasInform) ManagerViewModel.Inform("Unsupported file type");
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
            string reason = "";
            var res = await Task.Run(async () =>
            {
                List<ManifestGameObj> save = new();
                ObservableCollection<ManifestGameObj> newList = new();
                try
                {
                    if (File.Exists(DataSystem.gameInfoCacheFile))
                    {
                        HashSet<long> usedUnlockIds = new();
                        var cacheStr = await File.ReadAllTextAsync(DataSystem.manifestListCacheFile);
                        save = (cacheStr!.FromJSON<List<ManifestGameObj>?>()) ?? save;
                        foreach (var item in save)
                        {
                            var game = new ManifestGameObj(item.GameName, item.GameId);
                            game.Installed = item.Installed;
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
                ManagerViewModel.Inform($"读取清单缓存失败，扫描开始: {reason}");
                ScanManifestList(checkNetWork);
            }
            else
            {
                ManifestList?.Clear();
                ManifestList = res;
                HashSet<long> usedUnlockIds = new();
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
                ManagerViewModel.Inform($"Scan Failed: {reason}");
            else
                WeakReferenceMessenger.Default.Send(new ManifestListChangedMessage(-1));
        }
        private async Task<(bool, string)> ScanFromSteam(bool checkNetWork = true)
        {
            if (string.IsNullOrEmpty(DataSystem.Instance.SteamPath))
            {
                return (false, "Steam path is not set");
            }
            var steamPath = Path.GetDirectoryName(DataSystem.Instance.SteamPath);
            if (string.IsNullOrEmpty(steamPath))
                return (false, "Wrong Steam Path!");
            var res = await Task.Run(async () =>
            {
                await Task.Yield();
                SteamAppFinder.Instance.Scan();
                // 检查网络
                bool hasNetWork = checkNetWork;
                if (TryGetAppNameOnline && hasNetWork)
                {
                    (_, SteamWebData.GetAppInfoState searchState) = await SteamWebData.Instance.GetAppInformAsync($"https://store.steampowered.com/app/228980/");
                    if (searchState == SteamWebData.GetAppInfoState.WrongNetWork) hasNetWork = false;
                }
                // 获取游戏列表
                var gameData = DataSystem.Instance.GetGameDatas();
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
                searchAppCache ??= new();
                Dictionary<long, object> dict = new();
                foreach (var game in gameData)
                {
                    var gameCopy = new ManifestGameObj(game.GameName, game.GameId);
                    dict.Add(game.GameId, gameCopy);
                    foreach (var dlc in game.DlcsList)
                    {
                        dict.Add(dlc.DlcId, dlc);
                    }
                }
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
                HashSet<long> depotIdExists = new();
                string depotCachePath = Path.Combine(steamPath, "depotcache");
                if (Directory.Exists(depotCachePath))
                {
                    var files = Directory.GetFiles(depotCachePath);
                    lock (this)
                    {
                        FileTotal = files.Length + SteamAppFinder.Instance.DepotDecryptionKeys.Count;
                    }
                    async Task<(ManifestGameObj?, int)> TryAdd(long localDepotId, string mPath = "", bool withNetwork = true)
                    {
                        try
                        {

                            if (localDepotId < 230000) return (null, 1);
                            if (SteamAppFinder.Instance.Excluded.Contains(localDepotId)) return (null, 2);
                            if (!depotIdExists.Add(localDepotId))
                            {
                                if (!string.IsNullOrEmpty(mPath))
                                {
                                    if (dict.TryGetValue(localDepotId, out var obj))
                                    {
                                        if (obj is ManifestGameObj gameObj && !string.IsNullOrEmpty(mPath))
                                            gameObj.ManifestPath = mPath;
                                    }
                                }
                                return (null, 3);
                            }
                            var appid = localDepotId / 10 * 10;
                            if (SteamAppFinder.Instance.Excluded.Contains(appid)) return (null, 4);
                            if (dict.TryGetValue(appid, out var app))
                            {
                                if (app is ManifestGameObj game)
                                {
                                    if (game.GameId != localDepotId)
                                    {
                                        game.DepotList!.Add(new(game.GameName, localDepotId, game, mPath));
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrEmpty(mPath))
                                        {
                                            game.ManifestPath = mPath;
                                        }
                                        game.FindSelf = true;
                                    }
                                    return (game, 0);
                                }
                                if (app is DlcObj dlc)
                                {
                                    await TryAdd(dlc.Master!.GameId);
                                    if (dict.TryGetValue(dlc.Master!.GameId, out var masterObj) && masterObj is ManifestGameObj master)
                                    {
                                        master?.DepotList!.Add(new(dlc.DlcName, localDepotId, master, mPath));
                                        return (master, 0);
                                    }
                                    return (null, 10);
                                }
                                return (null, 5);
                            }
                            else
                            {
                                long parentId = 0;
                                string echoName = "";
                                // TODO: 增加改名缓存，允许对某个Depot改名并指定相应的父Depot，父亲Depot必须存在
                                // 云端查找缓存
                                if (!searchAppCache.TryGetValue(appid, out AppModelLite? appInfo))
                                {
                                    // 本地查找
                                    if (SteamAppFinder.Instance.FindGameByDepotId.TryGetValue(localDepotId, out var pGameId))
                                        parentId = pGameId;
                                    // 网络查找
                                    if (TryGetAppNameOnline && withNetwork && hasNetWork)
                                    {
                                        var storeUrl = $"https://store.steampowered.com/app/{appid}/";
                                        (AppModel? oriInfo, SteamWebData.GetAppInfoState err) = await SteamWebData.Instance.GetAppInformAsync(storeUrl);
                                        appInfo = oriInfo?.ToLite();
                                        if (err != SteamWebData.GetAppInfoState.WrongUrl && err != SteamWebData.GetAppInfoState.WrongNetWork)
                                            searchAppCache.Add(appid, appInfo);
                                    }
                                }
                                if (appInfo is not null)
                                {
                                    if (parentId == 0) parentId = appInfo.ParentId;
                                    echoName = appInfo.AppName;
                                }
                                if (parentId > 0 && parentId != localDepotId)
                                {
                                    await TryAdd(parentId);
                                    if (dict.TryGetValue(parentId, out var parentObj) && parentObj is ManifestGameObj master)
                                    {
                                        master.DepotList!.Add(new(echoName, localDepotId, master, mPath));
                                        return (master, 5);
                                    }
                                }
                                var game = new ManifestGameObj(echoName, appid);
                                if (game.GameId != localDepotId)
                                    game.DepotList!.Add(new(game.GameName, localDepotId, game, mPath));
                                else
                                    game.FindSelf = true;
                                dict.Add(appid, game);
                                return (game, 0);
                            }
                        }
                        catch (Exception e)
                        {
                            _ = OutAPI.MsgBox(e.Message, "Error");
                            return (null, -100);
                        }
                    }
                    foreach (string file in files)
                    {
                        if (file.EndsWith(".manifest"))
                        {
                            var name = Path.GetFileNameWithoutExtension(file);
                            var cuts = name.Split('_');
                            if (cuts.Length != 2) continue;
                            var depotId = long.Parse(cuts[0]);
                            await TryAdd(depotId, file);
                        }
                        lock (this)
                        {
                            FileDone++;
                        }
                    }
                    foreach (var keyPair in SteamAppFinder.Instance.DepotDecryptionKeys)
                    {
                        await TryAdd(keyPair.Key, "", GetDepotOnlyKey);
                        //if (keyPair.Key % 10 <= 5)
                        //{
                        //	var appid = keyPair.Key / 10 * 10;
                        //	if (dict.TryGetValue(appid, out var app))
                        //	{
                        //		if (app is ManifestGameObj game)
                        //		{
                        //			game.HasKey = true;
                        //		}
                        //	}
                        //}
                        lock (this)
                        {
                            FileDone++;
                        }
                    }
                }
                await File.WriteAllTextAsync(DataSystem.gameInfoCacheFile, JsonConvert.SerializeObject(searchAppCache));
                ObservableCollection<ManifestGameObj> newList = new();
                foreach (var item in dict.Values)
                {
                    if (item is not ManifestGameObj game) continue;
                    if (game is { FindSelf: false, DepotList.Count: 0 }) continue;
                    newList.Add(game);
                }
                await File.WriteAllTextAsync(DataSystem.manifestListCacheFile, JsonConvert.SerializeObject(newList));
                return newList;
            });
            ManifestList?.Clear();
            ManifestList = res;
            HashSet<long> usedUnlockIds = new();
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
        private bool isFilteredTextOutdated = true;
        private bool isFilteredListItemOutdated = true;
        public bool IsFilteredOrderOutdated { get; set; } = false;
        private ObservableCollection<ManifestGameObj> filtered = new();
        private List<ManifestGameObj> filteredListTemp;
        public ObservableCollection<ManifestGameObj> FilteredManifestList
        {
            get
            {
                if (ManifestList is null) return filtered;
                if (isFilteredListItemOutdated)
                {
                    isFilteredListItemOutdated = false;
                    filtered.Clear();
                    foreach (var game in ManifestList)
                    {
                        filtered.Add(game);
                    }
                }
                if (isFilteredTextOutdated)
                {
                    isFilteredTextOutdated = false;
                    foreach (var game in filtered)
                    {
                        if (!GetDepotOnlyKey)
                        {
                            bool needSkip = !(game.HasManifest || game.HasManifest);
                            foreach (var depot in game.DepotList)
                            {
                                if (!needSkip) break;
                                if (depot.HasManifest || depot.HasManifest) needSkip = false;
                            }
                            if (needSkip)
                            {
                                game.Hide = true;
                                continue;
                            }
                        }
                        var needFilter = true;
                        if (string.IsNullOrEmpty(FilterText))
                            needFilter = false;
                        else
                        {
                            if (game.TitleText.ToLower().Contains(FilterText.ToLower()))
                                needFilter = false;
                            else
                            {
                                foreach (var depot in game.DepotList)
                                {
                                    if (depot.DepotText.ToLower().Contains(FilterText.ToLower()))
                                        needFilter = false;
                                }
                            }
                        }
                        if (game.CheckItemCount > 0) needFilter = false;
                        game.Hide = needFilter;
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
                    filteredListTemp = manifestGame.ToList();
                    filtered.Clear();
                    foreach (var item in filteredListTemp)
                        filtered.Add(item);
                    filteredListTemp.Clear();
                }
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
        private void SearchBarButton()
        {
            SearchBar = !SearchBar;
            if (ShowMoreInfo) ShowMoreInfo = false;
            if (SearchBar) page.searchBarTextBox.Focus();
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
                if (FileTotal < 0) return "Ready to scan";
                if (FileTotal == 0) return "Scan complete";
                return $"{FileDone}/{FileTotal} Scanning";
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
                    return "Steam path is not set";
                }
                int count = ManifestList?.Count ?? -1;
                if (count == -1)
                    return "Scanning...";
                if (count == 0)
                    return "Nothing found.";
                if (count > 12)
                    return "No more depots available...";
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
        public bool TryGetAppNameOnline
        {
            get => DataSystem.Instance.TryGetAppNameOnline;
            set => DataSystem.Instance.TryGetAppNameOnline = value;
        }
        public bool GetDepotOnlyKey
        {
            get => DataSystem.Instance.GetDepotOnlyKey;
            set => DataSystem.Instance.GetDepotOnlyKey = value;
        }
        public string SelectPageAllDepotText => $"Select All Depot ({pageItemCount})";
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
                message =
                    "UAC (User Account Control) is a security feature in Windows systems. It prompts a confirmation dialog when a user attempts to run a program with administrator privileges.\n" +
                    "However, it can also prevent certain versions of Windows from dragging and dropping files into the software window.\n" +
                    "Disabling UAC allows the \"drag and drop files into the software window\" feature to work normally, and the software will no longer prompt for authorization upon startup. However, this may introduce certain security risks.\n\n" +
                    "Do you confirm disabling UAC?\n" +
                    "If you click Yes, the software will help configure the registry to disable UAC.\r\n" +
                    "If you click No, no action will be taken.";
            }
            else
            {
                message =
                    "UAC (User Account Control) was already disabled on your computer,\n" +
                    "so no additional settings are required.";
                MessageBox.Show(message, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            // 使用C#Win系统MessageBox弹窗询问用户是否确认禁用UAC
            var accept = MessageBox.Show(message,
                "Warn", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
            if (!accept) return;
            try
            {
                // 打开注册表子键，如果不存在则创建
                RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
                // 设置DWORD值
                key.SetValue("EnableLUA", 0, RegistryValueKind.DWord);
                key.Close();
                _ = OutAPI.MsgBox("UAC has been successfully disabled. Please restart your computer to apply the changes.", "Info");
            }
            catch (Exception ex)
            {
                _ = OutAPI.MsgBox(ex.Message, "Error");
            }
        }
    }
}
