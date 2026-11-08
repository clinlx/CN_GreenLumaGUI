using CN_GreenLumaGUI.tools;
using System;
using System.Windows;

namespace CN_GreenLumaGUI.Windows
{
    public partial class StartupNoticeWindow : Window
    {
        public StartupNoticeWindow()
        {
            InitializeComponent();
            MaxWidth = SystemParameters.WorkArea.Width;
            MaxHeight = SystemParameters.WorkArea.Height;
            DoNotShowAgainCheckBox.IsChecked = DataSystem.Instance.HideStartupNotice;
        }

        private void Acknowledge_Click(object sender, RoutedEventArgs e) => Close();

        protected override void OnClosed(EventArgs e)
        {
            DataSystem.Instance.HideStartupNotice = DoNotShowAgainCheckBox.IsChecked == true;
            try
            {
                DataSystem.Instance.SaveData();
            }
            catch (Exception ex)
            {
                OutAPI.PrintLog($"Failed to save startup notice preference: {ex.Message}");
            }
            base.OnClosed(e);
        }
    }
}
