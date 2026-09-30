using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.tools;
using CN_GreenLumaGUI.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CN_GreenLumaGUI.Models
{
	public class AppModel : ObservableObject
	{
		public AppModel()
		{
			Index = -1;
			AppImage = new BitmapImage();
			AppName = "";
			AppId = -1;
			AppSummary = "";
			AppStoreUrl = "https://store.steampowered.com/";
			IsGame = true;
			OpenWebInBrowser = new RelayCommand(OpenStoreWeb);
			ToggleButtonCmd = new RelayCommand(() =>
			{
				ChangeCheckedState(!IsChecked);
			});
			AddDlcCmd = new AsyncRelayCommand(AddDlcAsync);
			AddDlcCmd.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(AsyncRelayCommand.IsRunning)) RefreshDlcButtonState();
			};
			WeakReferenceMessenger.Default.Register<GameListChangedMessage>(this, (r, m) =>
			{
				if (AppId == m.gameId)
				{
					OnPropertyChanged(nameof(IsChecked));
				}
				if (ParentId == m.gameId) RefreshDlcButtonState();
			});
			WeakReferenceMessenger.Default.Register<DlcListChangedMessage>(this, (r, m) =>
			{
				if (AppId == m.dlcId) RefreshDlcButtonState();
			});
			WeakReferenceMessenger.Default.Register<ConfigChangedMessage>(this, (r, m) =>
			{
				if (m.kind == nameof(DataSystem.LanguageCode)) RefreshDlcButtonState();
			});
		}
		public AppModel(int index, string appImageUrl, string appName, long appId, string appSummaryString, string appStoreUrl) : this()
		{
			Index = index;
			AppImage = SteamWebData.GetImageFromUrl(appImageUrl);
			AppName = appName;
			AppId = appId;
			if (appSummaryString.Trim() == "")
				AppSummary = LocalizationService.GetString("Search_NoRating");
			else
				AppSummary = appSummaryString.Split('<')[0];
			AppStoreUrl = appStoreUrl;
		}
		//下标
		[JsonIgnore]
		public int Index { get; set; }
		//封面
		public BitmapSource AppImage { get; set; }
		//名字
		public string AppName { get; set; }
		//编号
		public long AppId { get; set; }
		//评价
		public string AppSummary { get; set; }
		//商店地址
		public string AppStoreUrl { get; set; }
		//类型
		private bool isGame = true;
		public bool IsGame
		{
			get => isGame;
			set
			{
				if (SetProperty(ref isGame, value)) RefreshDlcButtonState();
			}
		}
		//类型
		private long parentId;
		public long ParentId
		{
			get => parentId;
			set
			{
				if (SetProperty(ref parentId, value)) RefreshDlcButtonState();
			}
		}
		[JsonIgnore]
		public bool IsDlcAdded => !IsGame && ParentId > 0 &&
			DataSystem.Instance.GetGameObjFromId(ParentId)?.DlcsList.Any(dlc => dlc.DlcId == AppId) == true;
		[JsonIgnore]
		public bool CanAddDlc => !IsDlcAdded && !AddDlcCmd.IsRunning;
		[JsonIgnore]
		public string DlcButtonIcon => IsDlcAdded ? "Check" : HasParentGame ? "Plus" : "Alert";
		[JsonIgnore]
		public string DlcButtonColor => IsDlcAdded ? "#6B7280" : HasParentGame ? "#7356B8" : "#C2414C";
		[JsonIgnore]
		public string DlcButtonToolTip => AddDlcCmd.IsRunning ? LocalizationService.GetString("Search_DlcAdding") :
			IsDlcAdded ? LocalizationService.GetString("Search_DlcAlreadyAdded") :
			HasParentGame ? LocalizationService.GetString("Manual_TitleAddDLC") :
			LocalizationService.GetString("Search_DlcParentMissing");
		private bool HasParentGame => ParentId > 0 && DataSystem.Instance.IsGameExist(ParentId);
		private void RefreshDlcButtonState()
		{
			var dispatcher = Application.Current?.Dispatcher;
			if (dispatcher is not null && !dispatcher.CheckAccess())
			{
				dispatcher.BeginInvoke(new Action(RefreshDlcButtonState));
				return;
			}
			OnPropertyChanged(nameof(IsDlcAdded));
			OnPropertyChanged(nameof(CanAddDlc));
			OnPropertyChanged(nameof(DlcButtonIcon));
			OnPropertyChanged(nameof(DlcButtonColor));
			OnPropertyChanged(nameof(DlcButtonToolTip));
		}
		private async Task AddDlcAsync()
		{
			if (IsGame) return;
			try
			{
				if (ParentId <= 0)
				{
					var (app, _) = await SteamWebData.Instance.GetAppInformAsync(AppStoreUrl);
					if (app is null || app.IsGame || app.ParentId <= 0 || app.AppId != AppId)
					{
						ManagerViewModel.Inform(LocalizationService.GetString("Search_DlcParentLookupFailed"));
						return;
					}
					ParentId = app.ParentId;
				}
				var parent = DataSystem.Instance.GetGameObjFromId(ParentId);
				if (parent is null)
				{
					ManagerViewModel.Inform(LocalizationService.GetString("Search_DlcParentMissing"));
					return;
				}
				if (IsDlcAdded)
				{
					ManagerViewModel.Inform(LocalizationService.GetString("Search_DlcAlreadyAdded"));
					return;
				}
				var dlc = new DlcObj(AppName, AppId, parent);
				try
				{
					parent.DlcsList.Add(dlc);
					DataSystem.Instance.RegisterDlc(dlc);
					DataSystem.Instance.SaveData();
				}
				catch
				{
					parent.DlcsList.Remove(dlc);
					DataSystem.Instance.UnregisterDlc(dlc);
					throw;
				}
				ManagerViewModel.Inform(LocalizationService.GetString("Manual_DlcAdded"));
			}
			catch (Exception ex)
			{
				OutAPI.PrintLog(ex.Message);
				ManagerViewModel.Inform(string.Format(LocalizationService.GetString("Search_DlcAddFailedFormat"), ex.Message));
			}
			finally
			{
				RefreshDlcButtonState();
			}
		}
		//收藏按钮
		[JsonIgnore]
		public bool IsChecked
		{
			get { return DataSystem.Instance.IsGameExist(AppId); }
			set { /*ChangeCheckedState(value);*/ }
		}
		private void ChangeCheckedState(bool value)
		{
			if (!IsGame) return;
			if (IsChecked != value)
			{
				if (value)
				{
					DataSystem.Instance.AddGame(AppName, AppId, true, []);
					Task.Run(() =>
					{
						var theGame = DataSystem.Instance.GetGameObjFromId(AppId);
						if (theGame is not null)
							_ = SteamWebData.Instance.AutoAddDlcsAsync(theGame);
					});
				}
				else
				{
					var theGame = DataSystem.Instance.GetGameObjFromId(AppId);
					if (theGame is not null) DataSystem.Instance.RemoveGame(theGame);
				}

			}
		}

		//Cmd
		[JsonIgnore]
		public RelayCommand ToggleButtonCmd { get; set; }
		[JsonIgnore]
		public AsyncRelayCommand AddDlcCmd { get; }
		[JsonIgnore]
		public RelayCommand OpenWebInBrowser { get; set; }
		private void OpenStoreWeb()
		{
			OutAPI.OpenInBrowser(AppStoreUrl);
		}

		public AppModelLite ToLite()
		{
			return new AppModelLite(AppName, AppId, AppSummary, AppStoreUrl, IsGame, ParentId);
		}
	}
	public class AppModelLite
	{
		//名字
		public AppModelLite()
		{
			AppName = "";
			AppId = 0;
			AppSummary = "";
			AppStoreUrl = "";
			IsGame = false;
			ParentId = 0;
		}
		public AppModelLite(string appName, long appId, string appSummary, string appStoreUrl, bool isGame, long parentId)
		{
			AppName = appName;
			AppId = appId;
			AppSummary = appSummary;
			AppStoreUrl = appStoreUrl;
			IsGame = isGame;
			ParentId = parentId;
		}

		public string AppName { get; set; }
		//编号
		public long AppId { get; set; }
		//评价
		public string AppSummary { get; set; }
		//商店地址
		public string AppStoreUrl { get; set; }
		//类型
		public bool IsGame { get; set; }
		//类型
		public long ParentId { get; set; }
	}
}
