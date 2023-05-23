using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using CN_GreenLumaGUI.tools;

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
			dlcText = $"{dlcName} ({dlcId})";
		}

		[JsonIgnore]
		public GameObj? Master { get; set; }

		//Binding
		private bool isSelected;
		public bool IsSelected
		{
			get { return isSelected; }
			set
			{
				if (isSelected != value && DataSystem.Instance is not null)
				{
					if (value)
					{
						DataSystem.Instance.CheckedNumInc(DlcId);
					}
					else
					{
						DataSystem.Instance.CheckedNumDec(DlcId);
					}
				}
				isSelected = value;
				Master?.UpdateCheckNum();
			}
		}

		private string dlcText;
		[JsonIgnore]
		public string DlcText
		{
			get { return dlcText; }
			set
			{
				dlcText = value;
				OnPropertyChanged();
			}
		}
	}
}
