using System.Collections.Generic;
using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Gameloop.Vdf.Linq;
using Gameloop.Vdf;
using System.IO.Compression;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using CN_GreenLumaGUI.ViewModels;
using System.Diagnostics.Metrics;
using System.Xml.Linq;
using System.Threading;
using System;

namespace CN_GreenLumaGUI.Models
{
    public class ManifestGameObj : ObservableObject
    {
        public ManifestGameObj(string gameNameInput, long gameIdInput)
        {
            //属性
            gameName = gameNameInput;
            gameId = gameIdInput;
            depotList = new();
            ManifestPath = "";
            HasKey = SteamAppFinder.Instance.DepotDecryptionKeys.ContainsKey(gameId);
            DownloadCommand = new RelayCommand(DownloadButtonClick);
            ExportCommand = new RelayCommand(ExportButtonClick);
            OnPropertyChanged(nameof(SelectAllText));

            WeakReferenceMessenger.Default.Register<ConfigChangedMessage>(this, (r, m) =>
            {
                if (m.kind == "DarkMode")
                {
                    OnPropertyChanged(nameof(ManifestBarColor));
                }
            });

            WeakReferenceMessenger.Default.Register<GameListChangedMessage>(this, (r, m) =>
            {
                if (m.gameId == GameId)
                {
                    OnPropertyChanged(nameof(IsSelected));
                    OnPropertyChanged(nameof(ManifestBarColor));
                }
            });

            WeakReferenceMessenger.Default.Register<CheckedNumChangedMessage>(this, (r, m) =>
            {
                if (m.targetId == GameId)
                {
                    OnPropertyChanged(nameof(IsSelected));
                    OnPropertyChanged(nameof(ManifestBarColor));
                }
            });
        }
        [JsonIgnore]
        public bool IsTemp { get; set; } = false;
        [JsonIgnore]
        public bool FindSelf { get; set; } = false;
        //[JsonIgnore]
        public bool Installed { get; set; } = false;
        [JsonIgnore]
        public Visibility InstalledVisibility => Installed ? Visibility.Visible : Visibility.Collapsed;
        //[JsonIgnore]
        public string ManifestPath { get; set; }
        [JsonIgnore]
        public bool HasManifest => !string.IsNullOrEmpty(ManifestPath);
        [JsonIgnore]
        public Visibility HasManifestVisibility => HasManifest ? Visibility.Visible : Visibility.Collapsed;
        [JsonIgnore]
        public string HasManifestColor => HasKey ? "Green" : "DarkOrange";
        //[JsonIgnore]
        public bool HasKey { get; set; } = false;
        [JsonIgnore]
        public Visibility HasKeyVisibility => HasKey ? Visibility.Visible : Visibility.Collapsed;

        private bool hide = false;
        [JsonIgnore]
        public bool Hide { get => hide; set { hide = value; OnPropertyChanged(nameof(Visibility)); } }
        [JsonIgnore]
        public Visibility Visibility => Hide ? Visibility.Collapsed : Visibility.Visible;

