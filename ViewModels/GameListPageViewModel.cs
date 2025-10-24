using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace CN_GreenLumaGUI.ViewModels
{
	public class GameListPageViewModel : ObservableObject
	{
		readonly GameListPage page;
		public GameListPageViewModel(GameListPage page, ObservableCollection<GameObj> gamesList)
		{
			this.page = page;
			this.gamesList = gamesList;
			DataSystem.Instance.LoadData();
			WeakReferenceMessenger.Default.Send(new CheckedNumChangedMessage(0, false));

			WeakReferenceMessenger.Default.Register<GameListChangedMessage>(this, (r, m) =>
			{
				OnPropertyChanged(nameof(PageEndText));
			});
			
			// 启动客户端时尝试清理
			try
			{
				if (File.Exists(GLFileTools.DLLInjectorExePath))
					File.Delete(GLFileTools.DLLInjectorExePath);
				if (File.Exists(GLFileTools.DLLInjectorExeBakPath))
					File.Delete(GLFileTools.DLLInjectorExeBakPath);
			}
			catch { }
			for (int i = 0; i < 10; i++)
			{
				var dllFileName = $"{GLFileTools.DLLInjectorConfigDir}\\GreenLuma{i}.dll";
				try
				{
					if (File.Exists(dllFileName))
						File.Delete(dllFileName);
				}
				catch { }
			}

				// 更新时尝试清除缓存
				if (DataSystem.Instance.LastVersion != "null" && DataSystem.Instance.LastVersion != Program.Version)
			{
				try
				{
					if (Directory.Exists(GLFileTools.DLLInjectorConfigDir))
						Directory.Delete(GLFileTools.DLLInjectorConfigDir, true);
					DataSystem.Instance.LastVersion = Program.Version;
				}
				catch
				{
					MessageBox.Show("由于文件被占用，此次更新未能生效，可能出现游戏无法正常解锁的情况。强制关闭Steam后重启软件，或者重启电脑可以解决此问题。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}
		//Cmd



		//Binding

		private ObservableCollection<GameObj> gamesList;

		public ObservableCollection<GameObj> GamesList
		{
			get { return gamesList; }
			set
			{
				gamesList = value;
				OnPropertyChanged();
			}
		}

		public string PageEndText
		{
			get
			{
				int count = DataSystem.Instance.GetGameDatas().Count;
				if (count == 0)
					return "还没有游戏哦，可以在搜索界面添加几个游戏。";
				if (count > 5)
					return "没有更多了……";
				return "";
			}
		}


	}
}
