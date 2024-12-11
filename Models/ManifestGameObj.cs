using System.Collections.Generic;
using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace CN_GreenLumaGUI.Models
{
	public class ManifestGameObj : ObservableObject
	{
		public ManifestGameObj(string gameNameInput, long gameIdInput)
		{
			//属性
			gameName = gameNameInput;
			gameId = gameIdInput;
			depotList = new();
			OnPropertyChanged(nameof(SelectAllText));

			WeakReferenceMessenger.Default.Register<ConfigChangedMessage>(this, (r, m) =>
			{
				if (m.kind == "DarkMode")
				{
					OnPropertyChanged(nameof(ManifestBarColor));
				}
			});

			WeakReferenceMessenger.Default.Register<GameListChangedMessage>(this, (r, m) =>
			{
				if (m.gameId == GameId)
				{
					OnPropertyChanged(nameof(IsSelected));
					OnPropertyChanged(nameof(ManifestBarColor));
				}
			});

			WeakReferenceMessenger.Default.Register<CheckedNumChangedMessage>(this, (r, m) =>
			{
				if (m.targetId == GameId)
				{
					OnPropertyChanged(nameof(IsSelected));
					OnPropertyChanged(nameof(ManifestBarColor));
				}
			});
		}
		[JsonIgnore]
		public bool findSelf = false;
		[JsonIgnore]
		public bool Installed { get; set; } = false;
		[JsonIgnore]
		public Visibility InstalledVisibility => Installed ? Visibility.Visible : Visibility.Collapsed;
		[JsonIgnore]
		public bool HasManifest { get; set; } = false;
		[JsonIgnore]
		public Visibility HasManifestVisibility => HasManifest ? Visibility.Visible : Visibility.Collapsed;
		[JsonIgnore]
		public bool HasKey { get; set; } = false;
		[JsonIgnore]
		public Visibility HasKeyVisibility => HasKey ? Visibility.Visible : Visibility.Collapsed;
		//辅助数值
		private int checkNum = 0;
		public void UpdateCheckNum()
		{
			checkNum = DepotList.Count(x => x.IsSelected);
			OnPropertyChanged(nameof(SelectAll));
			OnPropertyChanged(nameof(ManifestBarColor));
		}
		//Binding
		private string gameName;
		public string GameName
		{
			get => gameName;
			set
			{
				gameName = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(TitleText));
			}
		}
		[JsonIgnore]
		public string GameTitle
		{
			get
			{
				if (!string.IsNullOrEmpty(gameName)) return gameName;
				return SteamAppFinder.Instance.GameInstall.GetValueOrDefault(GameId, "未知游戏或DLC");
			}
		}
		private long gameId;
		public long GameId
		{
			get => gameId;
			set
			{
				gameId = value;
				OnPropertyChanged(nameof(SelectAllText));
				OnPropertyChanged(nameof(TitleText));
			}
		}
		public bool isSelected;
		public bool IsSelected
		{
			get
			{
				var oriGame = DataSystem.Instance.GetGameObjFromId(GameId);
				if (oriGame is not null)
				{
					if (isSelected)
					{
						isSelected = false;
						DataSystem.Instance.SetDepotUnlock(GameId, false);
						//DataSystem.Instance.CheckedNumDec(GameId);
					}
					return oriGame.IsSelected;
				}
				return isSelected;
			}
			set
			{
				var oriGame = DataSystem.Instance.GetGameObjFromId(GameId);
				if (oriGame is not null)
				{
					oriGame.IsSelected = value;
					value = false;
				}
				if (isSelected != value)
				{
					isSelected = value;
					DataSystem.Instance.SetDepotUnlock(GameId, value);
					if (value)
					{
						//DataSystem.Instance.CheckedNumInc(GameId);
					}
					else
					{
						//DataSystem.Instance.CheckedNumDec(GameId);
					}
				}
				OnPropertyChanged();
				OnPropertyChanged(nameof(ManifestBarColor));
			}
		}
		[JsonIgnore]
		public bool? SelectAll
		{
			get
			{
				if (checkNum == 0) return false;
				if (checkNum == depotList.Count) return true;
				return null;
			}
			set
			{
				bool? newValue = value;
				if (newValue != null)
					foreach (var dlc in depotList)
					{
						dlc.IsSelected = (bool)newValue;
					}
				//DlcsList = new ObservableCollection<DlcObj>(dlcsList);
			}
		}
		[JsonIgnore]
		public Visibility SelectAllVisibility => depotList.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
		[JsonIgnore]
		public string ManifestBarColor
		{
			get
			{
				if (checkNum + (IsSelected ? 1 : 0) > 0)
					return DataSystem.Instance.DarkMode ? "#AAAAAA" : "#FFFFFF";
				return DataSystem.Instance.DarkMode ? "#777777" : "#EEEEEE";
			}
		}
		[JsonIgnore]
		public string TitleText => $"{GameTitle} ({GameId})";
		[JsonIgnore]
		public string SelectAllText => $"全选Depots ({DepotList.Count}个)";

		private ObservableCollection<DepotObj> depotList;

		public ObservableCollection<DepotObj> DepotList
		{
			get => depotList;
			set
			{
				depotList = value;
				OnPropertyChanged(nameof(SelectAllText));
				OnPropertyChanged();
			}
		}
		public override string ToString()
		{
			return TitleText;
		}
	}
}
