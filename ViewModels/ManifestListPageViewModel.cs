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
			this.manifestList = null;
			ScanManifestListCmd = new RelayCommand(ScanManifestList);
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
					OnPropertyChanged(nameof(PageEndText));
				}
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
		//Scan
		private bool isProcess = false;
		public RelayCommand ScanManifestListCmd { get; set; }
		private async void ScanManifestList()
		{
			if (isProcess) return;
			isProcess = true;
			(bool result, string reason) = await ScanFromSteam();
			isProcess = false;
			if (!result)
				ManagerViewModel.Inform($"获取清单失败: {reason}");
			else
				WeakReferenceMessenger.Default.Send(new ManifestListChangedMessage(-1));
		}

		private async Task<(bool, string)> ScanFromSteam()
		{
			if (string.IsNullOrEmpty(DataSystem.Instance.SteamPath))
			{
				return (false, "Steam路径未设置，请在设置页面设置Steam路径。");
			}
			var steamPath = Path.GetDirectoryName(DataSystem.Instance.SteamPath);
			if (string.IsNullOrEmpty(steamPath))
				return (false, "Steam路径错误。");
			var res = await Task.Run(() =>
			{
				var gameData = DataSystem.Instance.GetGameDatas();
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
				HashSet<long> depotIdExists = new();
				string depotCachePath = Path.Combine(steamPath, "depotcache");
				if (Directory.Exists(depotCachePath))
				{
					foreach (string file in Directory.GetFiles(depotCachePath))
					{
						if (file.EndsWith(".manifest"))
						{
							var name = Path.GetFileNameWithoutExtension(file);
							var cuts = name.Split('_');
							if (cuts.Length != 2) continue;
							var depotId = long.Parse(cuts[0]);
							if (!depotIdExists.Add(depotId)) continue;
							string manifestID = cuts[1];
							var appid = depotId / 10 * 10;
							if (dict.TryGetValue(appid, out var app))
							{
								if (app is ManifestGameObj game)
								{
									game.DepotList!.Add(new(game.GameName, depotId, game));
								}
								else if (app is DlcObj dlc)
								{
									var master = dict[dlc.Master!.GameId] as ManifestGameObj;
									master?.DepotList!.Add(new(dlc.DlcName, depotId, master));
								}
							}
							else
							{
								var game = new ManifestGameObj("未知游戏或DLC", appid);
								game.DepotList!.Add(new(game.GameName, depotId, game));
								dict.Add(appid, game);
							}
						}
					}
				}
				List<ManifestGameObj> newList = new();
				foreach (var item in dict.Values)
				{
					if (item is not ManifestGameObj game) continue;
					if (game.DepotList.Count == 0) continue;
					newList.Add(game);
				}
				return newList;
			});
			ManifestList?.Clear();
			ManifestList = res;
			return (true, string.Empty);
		}

		//Binding

		private List<ManifestGameObj>? manifestList;

		public List<ManifestGameObj>? ManifestList
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
				OnPropertyChanged();
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
				if (count > 5)
					return "没有更多了……";
				return "";
			}
		}
	}
}
