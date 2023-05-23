using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;
using System.Windows;

namespace CN_GreenLumaGUI.ViewModels
{
	public class SettingsPageViewModel : ObservableObject
	{
		//版权说明，Github地址，启动Steam后自动关闭软件，开启软件若未启动自动启动steam。
		private readonly SettingsPage page;
		public SettingsPageViewModel(SettingsPage page)
		{
			this.page = page;
			ChoseSteamPathCmd = new RelayCommand(ChoseSteamPath);
			ExpandAllGameListCmd = new RelayCommand(ExpandAllGameList);
			CloseAllGameListCmd = new RelayCommand(CloseAllGameList);
			ClearGameListCmd = new RelayCommand(ClearGameList);
			OpenGithubCmd = new RelayCommand(OpenGithub);
			WeakReferenceMessenger.Default.Register<ConfigChangedMessage>(this, (r, m) =>
			{
				if (m.kind == "SteamPath")
				{
					OnPropertyChanged(nameof(SteamPathString));
				}
				if (m.kind == "DarkMode")
				{
					OnPropertyChanged(nameof(IsDarkTheme));
				}
			});
		}
		//Cmd
		public RelayCommand ChoseSteamPathCmd { get; set; }
		private void ChoseSteamPath()
		{
			SteamPathString = GLFileTools.GetSteamPath_UserChose();
		}
		public RelayCommand ExpandAllGameListCmd { get; set; }
		private void ExpandAllGameList()
		{
			WeakReferenceMessenger.Default.Send(new ExpandedStateChangedMessage(true));
		}
		public RelayCommand CloseAllGameListCmd { get; set; }
		private void CloseAllGameList()
		{
			WeakReferenceMessenger.Default.Send(new ExpandedStateChangedMessage(false));
		}
		public RelayCommand ClearGameListCmd { get; set; }
		private void ClearGameList()
		{
			MessageBox.Show($"清空软件数据是一个危险动作，请手动操作。\n在关闭软件后，删除数据文件即可清空软件数据。\n【文件位置】{OutAPI.TempDir}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
			//点击确定打开目录
			if (MessageBox.Show("是否打开软件数据文件夹？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				Process.Start("explorer.exe", OutAPI.TempDir);
			}
		}
		public RelayCommand OpenGithubCmd { get; set; }
		private void OpenGithub()
		{
		}
		//Bindings
		public bool IsDarkTheme
		{
			get { return DataSystem.Instance.DarkMode; }
			set { DataSystem.Instance.DarkMode = value; }
		}
		public bool IsHidePromptText
		{
			get { return DataSystem.Instance.HidePromptText; }
			set { DataSystem.Instance.HidePromptText = value; }
		}
		public string SteamPathString
		{
			get { return DataSystem.Instance.SteamPath ?? ""; }
			set { DataSystem.Instance.SteamPath = value; }
		}

		public string ProgramVersion
		{
			get { return "v" + Program.Version; }
		}

	}
}
