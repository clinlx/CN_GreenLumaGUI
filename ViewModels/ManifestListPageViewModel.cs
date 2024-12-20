using System;
using System.Collections.Generic;
using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Microsoft.Win32;

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
				OnPropertyChanged(nameof(PageEndText));
			});
			WeakReferenceMessenger.Default.Register<PageChangedMessage>(this, (r, m) =>
			{
				if (m.toPageIndex == 3)
				{
					if (!isProcess && ManifestList is null && !string.IsNullOrEmpty(DataSystem.Instance.SteamPath))
						ScanManifestList();
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
					ManagerViewModel.Inform("Steam路径未正确设置");
					return;
				}
				if (Directory.Exists(m.path))
				{
					var successNum = ImportDir(m.path);
					ManagerViewModel.Inform($"从目录中导入{successNum}个文件");
					if (successNum > 0 && ManifestList is not null) ScanManifestList();
					return;
				}
				if (m.path.EndsWith(".zip"))
				{
					// 解压到临时目录
					string tempDir = OutAPI.SystemTempDir;
					if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
					Directory.CreateDirectory(tempDir);
					string tempZip = Path.Combine(tempDir, Path.GetFileName(m.path));
					File.Copy(m.path, tempZip, true);
					string tempUnzip = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(m.path));
					// 使用C#自带的压缩解压功能
					using (var zip = ZipFile.OpenRead(tempZip))
					{
						zip.ExtractToDirectory(tempUnzip);
					}
					// 导入
					var successNum = ImportDir(tempUnzip);
					ManagerViewModel.Inform($"从压缩包中导入{successNum}个文件");
					if (successNum > 0 && ManifestList is not null) ScanManifestList();
					// 删除临时文件
					Directory.Delete(tempUnzip, true);
					File.Delete(tempZip);
					return;
				}
				if (m.path.EndsWith(".manifest"))
				{
					string manifestPath = Path.Combine(steamPath, "depotcache", Path.GetFileName(m.path));
					if (File.Exists(manifestPath))
					{
						ManagerViewModel.Inform("清单已存在");
						return;
					}
					File.Copy(m.path, manifestPath, true);
					ManagerViewModel.Inform("清单已导入");
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
				if (hasInform) ManagerViewModel.Inform("暂不支持导入该压缩格式");
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
						if (hasInform) ManagerViewModel.Inform("描述信息已导入");
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
						string depotCachePath = Path.Combine(steamPath, "depotcache");
						if (!Directory.Exists(depotCachePath)) Directory.CreateDirectory(depotCachePath);
						string manifestPath = Path.Combine(depotCachePath, name + ".manifest");
						if (hasInform) ManagerViewModel.Inform(File.Exists(manifestPath) ? "清单已覆盖" : "清单已导入");
						File.Copy(path, manifestPath, true);
						return true;
					}
				}
				if (hasInform) ManagerViewModel.Inform("清单文件名格式错误");
				return false;
			}
			if (path.EndsWith(".lua") || path.EndsWith(".vdf"))
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
						if (hasInform) ManagerViewModel.Inform("未解析到有效的清单密钥");
						break;
					default:
						if (hasInform) ManagerViewModel.Inform($"{res}个清单密钥已合并");
						return true;
				}
				return false;
			}
			if (hasInform) ManagerViewModel.Inform("不支持的文件类型。");
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
			OnPropertyChanged(nameof(FilteredManifestList));
			if (!result)
				ManagerViewModel.Inform($"获取清单失败: {reason}");
			else
				WeakReferenceMessenger.Default.Send(new ManifestListChangedMessage(-1));
		}
		private async Task<(bool, string)> ScanFromSteam(bool checkNetWork = true)
		{
			if (string.IsNullOrEmpty(DataSystem.Instance.SteamPath))
			{
				return (false, "Steam路径未设置，请在设置页面设置Steam路径。");
			}
			var steamPath = Path.GetDirectoryName(DataSystem.Instance.SteamPath);
			if (string.IsNullOrEmpty(steamPath))
				return (false, "Steam路径错误。");
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
						findSelf = true,
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
										game.findSelf = true;
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
									game.findSelf = true;
								dict.Add(appid, game);
								return (game, 0);
							}
						}
						catch (Exception e)
						{
							_ = OutAPI.MsgBox(e.Message, "错误");
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
					if (game is { findSelf: false, DepotList.Count: 0 }) continue;
					newList.Add(game);
				}
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
				OnPropertyChanged();
				OnPropertyChanged(nameof(FilteredManifestList));
				OnPropertyChanged(nameof(SearchBarButtonColor));
			}
		}
		public List<ManifestGameObj> FilteredManifestList
		{
			get
			{
				var filtered = new List<ManifestGameObj>();
				if (ManifestList is null) return filtered;
				foreach (var game in ManifestList)
				{
					if (!GetDepotOnlyKey)
					{
						bool needSkip = !(game.HasManifest || game.HasManifest);
						foreach (var depot in game.DepotList)
						{
							if (!needSkip) break;
							if (depot.HasManifest || depot.HasManifest) needSkip = false;
						}
						if (needSkip) continue;
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
					if (!needFilter) filtered.Add(game);
				}
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
				return manifestGame.ToList();
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
				if (FileTotal < 0) return "准备扫描";
				if (FileTotal == 0) return "扫描完成";
				return $"{FileDone}/{FileTotal} 正在扫描";
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
					return "Steam路径未设置，请在设置页面设置Steam路径。";
				}
				int count = ManifestList?.Count ?? -1;
				if (count == -1)
					return "正在扫描本地清单……";
				if (count == 0)
					return "未找到清单，可能Steam未登录或未下载过游戏。";
				if (count > 12)
					return "没有更多了……";
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
		public string SelectPageAllDepotText => $"全选全部 {pageItemCount} 个Depot";
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
					"UAC（用户账户控制）是Windows系统中的一个安全机制，它会在用户尝试以管理员权限运行程序时，弹出一个提示框要求用户确认是否允许。\n" +
					"但也会导致一些版本的Windows无法将文件拖动到软件窗口上。\n" +
					"禁用UAC功能可以让“将文件拖动到软件窗口”的功能正常，软件在启动时也不会再弹窗询问授权，但也会导致一些安全风险。\n\n" +
					"是否确认禁用UAC？\n" +
					"若点击是，则软件会帮忙配置注册表以禁用UAC功能。\n" +
					"若点击否，则不会执行任何操作。";
			}
			else
			{
				message =
					"发现此前你电脑中的 UAC（用户账户控制）已被禁用，\n" +
					"因此无需额外进行设置。";
				MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}
			// 使用C#Win系统MessageBox弹窗询问用户是否确认禁用UAC
			var accept = MessageBox.Show(message,
				"提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
			if (!accept) return;
			try
			{
				// 打开注册表子键，如果不存在则创建
				RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
				// 设置DWORD值
				key.SetValue("EnableLUA", 0, RegistryValueKind.DWord);
				key.Close();
				_ = OutAPI.MsgBox("已成功禁用UAC，请重启计算机以应用设置。", "提示");
			}
			catch (Exception ex)
			{
				_ = OutAPI.MsgBox(ex.Message, "错误");
			}
		}
	}
}
