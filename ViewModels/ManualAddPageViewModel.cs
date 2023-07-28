using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Windows;

namespace CN_GreenLumaGUI.ViewModels
{
	public class ManualAddPageViewModel : ObservableObject
	{
		private readonly ManualAddPage page;
		public ManualAddPageViewModel(ManualAddPage page)
		{
			this.page = page;
			CancelCmd = new RelayCommand(Cancel);
			SaveItemCmd = new RelayCommand(SaveItem);
			WeakReferenceMessenger.Default.Register<GameListChangedMessage>(this, (r, m) =>
			{
				OnPropertyChanged(nameof(GameSelectBoxList));
			});
			WeakReferenceMessenger.Default.Register<AppItemEditMessage>(this, (r, m) =>
			{
				SetDataSource(m.appitem);
			});
			WeakReferenceMessenger.Default.Register<PageChangedMessage>(this, (r, m) =>
			{
				if (m.fromPageIndex == 2 && m.toPageIndex != 2 && !AddModel)
					SetDataSource(null);
			});
		}
		private GameObj? gameDataSource = null;
		private DlcObj? dlcDataSource = null;
		private bool AddModel
		{
			get
			{
				return gameDataSource == null && dlcDataSource == null;
			}
		}
		public void SetDataSource(object? item)
		{
			switch (item)
			{
				case null:
					gameDataSource = null;
					dlcDataSource = null;
					ItemNameString = "";
					AppIdString = "";
					SelectedGameItem = null;
					break;
				case GameObj game:
					IsDlcAppItem = false;
					gameDataSource = game;
					dlcDataSource = null;
					ItemNameString = game.GameName;
					AppIdString = game.GameId.ToString();
					SelectedGameItem = null;
					break;
				case DlcObj dlc:
					IsDlcAppItem = true;
					gameDataSource = null;
					dlcDataSource = dlc;
					ItemNameString = dlc.DlcName;
					AppIdString = dlc.DlcId.ToString();
					SelectedGameItem = dlc.Master;
					break;
				default:
					throw new System.Exception("未知数据类型");
			}
			OnPropertyChanged(nameof(PageTitle));
			OnPropertyChanged(nameof(NameTextBoxTitle));

			OnPropertyChanged(nameof(IsDlcAppItem));
			OnPropertyChanged(nameof(ItemNameString));
			OnPropertyChanged(nameof(appIdString));
			OnPropertyChanged(nameof(ItemKindSwitchVisibility));
		}
		//Binding
		public string PageTitle
		{
			get
			{
				if (AddModel)
				{
					if (IsDlcAppItem)
						return "手动添加一个新DLC";
					return "手动添加一个新游戏";
				}
				else
				{
					if (IsDlcAppItem)
						return "修改DLC属性";
					return "修改游戏属性";
				}
			}
		}
		public string NameTextBoxTitle
		{
			get
			{
				if (IsDlcAppItem)
					return "DLC备注名:";
				return "游戏备注名:";
			}
		}
		public ObservableCollection<GameObj> GameSelectBoxList
		{
			get
			{
				return DataSystem.Instance.GetGameDatas();
			}
		}
		private string itemNameString = "";
		public string ItemNameString
		{
			get { return itemNameString; }
			set
			{
				itemNameString = value;
				OnPropertyChanged();
			}
		}

		private bool isDlcAppItem = false;

		public bool IsDlcAppItem
		{
			get { return isDlcAppItem; }
			set
			{
				isDlcAppItem = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(PageTitle));
				OnPropertyChanged(nameof(NameTextBoxTitle));
				OnPropertyChanged(nameof(DlcMasterVisibility));
			}
		}
		public Visibility ItemKindSwitchVisibility
		{
			get
			{
				if (AddModel)
					return Visibility.Visible;
				return Visibility.Collapsed;
			}
		}

		public Visibility DlcMasterVisibility
		{
			get
			{
				if (IsDlcAppItem)
					return Visibility.Visible;
				return Visibility.Collapsed;
			}
		}


