using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CN_GreenLumaGUI.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using IWshRuntimeLibrary;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace CN_GreenLumaGUI
{
	/// <summary>
	/// ManagerWindow.xaml 的交互逻辑
	/// </summary>
	public partial class ManagerWindow : Window
	{
		public static ManagerWindow? Instance { get; set; }
		public static ManagerViewModel? ViewModel => Instance?.viewModel;

		private readonly ManagerViewModel viewModel;
		public ManagerWindow()
		{
			InitializeComponent();
			Instance = this;
			viewModel = new ManagerViewModel(this);
			DataContext = viewModel;
			MinWidth = Width;//测试500
			MinHeight = Height;//测试325
			MaxWidth = SystemParameters.WorkArea.Width + 7 + 7;
			MaxHeight = SystemParameters.WorkArea.Height + 7 + 7;
		}

		//GameListPage
		private void Frame1_LoadCompleted(object sender, NavigationEventArgs e)
		{
			if (frame1.Content is not FrameworkElement content)
			{
				return;
			}
			if (content is GameListPage page)
			{
				content.DataContext = new GameListPageViewModel(page, DataSystem.Instance.GetGameDatas());
			}
		}

		//SearchPage
		private void Frame2_LoadCompleted(object sender, NavigationEventArgs e)
		{
			if (frame2.Content is not FrameworkElement content)
			{
				return;
			}
			if (content is SearchPage page)
			{
				content.DataContext = new SearchPageViewModel(page);
			}
		}

		//ManualAddPage
		private void Frame3_LoadCompleted(object sender, NavigationEventArgs e)
		{
			if (frame3.Content is not FrameworkElement content)
			{
				return;
			}
			if (content is ManualAddPage page)
			{
				content.DataContext = new ManualAddPageViewModel(page);
			}
		}

		//ManifestListPage
		private void Frame4_LoadCompleted(object sender, NavigationEventArgs e)
		{
			if (frame4.Content is not FrameworkElement content)
			{
				return;
			}
			if (content is ManifestListPage page)
			{
				content.DataContext = new ManifestListPageViewModel(page);
			}
		}

		//SettingsPage
		private void Frame5_LoadCompleted(object sender, NavigationEventArgs e)
		{
			if (frame5.Content is not FrameworkElement content)
			{
				return;
			}
			if (content is SettingsPage page)
			{
				content.DataContext = new SettingsPageViewModel(page);
			}
		}
		private bool isOverlayVisible;
		private void Grid_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				isOverlayVisible = true;
				e.Effects = DragDropEffects.Link;
				overlay.Visibility = Visibility.Visible;
			}
			else
			{
				isOverlayVisible = false;
				e.Effects = DragDropEffects.None;
				overlay.Visibility = Visibility.Collapsed;
			}
		}

		private async void Grid_Drop(object sender, DragEventArgs e)
		{
			if (isOverlayVisible)
			{
				isOverlayVisible = false;
				overlay.Visibility = Visibility.Collapsed;
			}
			try
			{
				var fileList = (System.Array?)e.Data.GetData(DataFormats.FileDrop);
				if (fileList is null) return;
				foreach (var fileObj in fileList)
				{
					var fileName = fileObj.ToString();
					if (string.IsNullOrWhiteSpace(fileName)) return;
					var itemPath = fileName;
					var itemName = "";
					// 快捷方式需要获取目标文件路径
					if (fileName.ToLower().EndsWith("lnk"))
					{
						var shell = new WshShell();
						var wshShortcut = (IWshShortcut)shell.CreateShortcut(fileName);
						itemPath = wshShortcut.TargetPath;
					}
					System.IO.FileInfo file = new(fileName);
					if (string.IsNullOrWhiteSpace(file.Extension))
					{
						itemName = file.Name;
					}
					else
					{
						if (file.Extension.Length > 0 && file.Extension.Length <= file.Name.Length)
							itemName = file.Name[..^file.Extension.Length];
					}
					// MessageBox.Show($"添加游戏：{itemName}({itemPath})");
					WeakReferenceMessenger.Default.Send(new MouseDropFileMessage(itemName, itemPath));
					await Task.Delay(500);
				}
			}
			catch (Exception ex)
			{
				_ = OutAPI.MsgBox(ex.Message);
			}
		}

		private void Grid_MouseMove(object sender, MouseEventArgs e)
		{
			if (isOverlayVisible)
			{
				isOverlayVisible = false;
				overlay.Visibility = Visibility.Collapsed;
			}
		}
	}
}
