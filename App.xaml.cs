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
            // 優先從系統註冊表讀取語言設置
            var sysLang = LocalizationService.LoadLanguageFromSystem();
            if (!string.IsNullOrWhiteSpace(sysLang))
            {
                // 先移除所有语言字典，确保干净
                LocalizationService.RemoveAllLanguageDictionaries();
                LocalizationService.ApplyLanguage(sysLang);
                // 同步到 DataSystem，防止其使用默認邏輯覆蓋
                DataSystem.Instance.LanguageCode = sysLang;
            }
            else
            {
                // 首次啟動，使用系統語言並保存到註冊表
                var defaultLang = LocalizationService.GetSystemLanguageCode();
                LocalizationService.RemoveAllLanguageDictionaries();
                LocalizationService.ApplyLanguage(defaultLang);
                LocalizationService.SaveLanguageToSystem(defaultLang);
                DataSystem.Instance.LanguageCode = defaultLang;
            }

			base.OnStartup(e);

            // 手動啟動主窗口，確保語言資源加載完畢後才創建窗口
            MainWindow = new MainWindow();
            MainWindow.Show();
		}
	}
}
