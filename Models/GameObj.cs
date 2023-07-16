using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.tools;
using CN_GreenLumaGUI.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Linq;

namespace CN_GreenLumaGUI.Models
{
	public class GameObj : ObservableObject
	{
		public GameObj(string gameNameInput, long gameIdInput, bool? isSelectedInput, ObservableCollection<DlcObj> dlcsListInput)
		{
			//属性
			gameName = gameNameInput;
			gameId = gameIdInput;
			isSelected = false;
			IsSelected = isSelectedInput ?? false;
			dlcsList = new();
			DlcsList = dlcsListInput;
			OnPropertyChanged(nameof(GameText));
			RefreshGameCmd = new RelayCommand(RefreshGameDlcs);
			DeleteGameCmd = new RelayCommand(DeleteGame);

			WeakReferenceMessenger.Default.Register<ConfigChangedMessage>(this, (r, m) =>
			{
				if (m.kind == "DarkMode")
				{
					OnPropertyChanged(nameof(GameBarColor));
				}
			});
			WeakReferenceMessenger.Default.Register<ExpandedStateChangedMessage>(this, (r, m) =>
			{
				IsExpanded = m.isExpanded;
			});
		}
		//辅助数值
		private int checkNum = 0;
		public void UpdateCheckNum()
		{
			checkNum = DlcsList.Count(x => x.IsSelected);
			OnPropertyChanged(nameof(SelectAll));
			OnPropertyChanged(nameof(GameBarColor));
		}

		//Cmd
		private bool isRefreshing = false;
		[JsonIgnore]
		public RelayCommand RefreshGameCmd { get; set; }
		private async void RefreshGameDlcs()
		{
			if (isRefreshing) return;
			isRefreshing = true;
			bool result = await SteamWebData.Instance.AutoAddDlcsAsync(this);
			isRefreshing = false;
			if (result)
				ManagerViewModel.Inform("游戏DLC列表已刷新");
			else
				ManagerViewModel.Inform("刷新失败: 无法获取游戏DLC列表");
		}

		[JsonIgnore]
		public RelayCommand DeleteGameCmd { get; set; }
		private void DeleteGame()
		{
			DataSystem.Instance.RemoveGame(this);
			ManagerViewModel.Inform("已删除游戏: " + gameName);
		}
		//Binding
		private string gameName;
		public string GameName
		{
			get
			{
				return gameName;
			}
			set
			{
				gameName = value;
				OnPropertyChanged();
			}
		}
		private long gameId;
		public long GameId
		{
			get
			{
				return gameId;
			}
			set
			{
				gameId = value;
				OnPropertyChanged(nameof(GameText));
			}
		}
		private bool isSelected;
		public bool IsSelected
		{
			get
			{
				return isSelected;
			}
			set
			{
				if (isSelected != value && DataSystem.Instance is not null)
				{
					if (value)
					{
						DataSystem.Instance.CheckedNumInc(GameId);
					}
					else
					{
						DataSystem.Instance.CheckedNumDec(GameId);
					}
				}
				isSelected = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(SelectAll));
				OnPropertyChanged(nameof(GameBarColor));
			}
		}
		[JsonIgnore]
		public bool? SelectAll
		{
			get
			{
				if (checkNum == 0) return false;
				if (checkNum == dlcsList.Count) return true;
				return null;
			}
			set
			{
				bool? newValue = value;
				if (newValue != null)
					foreach (var dlc in dlcsList)
					{
						dlc.IsSelected = (bool)newValue;
					}
				DlcsList = new ObservableCollection<DlcObj>(dlcsList);
			}
		}
		private bool isExpanded;
		[JsonIgnore]
		public bool IsExpanded
		{
			get
			{
				return isExpanded;
			}
			set
			{
				isExpanded = value;
				OnPropertyChanged();
			}
		}

		[JsonIgnore]
		public string GameBarColor
		{
			get
			{
				if (checkNum + (IsSelected ? 1 : 0) > 0)
					return DataSystem.Instance.DarkMode ? "#AAAAAA" : "#FFFFFF";
				return DataSystem.Instance.DarkMode ? "#777777" : "#EEEEEE";
			}
		}
		[JsonIgnore]
		public string GameText
		{
			get { return $"游戏APPID：{gameId}\n游戏DLC列表： " + ((DlcsList is null || DlcsList.Count <= 0) ? "[暂无]" : $"({DlcsList.Count}个)"); }
		}

		private ObservableCollection<DlcObj> dlcsList;

		public ObservableCollection<DlcObj> DlcsList
		{
			get { return dlcsList; }
			set
			{
				dlcsList = value;
				OnPropertyChanged(nameof(GameText));
				OnPropertyChanged();
			}
		}
	}
}
