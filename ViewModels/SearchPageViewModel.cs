using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CN_GreenLumaGUI.ViewModels
{
	public class SearchPageViewModel : ObservableObject
	{
		private readonly SearchPage page;
		public SearchPageViewModel(SearchPage page)
		{
			this.page = page;
			loadingBarVis = Visibility.Collapsed;
			searchBarText = "";
			appsList = new();
			SearchButtonCmd = new(SearchButtonClick);
			SearchMoreButtonCmd = new(SearchMoreButtonClick);
		}
		//Cmd
		public RelayCommand SearchButtonCmd { get; set; }
		private async void SearchButtonClick()
		{
			if (LoadingBarVis == Visibility.Visible) return;
			//搜索完成一次以后第二次变为清空数据按钮
			if (IsSearching)
			{
				AppsList = new();
				IsSearching = false;
				return;
			}
			else IsSearching = true;
			//搜索前确保输入框有内容
			if (string.IsNullOrEmpty(SearchBarText))
			{
				ManagerViewModel.Inform("请先输入搜索内容");
				IsSearching = false;
				return;
			}
			//显示加载条
			LoadingBarVis = Visibility.Visible;
			//确认输入的是不是网址
			var headerStr = SearchBarText.Split('/')[0];
			if (headerStr == "https:" || headerStr == "http:")
			{
				//输入的是网址
				var res = new List<AppModel>();
				AppModel? app = await SteamWebData.Instance.GetAppInformAsync(SearchBarText);
				if (app is not null)
				{
					res.Add(app);
					AppsList = res;
				}
				else
				{
					ManagerViewModel.Inform("从网址获取游戏数据失败");
					IsSearching = false;
				}
			}
			else
			{
				//输入的不是网址
				var res = await SteamWebData.Instance.SearchGameAsync(SearchBarText);
				if (res is not null)
				{
					AppsList = res;
					if (!res.Any())
					{
						ManagerViewModel.Inform("未找到符合的游戏");
					}
				}
				else
				{
					ManagerViewModel.Inform("搜索时获取数据失败");
					IsSearching = false;
				}
			}
			//隐藏加载条
			LoadingBarVis = Visibility.Collapsed;
		}
		private int searchPageNumNow = 1;
		public RelayCommand SearchMoreButtonCmd { get; set; }
		private async void SearchMoreButtonClick()
		{
			if (IsSearchingMore) return;
			IsSearchingMore = true;
			LoadingBarVis = Visibility.Visible;
			var res = await SteamWebData.Instance.SearchGameAsync(SearchBarText, searchPageNumNow, appsList.Last().Index);
			if (res is not null)
			{
				if (!res.Any())
				{
					ManagerViewModel.Inform("没有更多了");
					LoadingBarVis = Visibility.Collapsed;
					return;
				}
				else
				{
					searchPageNumNow++;
					AppsList = res;
				}
			}
			else
			{
				ManagerViewModel.Inform("搜索时获取数据失败");
			}
			//隐藏加载条
			LoadingBarVis = Visibility.Collapsed;
			IsSearchingMore = false;
		}

		//Binding
		private bool isSearching = false;
		public bool IsSearching
		{
			get
			{
				return isSearching;
			}
			set
			{
				isSearching = value;
				searchPageNumNow = 1;
				OnPropertyChanged(nameof(SearchButtonIcon));
				OnPropertyChanged(nameof(SearchMoreButtonVisibility));
			}
		}
		private bool isSearchingMore = false;
		public bool IsSearchingMore
		{
			get
			{
				return isSearchingMore;
			}
			set
			{
				isSearchingMore = value;
				OnPropertyChanged(nameof(SearchMoreButtonVisibility));
			}
		}
		public string SearchButtonIcon { get { return IsSearching ? "Close" : "Send"; } }
		public Visibility SearchMoreButtonVisibility { get { return LoadingBarVis != Visibility.Visible && IsSearching && !IsSearchingMore ? Visibility.Visible : Visibility.Collapsed; } }

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

		private Visibility loadingBarVis;

		public Visibility LoadingBarVis
		{
			get { return loadingBarVis; }
			set
			{
				loadingBarVis = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(SearchMoreButtonVisibility));
			}
		}

	}
}
