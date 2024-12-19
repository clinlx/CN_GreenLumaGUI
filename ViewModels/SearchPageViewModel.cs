using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
				ManagerViewModel.Inform("Please enter the search content first.");
				NowSearchState = SearchState.Static;
				return;
			}
			//存储输入框中的内容
			lastSearchBarText = SearchBarText.Trim();
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
					if (app.IsGame)
					{
						res.Add(app);
						AppsList = res;
						NowSearchState = SearchState.Finished;
					}
					else
					{
						ManagerViewModel.Inform("This is not a game URL.");
						NowSearchState = SearchState.Static;
					}
				}
				else
				{
					if (msg == SteamWebData.GetAppInfoState.WrongNetWork)
						ManagerViewModel.Inform("Failed to fetch game datas from the URL.");
					else
						ManagerViewModel.Inform($"Failed to retrieve data({msg})");
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
							ManagerViewModel.Inform("No matching game found");
						else
							ManagerViewModel.Inform("No more games available");
						//隐藏“下一页”按钮
						SearchPageNumNow = -1;
						break;
					}
					if (CircularLoadingBarVis != Visibility.Collapsed)
					{
						CircularLoadingBarVis = Visibility.Collapsed;
						LoadingBarVis = Visibility.Visible;
					}
					List<AppModel> newList = new(AppsList)
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
						ManagerViewModel.Inform("Failed to retrieve data during search");
						if (AppsList.Count == 0)
						{
							NowSearchState = SearchState.Static;
						}
					}
					else
						ManagerViewModel.Inform("Search paused");
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
					SearchState.Searching => "Pause search",
					SearchState.Stoped => "Resume search",
					SearchState.Finished => "Next page",
					_ => "Error",
				};
			}
		}


		private List<AppModel> appsList;
		public List<AppModel> AppsList
		{
			get
			{
				return appsList;
			}
			set
			{
				appsList = value;
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
