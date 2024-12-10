using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Linq;

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
		}
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
		private bool isExpanded = true;
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
		public string ManifestBarColor
		{
			get
			{
				if (checkNum > 0)
					return DataSystem.Instance.DarkMode ? "#AAAAAA" : "#FFFFFF";
				return DataSystem.Instance.DarkMode ? "#777777" : "#EEEEEE";
			}
		}
		[JsonIgnore]
		public string TitleText => $"{GameName} ({GameId})";
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