        [JsonIgnore]
        public Visibility DownloadVisibility
        {
            get
            {
                if (!DataSystem.Instance.ShowManifestDownloadButton)
                    return Visibility.Collapsed;
                return CheckAnyDepotReady() ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        [JsonIgnore]
        public Visibility ExportVisibility => CheckAnyDepotReady() ? Visibility.Visible : Visibility.Collapsed;
        public bool CheckAnyDepotReady()
        {
            if (HasManifest && HasKey) return true;
            return DepotList.Count != 0 && DepotList.Any(x => x is { HasManifest: true, HasKey: true });
        }
        public void UpdateManifestPath(string manifestPath)
        {
            ManifestPath = manifestPath;
            OnPropertyChanged(nameof(HasManifest));
            OnPropertyChanged(nameof(HasManifestColor));
            OnPropertyChanged(nameof(HasManifestVisibility));
            OnPropertyChanged(nameof(DownloadVisibility));
            OnPropertyChanged(nameof(ExportVisibility));
            OnPropertyChanged(nameof(ManifestBarColor));
        }
        public void UpdateDepotList(List<DepotObj> depotList)
        {
            DepotList = new ObservableCollection<DepotObj>(depotList);
            OnPropertyChanged(nameof(ExportVisibility));
        }
        //辅助数值
        private int checkNum = 0;
        public void UpdateCheckNum()
        {
            checkNum = DepotList.Count(x => x.IsSelected);
            OnPropertyChanged(nameof(SelectAll));
            OnPropertyChanged(nameof(ManifestBarColor));
        }
        //Binding
        private string gameName;
        public string GameName
        {
            get => gameName;
            set
            {
                gameName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TitleText));
            }
        }
        [JsonIgnore]
        public string GameTitle
        {
            get
            {
                if (!string.IsNullOrEmpty(gameName)) return gameName;
                return SteamAppFinder.Instance.GameInstall.GetValueOrDefault(GameId, "未知游戏或DLC");
            }
        }
        private long gameId;
        public long GameId
        {
            get => gameId;
            set
            {
                gameId = value;
                OnPropertyChanged(nameof(SelectAllText));
                OnPropertyChanged(nameof(TitleText));
            }
        }
        public bool isSelected;
        [JsonIgnore]
        public bool IsSelected
        {
            get
            {
                var oriGame = DataSystem.Instance.GetGameObjFromId(GameId);
                if (oriGame is not null)
                {
                    if (isSelected)
                    {
                        isSelected = false;
                        DataSystem.Instance.SetDepotUnlock(GameId, false);
                        //DataSystem.Instance.CheckedNumDec(GameId);
                    }
                    return oriGame.IsSelected;
                }
                return isSelected;
            }
            set
            {
                var oriGame = DataSystem.Instance.GetGameObjFromId(GameId);
                if (oriGame is not null)
                {
                    oriGame.IsSelected = value;
                    value = false;
                }
                if (isSelected != value)
                {
                    isSelected = value;
                    DataSystem.Instance.SetDepotUnlock(GameId, value);
                    if (value)
                    {
                        //DataSystem.Instance.CheckedNumInc(GameId);
                    }
                    else
                    {
                        //DataSystem.Instance.CheckedNumDec(GameId);
                    }
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(ManifestBarColor));
            }
        }
        [JsonIgnore]
        public bool? SelectAll
        {
            get
            {
                if (checkNum == 0) return false;
                if (checkNum == depotList.Count) return true;
                return null;
            }
            set
            {
                bool? newValue = value;
                if (newValue != null)
                    foreach (var dlc in depotList)
                    {
                        dlc.IsSelected = (bool)newValue;
                    }
                //DlcsList = new ObservableCollection<DlcObj>(dlcsList);
            }
        }
        [JsonIgnore]
        public Visibility SelectAllVisibility => depotList.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        [JsonIgnore]
        public int CheckItemCount => checkNum + (IsSelected ? 1 : 0);
        [JsonIgnore]
        public string ManifestBarColor
        {
            get
            {
                if (CheckItemCount > 0)
                    return DataSystem.Instance.DarkMode ? "#AAAAAA" : "#FFFFFF";
                return DataSystem.Instance.DarkMode ? "#777777" : "#EEEEEE";
            }
        }
        [JsonIgnore]
        public string TitleText => $"{GameTitle} ({GameId})";
        [JsonIgnore]
        public string SelectAllText => $"全选Depots ({DepotList.Count}个)";

        private ObservableCollection<DepotObj> depotList;

