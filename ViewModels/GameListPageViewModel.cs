using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace CN_GreenLumaGUI.ViewModels
{
    public class GameListPageViewModel : ObservableObject
    {
        /// <summary>排序模式，仅作用于显示层</summary>
        public const int SortDefault = 0;
        public const int SortNameAsc = 1;
        public const int SortNameDesc = 2;
        public const int SortIdAsc = 3;
        public const int SortIdDesc = 4;
        public const int SortCheckedAsc = 5;
        public const int SortCheckedDesc = 6;

        readonly GameListPage page;
        public GameListPageViewModel(GameListPage page, ObservableCollection<GameObj> gamesList)
        {
            this.page = page;
            this.gamesList = gamesList;
            if (!DataSystem.isLoaded)
            {
                DataSystem.Instance.LoadData();
            }
            // 独立的视图实例，不动共享的默认视图，避免影响其他页面
            gamesView = new ListCollectionView(gamesList);
            // 从持久化配置恢复排序模式（放在LoadData之后，否则读到的总是默认值0）
            sortMode = DataSystem.Instance.GameListSortMode;
            ApplySort();
            WeakReferenceMessenger.Default.Send(new CheckedNumChangedMessage(0, false));

            WeakReferenceMessenger.Default.Register<GameListChangedMessage>(this, (r, m) =>
            {
                OnPropertyChanged(nameof(PageEndText));
            });

            WeakReferenceMessenger.Default.Register<ConfigChangedMessage>(this, (r, m) =>
            {
                if (m.kind == nameof(DataSystem.Instance.LanguageCode))
                {
                    // 當語言變更時，更新使用本地化資源的屬性
                    OnPropertyChanged(nameof(PageEndText));
                    OnPropertyChanged(nameof(SortOptions));
                }
            });

            WeakReferenceMessenger.Default.Register<PageChangedMessage>(this, (r, m) =>
            {
                // 按勾选状态排序时不做实时重排(避免点击时条目跳动)，回到本页时再重排一次
                if (m.toPageIndex == 0 && m.fromPageIndex != 0 &&
                    (sortMode == SortCheckedAsc || sortMode == SortCheckedDesc))
                {
                    gamesView.Refresh();
                }
            });

            // 启动客户端时尝试清理
            try
            {
                if (File.Exists(GLFileTools.DLLInjectorExePath))
                    File.Delete(GLFileTools.DLLInjectorExePath);
                if (File.Exists(GLFileTools.DLLInjectorExeBakPath))
                    File.Delete(GLFileTools.DLLInjectorExeBakPath);
            }
            catch { }
            for (int i = 0; i < 10; i++)
            {
                var dllFileName = $"{GLFileTools.DLLInjectorConfigDir}\\GreenLuma{i}.dll";
                try
                {
                    if (File.Exists(dllFileName))
                        File.Delete(dllFileName);
                }
                catch { }
            }

#if !DEBUG
            // 更新时尝试清除缓存
            if (DataSystem.Instance.LastVersion != Program.Version)
            {
                try
                {
                    if (DataSystem.Instance.LastVersion != "null" && Directory.Exists(GLFileTools.DLLInjectorConfigDir))
                        Directory.Delete(GLFileTools.DLLInjectorConfigDir, true);
                    DataSystem.Instance.LastVersion = Program.Version;
                }
                catch
                {
                    MessageBox.Show(LocalizationService.GetString("Error_FileOccupied"), LocalizationService.GetString("Common_Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
#endif
        }
        //Cmd



        //Binding

        private ObservableCollection<GameObj> gamesList;

        public ObservableCollection<GameObj> GamesList
        {
            get { return gamesList; }
            set
            {
                gamesList = value;
                gamesView = new ListCollectionView(gamesList);
                ApplySort();
                OnPropertyChanged();
                OnPropertyChanged(nameof(GamesView));
            }
        }

        private ListCollectionView gamesView;
        /// <summary>界面实际绑定的视图，排序只在这一层生效</summary>
        public ListCollectionView GamesView => gamesView;

        private int sortMode = DataSystem.Instance.GameListSortMode;
        public int SortMode
        {
            get => sortMode;
            set
            {
                if (sortMode == value) return;
                sortMode = value;
                DataSystem.Instance.GameListSortMode = value;
                ApplySort();
                OnPropertyChanged();
            }
        }

        public List<string> SortOptions => new()
        {
            LocalizationService.GetString("GameList_SortDefault"),
            LocalizationService.GetString("GameList_SortNameAsc"),
            LocalizationService.GetString("GameList_SortNameDesc"),
            LocalizationService.GetString("GameList_SortIdAsc"),
            LocalizationService.GetString("GameList_SortIdDesc"),
            LocalizationService.GetString("GameList_SortCheckedAsc"),
            LocalizationService.GetString("GameList_SortCheckedDesc"),
        };

        private void ApplySort()
        {
            // 用一次 DeferRefresh 批量应用，避免多次触发界面重排
            using (gamesView.DeferRefresh())
            {
                gamesView.CustomSort = sortMode switch
                {
                    SortNameAsc => new GameComparer(SortNameAsc),
                    SortNameDesc => new GameComparer(SortNameDesc),
                    SortIdAsc => new GameComparer(SortIdAsc),
                    SortIdDesc => new GameComparer(SortIdDesc),
                    SortCheckedAsc => new GameComparer(SortCheckedAsc),
                    SortCheckedDesc => new GameComparer(SortCheckedDesc),
                    _ => null,// 默认：保持存储顺序
                };
            }
        }

        /// <summary>显示层排序比较器，不改动底层集合</summary>
        private sealed class GameComparer : System.Collections.IComparer
        {
            private readonly int mode;
            public GameComparer(int mode) => this.mode = mode;

            public int Compare(object? x, object? y)
            {
                if (x is not GameObj a || y is not GameObj b) return 0;
                int r = mode switch
                {
                    SortNameAsc or SortNameDesc =>
                        string.Compare(a.GameName, b.GameName, System.StringComparison.CurrentCultureIgnoreCase),
                    SortIdAsc or SortIdDesc => a.GameId.CompareTo(b.GameId),
                    // 勾选状态：正=已勾选在前，逆=未勾选在前
                    SortCheckedAsc or SortCheckedDesc => b.IsSelected.CompareTo(a.IsSelected),
                    _ => 0,
                };
                // 同序时用ID兜底，保证排序稳定，避免刷新后顺序抖动
                if (r == 0) r = a.GameId.CompareTo(b.GameId);
                else if (mode is SortNameDesc or SortIdDesc or SortCheckedDesc) r = -r;
                return r;
            }
        }

        public string PageEndText
        {
            get
            {
                int count = DataSystem.Instance.GetGameDatas().Count;
                if (count == 0)
                    return LocalizationService.GetString("GameList_NoGamesPrompt");
                if (count > 5)
                    return LocalizationService.GetString("GameList_NoMoreGames");
                return "";
            }
        }


    }
}
