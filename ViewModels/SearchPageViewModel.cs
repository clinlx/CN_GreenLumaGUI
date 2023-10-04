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
		private readonly SearchPage page;
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
		private AppAsyncEnumerable gamesAsyncEnumerable;
		private IAsyncEnumerator<AppModel?> gamesAsyncEnumertor;
		private async Task ToSearch()
		{
			//搜索前确保输入框有内容
			if (string.IsNullOrEmpty(SearchBarText.Trim()))
			{
				ManagerViewModel.Inform("请先输入搜索内容");
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
				AppModel? app = await SteamWebData.Instance.GetAppInformAsync(lastSearchBarText);
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
						ManagerViewModel.Inform("这不是一个游戏本体的地址");
						NowSearchState = SearchState.Static;
					}
				}
				else
				{
					ManagerViewModel.Inform("从网址获取游戏数据失败");
					NowSearchState = SearchState.Static;
				}
				//隐藏加载条
				CircularLoadingBarVis = Visibility.Collapsed;
			}
			else
			{
				//输入的不是网址
				gamesAsyncEnumerable = SteamWebData.Instance.SearchGameAsync(lastSearchBarText);
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
							ManagerViewModel.Inform("未找到符合的游戏");
						else
							ManagerViewModel.Inform("没有更多了");
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
						ManagerViewModel.Inform("搜索时获取数据失败");
						if (AppsList.Count == 0)
						{
							NowSearchState = SearchState.Static;
						}
					}
					else
						ManagerViewModel.Inform("搜索暂停");
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
			gamesAsyncEnumerable = SteamWebData.Instance.SearchGameAsync(lastSearchBarText, SearchPageNumNow, appsList.Last().Index);
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
				switch (NowSearchState)
				{
					case SearchState.Static:
						return "Send";
					case SearchState.Searching:
						return "PauseOctagonOutline";
					case SearchState.Stoping:
						return "Clock";
					case SearchState.Stoped:
					case SearchState.Finished:
						return "Close";
					default:
						return "AlertCircle";
				}
			}
		}
		public Visibility FloatButtonVisibility
		{
			get
			{
				if (searchPageNumNow < 0)
					return Visibility.Collapsed;
				switch (NowSearchState)
				{
					case SearchState.Searching:
					case SearchState.Stoped:
					case SearchState.Finished:
						return Visibility.Visible;
				}
				return Visibility.Collapsed;
			}
		}

		public string FloatButtonIcon
		{
			get
			{
				if (searchPageNumNow < 0)
					return "AlertCircle";
				switch (NowSearchState)
				{
					case SearchState.Searching:
						return "Pause";
					case SearchState.Stoped:
						return "Magnify";
					case SearchState.Finished:
						return "PageNextOutline";
				}
				return "AlertCircle";
			}
		}

		public string FloatButtonText
		{
			get
			{
				if (searchPageNumNow < 0)
					return "Error";
				switch (NowSearchState)
				{
					case SearchState.Searching:
						return "暂停搜索";
					case SearchState.Stoped:
						return "继续搜索";
					case SearchState.Finished:
						return "下一页";
				}
				return "Error";
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
