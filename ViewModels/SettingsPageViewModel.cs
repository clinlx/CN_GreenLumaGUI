using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CN_GreenLumaGUI.ViewModels
{
    public class SettingsPageViewModel : ObservableObject
    {
        //TODO: 启动Steam后自动关闭软件，开启软件若未启动自动启动steam。
        private readonly SettingsPage page;
        private readonly ObservableCollection<LanguageOption> languageOptions;
        private LanguageOption? selectedLanguage;
        public SettingsPageViewModel(SettingsPage page)
        {
            this.page = page;
            ChoseSteamPathCmd = new RelayCommand(ChoseSteamPath);
            ExpandAllGameListCmd = new RelayCommand(ExpandAllGameList);
            CloseAllGameListCmd = new RelayCommand(CloseAllGameList);
            ClearGameListCmd = new RelayCommand(ClearGameList);
            OpenGithubCmd = new RelayCommand(OpenGithub);
            OpenUpdateAddressCmd = new RelayCommand(OpenUpdateAddress);
            languageOptions = new ObservableCollection<LanguageOption>(LocalizationService.SupportedLanguages);
            UpdateSelectedLanguage();
            WeakReferenceMessenger.Default.Register<ConfigChangedMessage>(this, (r, m) =>
            {
                if (m.kind == nameof(DataSystem.Instance.SteamPath))
                {
                    OnPropertyChanged(nameof(SteamPathString));
                }
                if (m.kind == nameof(DataSystem.Instance.DarkMode))
                {
                    OnPropertyChanged(nameof(IsDarkTheme));
                }
                if (m.kind == nameof(DataSystem.Instance.StartWithBak))
                {
                    OnPropertyChanged(nameof(IsStartWithBak));
                }
                if (m.kind == nameof(DataSystem.Instance.ScrollBarEcho))
                {
                    OnPropertyChanged(nameof(IsEchoScrollBar));
                }
                if (m.kind == nameof(DataSystem.Instance.ModifySteamDNS))
                {
                    OnPropertyChanged(nameof(IsModifySteamDNS));
                }
                if (m.kind == nameof(DataSystem.Instance.RunSteamWithAdmin))
                {
                    OnPropertyChanged(nameof(IsRunSteamWithAdmin));
                }
                if (m.kind == nameof(DataSystem.Instance.NewFamilyModel))
                {
                    OnPropertyChanged(nameof(IsNewFamilyModel));
                }
                if (m.kind == nameof(DataSystem.Instance.ClearSteamAppCache))
                {
                    OnPropertyChanged(nameof(IsClearSteamAppCache));
                }
                if (m.kind == nameof(DataSystem.Instance.SingleConfigFileMode))
                {
                    OnPropertyChanged(nameof(IsSingleConfigFileMode));
                }
                if (m.kind == nameof(DataSystem.LanguageCode))
                {
                    UpdateSelectedLanguage();
                }

            });
            WeakReferenceMessenger.Default.Register<PageChangedMessage>(this, (r, m) =>
            {
                OnPropertyChanged(nameof(OpenUpdateBtnVisibility));
            });
        }
        //Cmd
        public RelayCommand ChoseSteamPathCmd { get; set; }
        private void ChoseSteamPath()
        {
            SteamPathString = GLFileTools.GetSteamPath_UserChose();
        }
        public RelayCommand ExpandAllGameListCmd { get; set; }
        private void ExpandAllGameList()
        {
            WeakReferenceMessenger.Default.Send(new ExpandedStateChangedMessage(true));
        }
        public RelayCommand CloseAllGameListCmd { get; set; }
        private void CloseAllGameList()
        {
            WeakReferenceMessenger.Default.Send(new ExpandedStateChangedMessage(false));
        }
        public RelayCommand ClearGameListCmd { get; set; }
        private void ClearGameList()
        {
            var warning = string.Format(LocalizationService.GetString("Settings_ClearDataWarning"), OutAPI.TempDir);
            MessageBox.Show(warning,
                            LocalizationService.GetString("Common_Warning"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
            //点击确定打开目录
            if (MessageBox.Show(LocalizationService.GetString("Settings_OpenDataFolderPrompt"),
                                LocalizationService.GetString("Common_Information"),
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Process.Start("explorer.exe", OutAPI.TempDir);
            }
        }
        public RelayCommand OpenGithubCmd { get; set; }
        private void OpenGithub()
        {
            OutAPI.OpenInBrowser("https://github.com/clinlx/CN_GreenLumaGUI");
        }
        public RelayCommand OpenUpdateAddressCmd { get; set; }
        volatile bool inGetAddr = false;
        private async void OpenUpdateAddress()
        {
            if (inGetAddr) return;
            inGetAddr = true;
            OnPropertyChanged(nameof(OpenUpdateBtnVisibility));
            var url = (await SteamWebData.Instance.GetServerDownLoadObj())?.DownUrl ?? null;
            if (url != null && url != "None")
            {
                OutAPI.OpenInBrowser(url);
                ManagerViewModel.Inform(LocalizationService.GetString("Settings_UpdateOpeningBrowser"));
                await Task.Delay(5000);
            }
            else
            {
                ManagerViewModel.Inform(LocalizationService.GetString("Settings_UpdateFailed"));
            }
            inGetAddr = false;
            OnPropertyChanged(nameof(OpenUpdateBtnVisibility));
        }
        //Bindings
        public Visibility OpenUpdateBtnVisibility
        {
            get
            {
                if (inGetAddr)
                    return Visibility.Hidden;
                if (!Program.NeedUpdate)
                    return Visibility.Hidden;
                return Visibility.Visible;
            }
        }
        public bool IsDarkTheme
        {
            get { return DataSystem.Instance.DarkMode; }
            set { DataSystem.Instance.DarkMode = value; }
        }
        public bool IsHidePromptText
        {
            get { return DataSystem.Instance.HidePromptText; }
            set { DataSystem.Instance.HidePromptText = value; }
        }
        public bool IsStartWithBak
        {
            get { return DataSystem.Instance.StartWithBak; }
            set { DataSystem.Instance.StartWithBak = value; }
        }
        public bool IsEchoScrollBar
        {
            get { return DataSystem.Instance.ScrollBarEcho; }
            set { DataSystem.Instance.ScrollBarEcho = value; }
        }
        public bool IsModifySteamDNS
        {
            get { return DataSystem.Instance.ModifySteamDNS; }
            set { DataSystem.Instance.ModifySteamDNS = value; }
        }
        public bool IsRunSteamWithAdmin
        {
            get { return DataSystem.Instance.RunSteamWithAdmin; }
            set { DataSystem.Instance.RunSteamWithAdmin = value; }
        }
        public bool IsNewFamilyModel
        {
            get
            {
                return DataSystem.Instance.NewFamilyModel;
            }
            set
            {
                if (value)
                {
                    MessageBox.Show(LocalizationService.GetString("Settings_NewFamilyModeWarning"),
                                    LocalizationService.GetString("Common_Warning"),
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                }
                DataSystem.Instance.NewFamilyModel = value;
            }
        }
        public bool IsClearSteamAppCache
        {
            get { return DataSystem.Instance.ClearSteamAppCache; }
            set { DataSystem.Instance.ClearSteamAppCache = value; }
        }
        public bool IsSingleConfigFileMode
        {
            get { return DataSystem.Instance.SingleConfigFileMode; }
            set { DataSystem.Instance.SingleConfigFileMode = value; }
        }

        public string SteamPathString
        {
            get { return DataSystem.Instance.SteamPath ?? ""; }
            set { DataSystem.Instance.SteamPath = value; }
        }
        public string ProgramVersion
        {
            get { return "v" + Program.Version; }
        }

        public ObservableCollection<LanguageOption> LanguageOptions => languageOptions;

        public LanguageOption? SelectedLanguage
        {
            get => selectedLanguage;
            set
            {
                if (value is null)
                    return;
                if (selectedLanguage?.Code == value.Code)
                    return;
                selectedLanguage = value;

                // 保存語言設定
                DataSystem.Instance.LanguageCode = value.Code;
                DataSystem.Instance.SaveData();
                // 即時提示語言已套用（不重啟）
                ManagerViewModel.Inform(LocalizationService.GetString("Settings_LanguageApplied"));

                OnPropertyChanged();
            }
        }

        private void UpdateSelectedLanguage()
        {
            var target = languageOptions.FirstOrDefault(option => option.Code == DataSystem.Instance.LanguageCode)
                         ?? languageOptions.FirstOrDefault();
            if (target == null)
                return;
            selectedLanguage = target;
            OnPropertyChanged(nameof(SelectedLanguage));
        }

        // 已改為即時切換語言，不再需要強制重啟邏輯
    }
}
