using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Windows;
using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.tools;
using CN_GreenLumaGUI.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Newtonsoft.Json;

namespace CN_GreenLumaGUI.Models
{
	public class DepotObj : ObservableObject
	{
		public string Name { get; set; }
		public long DepotId { get; set; }
		public DepotObj(string name, long depotId, ManifestGameObj master, string manifestPath)
		{
			Name = name;
			Master = master;
			DepotId = depotId;
			isSelected = false;
			ManifestPath = manifestPath;
			DownloadCommand = new RelayCommand(DownloadButtonClick);
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
		//[JsonIgnore]
		public string ManifestPath { get; set; }
		[JsonIgnore]
		public bool HasManifest => !string.IsNullOrEmpty(ManifestPath);
		[JsonIgnore]
		public Visibility HasManifestVisibility => HasManifest ? Visibility.Visible : Visibility.Collapsed;
		[JsonIgnore]
		public string HasManifestColor => HasKey ? "Green" : "DarkOrange";
		//[JsonIgnore]
		public bool HasKey { get; set; } = false;
		[JsonIgnore]
		public Visibility HasKeyVisibility => HasKey ? Visibility.Visible : Visibility.Collapsed;
		[JsonIgnore]
		public Visibility DownloadVisibility
		{
			get
			{
				if (!DataSystem.Instance.ShowManifestDownloadButton)
					return Visibility.Collapsed;
				return (HasManifest && HasKey) ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		//Binding
		private bool isSelected;
        [JsonIgnore]
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
				return LocalizationService.GetString("Depot_UnknownDepot");
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
        [JsonIgnore]
        public RelayCommand DownloadCommand { get; set; }
		public void DownloadButtonClick()
		{
			// 检查状态：必须Steam已经启动
			if (!ManagerViewModel.SteamRunning)
			{
				ManagerViewModel.Inform(LocalizationService.GetString("Common_DownloadTriggerSteamRunning"));
				return;
			}
			// 运行steam://install/<DepotId>
			var url = $"steam://install/{DepotId}";
			OutAPI.OpenInBrowser(url);
			ManagerViewModel.Inform(LocalizationService.GetString("Common_DownloadTriggerAttempt"));
		}

		public void Export(string zipPath)
		{
			if (!zipPath.EndsWith(".zip"))
			{
				_ = OutAPI.MsgBox(LocalizationService.GetString("Export_ZipOnly"), LocalizationService.GetString("Export_Failed"));
				return;
			}
			try
			{
				var tempDir = OutAPI.SystemTempDir;
				var depotTemp = Path.Combine(tempDir, "ZipFile", DepotId.ToString());
				if (Directory.Exists(depotTemp)) Directory.Delete(depotTemp, true);
				// 新建文件夹
				if (!Directory.Exists(depotTemp))
				{
					Directory.CreateDirectory(depotTemp);
				}
				// 输出manifest
				if (HasManifest)
				{
					var manifestName = Path.GetFileName(ManifestPath);
					File.Copy(ManifestPath, Path.Combine(depotTemp, manifestName), true);
				}
				// 输出key
				if (HasKey)
				{
					if (SteamAppFinder.Instance.DepotDecryptionKeys.TryGetValue(DepotId, out var decKey))
					{
						var obj = new VObject();
						obj.Add(DepotId.ToString(), new VObject { { "DecryptionKey", new VValue(decKey) } });
						var str = VdfConvert.Serialize(new VProperty("depots", obj));
						File.WriteAllText(Path.Combine(depotTemp, "Key.vdf"), str);
					}
				}
				// 输出应用信息
				Dictionary<long, (string, long)> appInfo = new() { { DepotId, (Name, Master?.GameId ?? -1) } };
				File.WriteAllText(Path.Combine(depotTemp, "info.json"), JsonConvert.SerializeObject(appInfo));
				// 使用C#的zip压缩库压缩
				using (var zip = new ZipArchive(File.Create(zipPath), System.IO.Compression.ZipArchiveMode.Create, true))
				{
					foreach (var file in Directory.GetFiles(depotTemp))
					{
						ZipArchiveEntry entry = zip.CreateEntry(Path.GetFileName(file));
						using Stream stream = entry.Open();
						using FileStream fileStream = File.OpenRead(file);
						fileStream.CopyTo(stream);
					}
				}
				// 删除临时文件夹
				Directory.Delete(depotTemp, true);
			}
			catch (Exception ex)
			{
				_ = OutAPI.MsgBox(ex.Message, LocalizationService.GetString("Export_Failed"));
			}
		}
	}
}
