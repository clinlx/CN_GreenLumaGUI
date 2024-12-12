using System.Collections.Generic;
using System.Windows;
using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;

namespace CN_GreenLumaGUI.Models
{
	public class DepotObj : ObservableObject
	{
		public string Name { get; set; }
		public long DepotId { get; set; }
		public DepotObj(string name, long depotId, ManifestGameObj master, bool hasM = false)
		{
			Name = name;
			Master = master;
			DepotId = depotId;
			isSelected = false;
			HasManifest = hasM;
			HasKey = SteamAppFinder.Instance.DepotDecryptionKeys.ContainsKey(DepotId);

			WeakReferenceMessenger.Default.Register<DlcListChangedMessage>(this, (r, m) =>
			{
				if (m.dlcId == DepotId)
				{
					Master?.UpdateCheckNum();
					OnPropertyChanged(nameof(IsSelected));
				}
			});

			WeakReferenceMessenger.Default.Register<CheckedNumChangedMessage>(this, (r, m) =>
			{
				if (m.targetId == DepotId)
				{
					Master?.UpdateCheckNum();
					OnPropertyChanged(nameof(IsSelected));
				}
			});
		}

		[JsonIgnore]
		public ManifestGameObj? Master { get; set; }
		[JsonIgnore]
		public bool HasManifest { get; set; } = false;
		[JsonIgnore]
		public Visibility HasManifestVisibility => HasManifest ? Visibility.Visible : Visibility.Collapsed;
		[JsonIgnore]
		public string HasManifestColor => HasKey ? "Green" : "DarkOrange";
		[JsonIgnore]
		public bool HasKey { get; set; } = false;
		[JsonIgnore]
		public Visibility HasKeyVisibility => HasKey ? Visibility.Visible : Visibility.Collapsed;

		//Binding
		private bool isSelected;
		public bool IsSelected
		{
			get
			{
				var oriDlc = DataSystem.Instance.GetDlcObjFromId(DepotId);
				if (oriDlc is not null)
				{
					if (isSelected)
					{
						isSelected = false;
						DataSystem.Instance.SetDepotUnlock(DepotId, false);
						//DataSystem.Instance.CheckedNumDec(DepotId);
					}
					return oriDlc.IsSelected;
				}
				return isSelected;
			}
			set
			{
				var oriDlc = DataSystem.Instance.GetDlcObjFromId(DepotId);
				if (oriDlc is not null)
				{
					oriDlc.IsSelected = value;
					value = false;
				}
				if (isSelected != value)
				{
					isSelected = value;
					DataSystem.Instance.SetDepotUnlock(DepotId, value);
					if (value)
					{
						//DataSystem.Instance.CheckedNumInc(DepotId);
					}
					else
					{
						//DataSystem.Instance.CheckedNumDec(DepotId);
					}
				}
				Master?.UpdateCheckNum();
				OnPropertyChanged();
			}
		}
		[JsonIgnore]
		public string DepotName
		{
			get
			{
				if (!string.IsNullOrEmpty(Name)) return Name;
				return "未知Depot";
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
			return $"{DepotName} ({DepotId})";
		}
	}
}
