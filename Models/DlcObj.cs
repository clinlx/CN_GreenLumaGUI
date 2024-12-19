using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.tools;
using CN_GreenLumaGUI.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using System.Data.Common;

namespace CN_GreenLumaGUI.Models
{
	public class DlcObj : ObservableObject
	{
		public string DlcName { get; set; }
		public long DlcId { get; set; }
		public DlcObj(string dlcName, long dlcId, GameObj master)
		{
			Master = master;
			DlcName = dlcName;
			DlcId = dlcId;
			isSelected = false;
			DeleteDlcCmd = new RelayCommand(DeleteDlc);
			EditDlcCmd = new RelayCommand(EditDlc);
		}

		[JsonIgnore]
		public GameObj? Master { get; set; }

		//Commands

		[JsonIgnore]
		public RelayCommand DeleteDlcCmd { get; set; }
		private void DeleteDlc()
		{
			if (Master == null) return;
			IsSelected = false;
			DataSystem.Instance.UnregisterDlc(this);
			Master.DlcsList.Remove(this);
			Master.UpdateCheckNum();
			Master = null;
			ManagerViewModel.Inform("DLC removed");
		}
		[JsonIgnore]
		public RelayCommand EditDlcCmd { get; set; }
		private void EditDlc()
		{
			WeakReferenceMessenger.Default.Send(new AppItemEditMessage(this));
			WeakReferenceMessenger.Default.Send(new SwitchPageMessage(2));
		}

		//Binding
		private bool isSelected;
		public bool IsSelected
		{
			get => isSelected;
			set
			{
				if (isSelected != value)
				{
					isSelected = value;
					if (value)
					{
						DataSystem.Instance.CheckedNumInc(DlcId);
					}
					else
					{
						DataSystem.Instance.CheckedNumDec(DlcId);
					}
					DataSystem.Instance.CheckDepotUnlockItem(DlcId);
				}
				Master?.UpdateCheckNum();
				OnPropertyChanged();
			}
		}

		[JsonIgnore]
		public string DlcText
		{
			get { return ToString(); }
		}

		//Funcs

		public void UpdateText()
		{
			OnPropertyChanged(nameof(DlcText));
		}

		public override string ToString()
		{
			return $"{DlcName} ({DlcId})";
		}
	}
}
