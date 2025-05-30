﻿using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace CN_GreenLumaGUI.ViewModels
{
    public class SettingsPageViewModel : ObservableObject
    {
        //TODO: 启动Steam后自动关闭软件，开启软件若未启动自动启动steam。
        private readonly SettingsPage page;
        public SettingsPageViewModel(SettingsPage page)
        {
            this.page = page;
            ChoseSteamPathCmd = new RelayCommand(ChoseSteamPath);
            ExpandAllGameListCmd = new RelayCommand(ExpandAllGameList);
            CloseAllGameListCmd = new RelayCommand(CloseAllGameList);
            ClearGameListCmd = new RelayCommand(ClearGameList);
            OpenGithubCmd = new RelayCommand(OpenGithub);
            OpenUpdateAddressCmd = new RelayCommand(OpenUpdateAddress);
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
            MessageBox.Show($"清空软件数据是一个危险动作，请手动操作。\n在关闭软件后，删除数据文件即可清空软件数据。\n【文件位置】{OutAPI.TempDir}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            //点击确定打开目录
            if (MessageBox.Show("是否打开软件数据文件夹？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
                ManagerViewModel.Inform("正在跳转至浏览器。");
                await Task.Delay(5000);
            }
            else
            {
                ManagerViewModel.Inform("获取更新地址失败，请稍后重试。");
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
                    MessageBox.Show("使用此模式后，注意请勿使用解锁启动带有VAC反作弊系统的游戏!\n否则可能导致你在该游戏中遭到VAC封禁!\n所以请自行确认您游玩的游戏是否包含VAC!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
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
    }
}
