using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CN_GreenLumaGUI.ViewModels
{
	public class SearchPageViewModel : ObservableObject
	{
		public readonly SearchPage page;
		public SearchPageViewModel(SearchPage page)
		{
			this.page = page;
			circularLoadingBarVis = Visibility.Collapsed;
			LoadingBarVis = Visibility.Hidden;
			searchBarText = "";
			appsList = new();
			SearchButtonCmd = new(SearchButtonClick);
			FloatButtonCmd = new(FloatButtonClick);
			KeyDownEnterCmd = new(KeyDownEnter);

			// 監聽語言變更事件
			WeakReferenceMessenger.Default.Register<ConfigChangedMessage>(this, (r, m) =>
			{
				if (m.kind == nameof(DataSystem.Instance.LanguageCode))
				{
					// 當語言變更時，更新使用資源的動態文字
					OnPropertyChanged(nameof(FloatButtonText));

					// 刷新 DataGrid 以更新列標題翻譯
					// 透過重新通知 AppsList 來強制 DataGrid 刷新
					OnPropertyChanged(nameof(AppsList));
				}
			});
		}

		enum SearchState { Static, Searching, Stoping, Stoped, Finished }

		//Cmd
		private SearchState searchState = SearchState.Static;
		private SearchState NowSearchState
		{
			get
			{
				return searchState;
			}
			set
			{
				searchState = value;
				OnPropertyChanged(nameof(SearchButtonIcon));
				OnPropertyChanged(nameof(FloatButtonVisibility));
				OnPropertyChanged(nameof(FloatButtonText));
				OnPropertyChanged(nameof(FloatButtonIcon));
			}
		}
		public RelayCommand SearchButtonCmd { get; set; }
		private async void SearchButtonClick()
		{
			switch (NowSearchState)
			{
				case SearchState.Static:
					//To Search
					NowSearchState = SearchState.Searching;
					SearchPageNumNow = 0;
					await ToSearch();
					break;
				case SearchState.Searching:
					//To Stop
					NowSearchState = SearchState.Stoping;
					gamesAsyncEnumerable?.CancelSearch();
					break;
				case SearchState.Stoping:
					//Waitting, do nothing
					return;
				case SearchState.Stoped:
				case SearchState.Finished:
					//搜索完成一次以后第二次变为清空数据按钮
					AppsList = new();
					NowSearchState = SearchState.Static;
					LoadingBarVis = Visibility.Hidden;
					return;
			}
		}
		private AppAsyncEnumerable? gamesAsyncEnumerable;
		private IAsyncEnumerator<AppModel?>? gamesAsyncEnumertor;
		private async Task ToSearch()
		{
			//搜索前确保输入框有内容
			if (string.IsNullOrEmpty(SearchBarText.Trim()))
			{
				ManagerViewModel.Inform(LocalizationService.GetString("Search_EnterSearchContent"));
				NowSearchState = SearchState.Static;
				return;
			}
			//存储输入框中的内容
			lastSearchBarText = SearchBarText.Trim();
			normalizedSearchText = NormalizeSearchText(lastSearchBarText);
			//确认输入的是不是网址
			var headerStr = lastSearchBarText.Split('/')[0];
			if (headerStr == "https:" || headerStr == "http:")
			{
				//显示加载条
				CircularLoadingBarVis = Visibility.Visible;
				//输入的是网址
				var res = new List<AppModel>();
				AppModel? app;
				(app, var msg) = await SteamWebData.Instance.GetAppInformAsync(lastSearchBarText);
				if (app is not null)
				{
					app.Index = 1;
					res.Add(app);
					AppsList = res;
					SearchPageNumNow = -1; // 链接查询只有一个结果，不提供下一页。
					NowSearchState = SearchState.Finished;
				}
				else
				{
					if (msg == SteamWebData.GetAppInfoState.WrongNetWork)
						ManagerViewModel.Inform(LocalizationService.GetString("Search_GetDataFromUrlFailed"));
					else
						ManagerViewModel.Inform(string.Format(LocalizationService.GetString("Search_GetDataFailedFormat"), msg));
					NowSearchState = SearchState.Static;
				}
				//隐藏加载条
				CircularLoadingBarVis = Visibility.Collapsed;
			}
			else
			{
				//输入的不是网址
				gamesAsyncEnumerable = SteamWebData.SearchGameAsync(lastSearchBarText);
				gamesAsyncEnumertor = gamesAsyncEnumerable.GetAsyncEnumerator();
				await ContinueSearch();
			}
		}
		private int searchPageNumNow = 0;
		private int SearchPageNumNow
		{
			get
			{
				return searchPageNumNow;
			}
			set
			{
				searchPageNumNow = value;
				OnPropertyChanged(nameof(FloatButtonVisibility));
				OnPropertyChanged(nameof(FloatButtonText));
				OnPropertyChanged(nameof(FloatButtonIcon));
			}
		}
		public RelayCommand FloatButtonCmd { get; set; }
		private async void FloatButtonClick()
		{
			switch (NowSearchState)
			{
				case SearchState.Searching:
					//To Stop
					NowSearchState = SearchState.Stoping;
					gamesAsyncEnumerable?.CancelSearch();
					break;
				case SearchState.Stoped:
					//To Continue
					await ContinueSearch();
					break;
				case SearchState.Finished:
					//To SearchMore
					await SearchMore();
					break;
			}
		}
		private async Task ContinueSearch()
		{
			if (gamesAsyncEnumerable is null) return;
			if (gamesAsyncEnumertor is null) return;
			//显示加载条
			CircularLoadingBarVis = Visibility.Visible;
			LoadingBarValue = 0;
			while (await gamesAsyncEnumertor.MoveNextAsync())
			{
				NowSearchState = SearchState.Searching;
				var res = gamesAsyncEnumertor.Current;
				if (res is not null)
				{
					if (res.Index == -1)
					{
						if (SearchPageNumNow == 0)
							ManagerViewModel.Inform(LocalizationService.GetString("Search_NoMatchingGames"));
						else
							ManagerViewModel.Inform(LocalizationService.GetString("Search_NoMoreResults"));
						//隐藏"下一页"按钮
						SearchPageNumNow = -1;
						break;
					}
					if (CircularLoadingBarVis != Visibility.Collapsed)
					{
						CircularLoadingBarVis = Visibility.Collapsed;
						LoadingBarVis = Visibility.Visible;
					}
					List<AppModel> newList = new(appsList)
					{
						res
					};
					AppsList = newList;
					LoadingBarValue = gamesAsyncEnumerable.GetProgress();
				}
				else
				{
					if (gamesAsyncEnumerable.StopBecauseNet)
					{
						ManagerViewModel.Inform(LocalizationService.GetString("Search_DataFetchFailed"));
						if (AppsList.Count == 0)
						{
							NowSearchState = SearchState.Static;
						}
					}
					else
						ManagerViewModel.Inform(LocalizationService.GetString("Search_Paused"));
					if (NowSearchState != SearchState.Static)
						NowSearchState = SearchState.Stoped;
					break;
				}
			}
			//隐藏加载条
			CircularLoadingBarVis = Visibility.Collapsed;
			if (NowSearchState == SearchState.Searching)
			{
				NowSearchState = SearchState.Finished;
				LoadingBarValue = 100;
			}
		}
		private async Task SearchMore()
		{
			if (searchPageNumNow < 0) return;
			if (NowSearchState != SearchState.Finished) return;
			if (string.IsNullOrEmpty(lastSearchBarText)) return;
			SearchPageNumNow++;
			gamesAsyncEnumerable = SteamWebData.SearchGameAsync(lastSearchBarText, SearchPageNumNow, appsList.Last().Index);
			gamesAsyncEnumertor = gamesAsyncEnumerable.GetAsyncEnumerator();
			await ContinueSearch();
		}

		public RelayCommand KeyDownEnterCmd { get; set; }
		private void KeyDownEnter()
		{
			switch (NowSearchState)
			{
				case SearchState.Static:
					SearchButtonClick();//搜索
					break;
				case SearchState.Stoped:
				case SearchState.Finished:
					SearchButtonClick();//清空内容
					SearchButtonClick();//搜索
					break;
			}
		}
		public string SearchButtonIcon
		{
			get
			{
				return NowSearchState switch
				{
					SearchState.Static => "Send",
					SearchState.Searching => "PauseOctagonOutline",
					SearchState.Stoping => "Clock",
					SearchState.Stoped or SearchState.Finished => "Close",
					_ => "AlertCircle",
				};
			}
		}
		public Visibility FloatButtonVisibility
		{
			get
			{
				if (searchPageNumNow < 0)
					return Visibility.Collapsed;
				return NowSearchState switch
				{
					SearchState.Searching or SearchState.Stoped or SearchState.Finished => Visibility.Visible,
					_ => Visibility.Collapsed,
				};
			}
		}

		public string FloatButtonIcon
		{
			get
			{
				if (searchPageNumNow < 0)
					return "AlertCircle";
				return NowSearchState switch
				{
					SearchState.Searching => "Pause",
					SearchState.Stoped => "Magnify",
					SearchState.Finished => "PageNextOutline",
					_ => "AlertCircle",
				};
			}
		}

		public string FloatButtonText
		{
			get
			{
				if (searchPageNumNow < 0)
					return "Error";
				return NowSearchState switch
				{
					SearchState.Searching => LocalizationService.GetString("Search_PauseSearch"),
					SearchState.Stoped => LocalizationService.GetString("Search_ContinueSearch"),
					SearchState.Finished => LocalizationService.GetString("Search_NextPage"),
					_ => "Error",
				};
			}
		}


		// 保留接收顺序用于分页，优先级排序只影响展示。
		private List<AppModel> appsList;
		private List<AppModel> prioritizedAppsList = new();
		private string normalizedSearchText = "";
		private static string NormalizeSearchText(string text)
		{
			return new string(text.Where(c => !char.IsWhiteSpace(c) && c != '_').ToArray());
		}
		private int GetSearchPriority(AppModel app)
		{
			if (normalizedSearchText.Length > 0)
			{
				string normalizedName = NormalizeSearchText(app.AppName);
				if (string.Equals(normalizedName, normalizedSearchText, StringComparison.OrdinalIgnoreCase)) return 0;
				if (normalizedSearchText.Length >= 5 && normalizedName.StartsWith(normalizedSearchText, StringComparison.OrdinalIgnoreCase)) return 1;
			}
			return app.IsGame ? 2 : 3;
		}
		public List<AppModel> AppsList
		{
			get
			{
				return prioritizedAppsList;
			}
			set
			{
				appsList = value;
				const int priorityLimit = 5;
				var topApps = new List<AppModel>();
				var exactMatches = new List<AppModel>();
				var prefixMatches = new List<AppModel>();
				var otherApps = new List<AppModel>();
				// 按 Steam 原始名次分组，避免并发加载顺序影响前三名和优先级名额。
				foreach (var app in appsList.OrderBy(app => app.SearchResultIndex))
				{
					if (app.SearchResultIndex is >= 1 and <= 3)
					{
						topApps.Add(app);
						continue;
					}
					int priority = GetSearchPriority(app);
					if (priority == 0 && exactMatches.Count < priorityLimit)
						exactMatches.Add(app);
					else if (priority == 1 && prefixMatches.Count < priorityLimit)
						prefixMatches.Add(app);
					else
						otherApps.Add(app);
				}
				// 超过名额的匹配项归入其他，同类型保留 Steam 原始顺序。
				prioritizedAppsList = topApps.Concat(exactMatches).Concat(prefixMatches)
					.Concat(otherApps.OrderBy(app => app.IsGame ? 0 : 1)).ToList();
				for (int i = 0; i < prioritizedAppsList.Count; i++)
					prioritizedAppsList[i].DisplayIndex = i + 1;
				OnPropertyChanged();
			}
		}
		private string? lastSearchBarText;

		private string searchBarText;

		public string SearchBarText
		{
			get { return searchBarText; }
			set
			{
				searchBarText = value;
				OnPropertyChanged();
			}
		}

		private Visibility circularLoadingBarVis;

		public Visibility CircularLoadingBarVis
		{
			get { return circularLoadingBarVis; }
			set
			{
				circularLoadingBarVis = value;
				OnPropertyChanged();
			}
		}


		private Visibility loadingBarVis;

		public Visibility LoadingBarVis
		{
			get { return loadingBarVis; }
			set
			{
				loadingBarVis = value;
				OnPropertyChanged();
			}
		}
		private int loadingBarValue;

		public int LoadingBarValue
		{
			get { return loadingBarValue; }
			set
			{
				loadingBarValue = value;
				OnPropertyChanged();
			}
		}

	}
}
