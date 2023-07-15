using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CN_GreenLumaGUI.ViewModels
{
	public class ManualAddPageViewModel : ObservableObject
	{
		private readonly ManualAddPage page;
		public ManualAddPageViewModel(ManualAddPage page)
		{
			this.page = page;
			WeakReferenceMessenger.Default.Register<GameListChangedMessage>(this, (r, m) =>
			{
				OnPropertyChanged(nameof(GameSelectBoxList));
			});
		}
		//Binding
		public List<string> GameSelectBoxList
		{
			get
			{
				List<string> gameSelectBoxList = new();
				for (int i = 0; i < DataSystem.Instance.GetGameDatas().Count; i++)
				{
					gameSelectBoxList.Add($"{DataSystem.Instance.GetGameDatas()[i].GameName}({DataSystem.Instance.GetGameDatas()[i].GameId})");
				}
				return gameSelectBoxList;
			}
		}
		//functions
		private void AddNewGame()
		{
			string gameName = "gamename";
			long gameId = 0;
			DataSystem.Instance.AddGame(gameName, gameId, false, new ObservableCollection<DlcObj>());
		}
		private void AddDlcForGame()
		{
			int gamePos = 0;
			string dlcName = "dlcname";
			long dlcId = 0;
			GameObj game = DataSystem.Instance.GetGameDatas()[gamePos];
			ObservableCollection<DlcObj> newDlcList = new(game.DlcsList);
			newDlcList.Add(new DlcObj(dlcName, dlcId, game));
			game.DlcsList = newDlcList;
		}
	}
}
