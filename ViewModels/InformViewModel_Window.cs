using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Windows;

namespace CN_GreenLumaGUI.ViewModels
{
	public partial class InformViewModel : ObservableObject
	{
		readonly InformWindow windowFrom;
		public InformViewModel(InformWindow window, string title)
		{
			windowFrom = window;
			windowTitle = title;
			CmdInit();
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
		private string windowTitle;

		public string WindowTitle
		{
			get { return windowTitle; }
			set
			{
				windowTitle = value;
				OnPropertyChanged();
			}
		}
		private List<TextItemModel>? textLinesList;

		public List<TextItemModel>? TextLinesList
		{
			get { return textLinesList; }
			set
			{
				textLinesList = value;
				OnPropertyChanged();
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
