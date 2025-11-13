using CN_GreenLumaGUI.tools;
using System.Windows;

namespace CN_GreenLumaGUI
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			LocalizationService.ApplyLanguage(DataSystem.Instance.LanguageCode);
		}
	}
}
