using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace CN_GreenLumaGUI.Models
{
	public class DepotObj : ObservableObject
	{
		public string Name { get; set; }
		public long DepotId { get; set; }
		public DepotObj(string name, long depotId, ManifestGameObj master)
		{
			Name = name;
			Master = master;
			DepotId = depotId;
			isSelected = false;
		}

		[JsonIgnore]
		public ManifestGameObj? Master { get; set; }

		//Binding
		private bool isSelected;
		public bool IsSelected
		{
			get => isSelected;
			set
			{
				if (isSelected != value && DataSystem.Instance is not null)
				{
					if (value)
					{
						DataSystem.Instance.CheckedNumInc(DepotId);
					}
					else
					{
						DataSystem.Instance.CheckedNumDec(DepotId);
					}
				}
				isSelected = value;
				Master?.UpdateCheckNum();
				OnPropertyChanged();
			}
		}

		[JsonIgnore]
		public string DepotText => ToString();

		//Funcs

		public void UpdateText()
		{
			OnPropertyChanged(nameof(DepotText));
		}

		public override string ToString()
		{
			return $"{Name} ({DepotId})";
		}
	}
}
