using CommunityToolkit.Mvvm.ComponentModel;
using CN_GreenLumaGUI.Pages;

namespace CN_GreenLumaGUI.ViewModels
{
	public class ManualAddPageViewModel : ObservableObject
	{
		private readonly ManualAddPage page;
		public ManualAddPageViewModel(ManualAddPage page)
		{
			this.page = page;
		}

	}
}
