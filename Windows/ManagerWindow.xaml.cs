using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CN_GreenLumaGUI.ViewModels;
using System.Windows;
using System.Windows.Navigation;

namespace CN_GreenLumaGUI
{
	/// <summary>
	/// ManagerWindow.xaml 的交互逻辑
	/// </summary>
	public partial class ManagerWindow : Window
	{
		readonly ManagerViewModel viewModel;
		public ManagerWindow()
		{
			InitializeComponent();
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

		//SettingsPage
		private void Frame4_LoadCompleted(object sender, NavigationEventArgs e)
		{
			if (frame4.Content is not FrameworkElement content)
			{
				return;
			}
			if (content is SettingsPage page)
			{
				content.DataContext = new SettingsPageViewModel(page);
			}
		}
	}
}
