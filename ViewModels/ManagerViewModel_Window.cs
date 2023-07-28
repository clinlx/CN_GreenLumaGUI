using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
			WeakReferenceMessenger.Default.Register<SwitchPageMessage>(this, (r, m) =>
			{
				SelectPageNum = m.pageIndex;
			});
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
		//Bindings
		private int selectPageNum = 0;
		public int SelectPageNum
		{
			get
			{
				return selectPageNum;
			}
			set
			{
				WeakReferenceMessenger.Default.Send(new PageChangedMessage(selectPageNum, value));
				selectPageNum = value;
				OnPropertyChanged();
			}
		}
		public string ScrollBarEchoState
		{
			get
			{
				if (DataSystem.Instance.ScrollBarEcho)
					return "Visible";
				return "Hidden";
			}
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
