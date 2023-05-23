using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace CN_GreenLumaGUI.ViewModels
{
	public partial class ManagerViewModel : ObservableObject
	{
		readonly ManagerWindow windowFrom;
		public ManagerViewModel(ManagerWindow window)
		{
			windowFrom = window;
			CmdInit();
			DockInit();
		}
		/// <summary>
		/// 处理标题栏按钮command 
		/// </summary>
		void CmdInit()
		{
			CloseCmd = new RelayCommand(CloseFrom);
			MrCmd = new RelayCommand(MrFrom);
			MiniCmd = new RelayCommand(MiniFrom);
		}
		//Commands
		public RelayCommand? CloseCmd { get; set; }
		void CloseFrom()
		{
			windowFrom.Close();
		}
		public RelayCommand? MrCmd { get; set; }
		void MrFrom()
		{
			if (windowFrom.WindowState == WindowState.Normal)
			{
				windowFrom.WindowState = WindowState.Maximized;
			}
			else
			{
				windowFrom.WindowState = WindowState.Normal;
			}
		}

		public RelayCommand? MiniCmd { get; set; }
		void MiniFrom()
		{
			windowFrom.WindowState = WindowState.Minimized;
		}
	}
}
