using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading.Tasks;
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
			WeakReferenceMessenger.Default.Register<GameListChangedMessage>(this, (r, m) =>
			{
				if (AppId == m.gameId)
				{
					OnPropertyChanged(nameof(IsChecked));
				}
			});
		}
		public AppModel(int index, string appImageUrl, string appName, long appId, string appSummaryString, string appStoreUrl) : this()
		{
			Index = index;
			AppImage = SteamWebData.GetImageFromUrl(appImageUrl);
			AppName = appName;
			AppId = appId;
			if (appSummaryString.Trim() == "")
				AppSummary = "暂无评价";
			else
				AppSummary = appSummaryString.Split('<')[0];
			AppStoreUrl = appStoreUrl;
		}
		//下标
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
		public bool IsGame { get; set; }
		//收藏按钮
		public bool IsChecked
		{
			get { return DataSystem.Instance.IsGameExist(AppId); }
			set { /*ChangeCheckedState(value);*/ }
		}
		private void ChangeCheckedState(bool value)
		{
			if (IsChecked != value)
			{
				if (value)
				{
					DataSystem.Instance.AddGame(AppName, AppId, true, new());
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
		public RelayCommand ToggleButtonCmd { get; set; }
		public RelayCommand OpenWebInBrowser { get; set; }
		private void OpenStoreWeb()
		{
			OutAPI.OpenInBrowser(AppStoreUrl);
		}

	}
}