        public ObservableCollection<DepotObj> DepotList
        {
            get => depotList;
            set
            {
                depotList = value;
                OnPropertyChanged(nameof(SelectAllText));
                OnPropertyChanged();
            }
        }
        public override string ToString()
        {
            return TitleText;
        }
        [JsonIgnore]
        public RelayCommand DownloadCommand { get; set; }
        public void DownloadButtonClick()
        {
            // 检查状态：必须Steam已经启动
            if (!ManagerViewModel.SteamRunning)
            {
                ManagerViewModel.Inform("仅在Steam启动时才可触发下载");
                return;
            }
            // 运行steam://install/<GameId>
            var url = $"steam://install/{GameId}";
            OutAPI.OpenInBrowser(url);
            ManagerViewModel.Inform("尝试触发下载(实际情况取决于清单状况)");
        }
        [JsonIgnore]
        public RelayCommand ExportCommand { get; set; }
        public void ExportButtonClick()
        {
            var trueTitle = GameTitle
                .Replace(":", "∶")
                .Replace("*", "＊")
                .Replace("?", "？")
                .Replace("\"", "\'")
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace("|", "_");
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = trueTitle,
                DefaultExt = ".zip",
                Filter = "Zip files (*.zip)|*.zip"
            };
            if (dialog.ShowDialog() != true) return;
            Export(dialog.FileName, true);
            ManagerViewModel.Inform("导出成功");
        }
        public void Export(string zipPath, bool includeSubDepots = false)
        {
            if (!zipPath.EndsWith(".zip"))
            {
                _ = OutAPI.MsgBox("只能导出为zip文件！", "导出失败");
                return;
            }
            SteamAppFinder.Instance.DepotDecryptionKeys.TryGetValue(GameId, out var gameDecKey);
            int totalManifests = 0, successManifests = 0, totalKeys = 0, successKeys = 0;
            try
            {
                var tempDir = OutAPI.SystemTempDir;
                var depotTemp = Path.Combine(tempDir, "ZipFile", GameId.ToString());
                if (Directory.Exists(depotTemp)) Directory.Delete(depotTemp, true);
                // 新建文件夹
                if (!Directory.Exists(depotTemp))
                {
                    Directory.CreateDirectory(depotTemp);
                }
                // 输出manifest
                if (HasManifest)
                {
                    totalManifests++;
                    if (File.Exists(ManifestPath))
                    {
                        var manifestName = Path.GetFileName(ManifestPath);
                        File.Copy(ManifestPath, Path.Combine(depotTemp, manifestName), true);
                        successManifests++;
                    }
                    else
                    {
                        ManifestPath = "";
                    }
                }
                if (includeSubDepots)
                {
                    // 输出其下的所有depot的manifest
                    foreach (DepotObj d in DepotList)
                    {
                        if (d.HasManifest)
                        {
                            totalManifests++;
                            if (File.Exists(d.ManifestPath))
                            {
                                var manifestName = Path.GetFileName(d.ManifestPath);
                                File.Copy(d.ManifestPath, Path.Combine(depotTemp, manifestName), true);
                                successManifests++;
                            }
                            else
                            {
                                d.ManifestPath = "";
                            }
                        }
                    }
                }
                // 输出应用信息
                Dictionary<long, (string, long)> appInfo = new() { { GameId, (GameName, -1) } };
                if (includeSubDepots)
                {
                    foreach (DepotObj d in DepotList)
                    {
                        appInfo.Add(d.DepotId, (d.Name, d.DepotId));
                    }
                }
                File.WriteAllText(Path.Combine(depotTemp, "info.json"), JsonConvert.SerializeObject(appInfo));
                // 输出key
                var obj = new VObject();
                if (HasKey)
                {
                    totalKeys++;
                    if (gameDecKey != null)
                    {
                        obj.Add(GameId.ToString(), new VObject { { "DecryptionKey", new VValue(gameDecKey.ToUpper()) } });
                        successKeys++;
                    }
                    else
                    {
                        HasKey = false;
                    }
                }
                if (includeSubDepots)
                {
                    // 输出其下的所有depot的key
                    foreach (DepotObj d in DepotList)
                    {
                        if (!d.HasKey) continue;
                        totalKeys++;
                        if (SteamAppFinder.Instance.DepotDecryptionKeys.TryGetValue(d.DepotId, out var dlcDecKey))
                        {
                            obj.Add(d.DepotId.ToString(), new VObject { { "DecryptionKey", new VValue(dlcDecKey.ToUpper()) } });
                            successKeys++;
                        }
                        else
                        {
                            d.HasKey = false;
                        }
                    }
                }
                var str = VdfConvert.Serialize(new VProperty("depots", obj));
                File.WriteAllText(Path.Combine(depotTemp, "Key.vdf"), str);
                // 输出lua脚本
                //string luaText = "";
                //if (HasKey && SteamAppFinder.Instance.DepotDecryptionKeys.TryGetValue(GameId, out var decGameKey))
                //{
                //	luaText += $"addappid({GameId},0,\"{decGameKey.ToLower()}\")\n";
                //}
                //else
                //{
                //	luaText += $"addappid({GameId})\n";
                //}
                //if (HasManifest)
                //{
                //	var manifestCode = Path.GetFileNameWithoutExtension(ManifestPath).Split('_').Last();
                //	luaText += $"setManifestid({GameId},\"{manifestCode}\")\n";
                //}
                //if (includeSubDepots)
                //{
                //	// 输出其下的所有depot的key
                //	foreach (DepotObj d in DepotList)
                //	{
                //		if (!d.HasKey) continue;
                //		if (!d.HasManifest) continue;
                //		if (SteamAppFinder.Instance.DepotDecryptionKeys.TryGetValue(d.DepotId, out var decDepotKey))
                //		{
                //			luaText += $"addappid({d.DepotId},0,\"{decDepotKey.ToLower()}\")\n";
                //		}
                //		else
                //		{
                //			luaText += $"addappid({d.DepotId})\n";
                //		}
                //		var manifestCode = Path.GetFileNameWithoutExtension(d.ManifestPath).Split('_').Last();
                //		luaText += $"setManifestid({d.DepotId},\"{manifestCode}\")\n";
                //	}
                //}
                //File.WriteAllText(Path.Combine(depotTemp, $"{GameId}.lua"), luaText);
                // 使用C#的zip压缩库压缩
                var zipPathTemp = Path.Combine(tempDir, "ZipFileTemp.zip");
                using (FileStream zipFileToOpen = new FileStream(zipPathTemp, FileMode.Create))
                {
                    using (var zip = new ZipArchive(zipFileToOpen, ZipArchiveMode.Create, true))
                    {
                        foreach (var file in Directory.GetFiles(depotTemp))
                        {
                            ZipArchiveEntry entry = zip.CreateEntry(Path.GetFileName(file));
                            using Stream stream = entry.Open();
                            using FileStream fileStream = File.OpenRead(file);
                            fileStream.CopyTo(stream);
                        }
                    }
                }
                File.Move(zipPathTemp, zipPath, true);
                // 删除临时文件夹
                Directory.Delete(depotTemp, true);
                // Msg
                if ((totalKeys + totalManifests - successManifests - successKeys) > 0)
                {
                    string msg = "";
                    if (totalManifests - successManifests > 0)
                        msg += $"导出时发现{totalManifests - successManifests}个清单文件丢失,\n";
                    if (totalKeys - successKeys > 0)
                        msg += $"导出时发现{totalManifests - successManifests}个密钥丢失,\n";
                    _ = OutAPI.MsgBox(msg + "可能部分内容已经被卸载！", "导出失败");
                }
            }
            catch (System.Exception ex)
            {
                _ = OutAPI.MsgBox(ex.Message, "导出失败");
            }
        }
    }
}