		private GameObj? selectedGameItem = null;

		public GameObj? SelectedGameItem
		{
			get { return selectedGameItem; }
			set
			{
				selectedGameItem = value;
				OnPropertyChanged();
			}
		}


		private string appIdString = "";
		public string AppIdString
		{
			get { return appIdString; }
			set
			{
				appIdString = value;
				OnPropertyChanged();
			}
		}
		//Commands
		public RelayCommand CancelCmd { get; set; }
		private void Cancel()
		{
			SetDataSource(null);
		}
		public RelayCommand SaveItemCmd { get; set; }
		private void SaveItem()
		{
			if (string.IsNullOrEmpty(ItemNameString))
			{
				ManagerViewModel.Inform("备注名不能为空");
				return;
			}
			if (string.IsNullOrEmpty(AppIdString))
			{
				ManagerViewModel.Inform("AppID栏不能为空");
				return;
			}
			//确认输入的是不是网址
			var headerStr = AppIdString.Split('/')[0];
			if (headerStr == "https:" || headerStr == "http:")
			{
				//输入的是网址
				try
				{
					AppIdString = long.Parse(AppIdString.Split("/app/")[1].Split('/')[0]).ToString();
				}
				catch
				{
					ManagerViewModel.Inform("地址不正确");
					return;
				}
			}
			if (IsDlcAppItem && SelectedGameItem == null)
			{
				ManagerViewModel.Inform("请先选择一个要添加DLC游戏");
				return;
			}
			if (long.TryParse(AppIdString, out long appId))
			{
				if (AddModel)
				{
					//新增模式
					if (IsDlcAppItem)
						AddDlcForGame(ItemNameString, appId);
					else
						AddNewGame(ItemNameString, appId);
				}
				else
				{
					//覆盖编辑模式
					if (IsDlcAppItem)
						ChangeDlcInfo(ItemNameString, appId);
					else
						ChangeGameInfo(ItemNameString, appId);
				}
			}
			else
			{
				ManagerViewModel.Inform("无法解析出正确的数字AppID");
			}
		}
		//functions
		private void AddNewGame(string name, long id)
		{
			DataSystem.Instance.AddGame(name, id, false, new ObservableCollection<DlcObj>());
			ManagerViewModel.Inform("游戏添加成功");
			Cancel();
		}
		private void AddDlcForGame(string name, long id)
		{
			if (SelectedGameItem == null) return;
			DlcObj newDlc = new DlcObj(name, id, SelectedGameItem);
			SelectedGameItem.DlcsList.Add(newDlc);
			ManagerViewModel.Inform("DLC添加成功");
			Cancel();
		}
		private void ChangeGameInfo(string name, long id)
		{
			if (gameDataSource == null) return;
			gameDataSource.GameName = name;
			gameDataSource.GameId = id;
			WeakReferenceMessenger.Default.Send(new SwitchPageMessage(0));
			ManagerViewModel.Inform("游戏属性修改成功");
			Cancel();
		}
		private void ChangeDlcInfo(string name, long id)
		{
			if (dlcDataSource == null) return;
			if (SelectedGameItem == null) return;
			dlcDataSource.DlcName = name;
			dlcDataSource.DlcId = id;
			//修改Dlc归属	
			if (dlcDataSource.Master != null)
			{
				if (dlcDataSource.Master != SelectedGameItem)
				{
					dlcDataSource.Master.DlcsList.Remove(dlcDataSource);
					dlcDataSource.IsSelected = false;
					dlcDataSource.Master.UpdateCheckNum();
					dlcDataSource.Master = SelectedGameItem;
					SelectedGameItem.DlcsList.Add(dlcDataSource);
					SelectedGameItem.UpdateCheckNum();
				}
				else
				{
				}
				dlcDataSource.UpdateText();
			}
			WeakReferenceMessenger.Default.Send(new SwitchPageMessage(0));
			ManagerViewModel.Inform("DLC属性修改成功");
			Cancel();
		}
	}
}
