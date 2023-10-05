using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.ViewModels;
using System.Collections.Generic;
using System.Windows;

namespace CN_GreenLumaGUI.Windows
{
	/// <summary>
	/// InformWindow.xaml 的交互逻辑
	/// </summary>
	public partial class InformWindow : Window
	{
		private readonly InformViewModel viewModel;
		public InformWindow(string title, List<TextItemModel> content)
		{
			InitializeComponent();
			viewModel = new InformViewModel(this, title, content);
			DataContext = viewModel;
			MinWidth = Width;
			MinHeight = Height;
			MaxWidth = SystemParameters.WorkArea.Width + 7 + 7;
			MaxHeight = SystemParameters.WorkArea.Height + 7 + 7;
		}
	}
}
