using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;

namespace CN_GreenLumaGUI.ViewModels
{
	public class GameListPageViewModel : ObservableObject
	{
		readonly GameListPage page;
		public GameListPageViewModel(GameListPage page, ObservableCollection<GameObj> gamesList)
		{
			this.page = page;
			this.gamesList = gamesList;
			DataSystem.Instance.LoadData();
			WeakReferenceMessenger.Default.Register<GameListChangedMessage>(this, (r, m) =>
			{
				OnPropertyChanged(nameof(PageEndText));
			});
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
				OnPropertyChanged();
			}
		}

		public string PageEndText
		{
			get
			{
				int count = DataSystem.Instance.GetGameDatas().Count;
				if (count == 0)
					return "There are no games available yet. You can add a few through the search interface.";
				if (count > 5)
					return "No more games available...";
				return "";
			}
		}


	}
}
