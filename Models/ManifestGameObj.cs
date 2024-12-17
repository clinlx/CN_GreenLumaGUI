﻿using System.Collections.Generic;
using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Gameloop.Vdf.Linq;
using Gameloop.Vdf;
using System.IO.Compression;
using System.IO;

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
			ManifestPath = "";
			HasKey = SteamAppFinder.Instance.DepotDecryptionKeys.ContainsKey(gameId);
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
		public string ManifestPath { get; set; }
		[JsonIgnore]
		public bool HasManifest => !string.IsNullOrEmpty(ManifestPath);
		[JsonIgnore]
		public Visibility HasManifestVisibility => HasManifest ? Visibility.Visible : Visibility.Collapsed;
		[JsonIgnore]
		public string HasManifestColor => HasKey ? "Green" : "DarkOrange";
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
		public void Export(string zipPath, bool includeSubDepots = false)
		{
			if (!zipPath.EndsWith(".zip"))
			{
				_ = OutAPI.MsgBox("只能导出为zip文件！", "导出失败");
				return;
			}
			try
			{
				var tempDir = OutAPI.SystemTempDir;
				var depotTemp = Path.Combine(tempDir, "ZipFile", GameId.ToString());
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
				if (includeSubDepots)
				{
					// 输出其下的所有depot的manifest
					foreach (DepotObj d in DepotList)
					{
						if (d.HasManifest)
						{
							var manifestName = Path.GetFileName(d.ManifestPath);
							File.Copy(d.ManifestPath, Path.Combine(depotTemp, manifestName), true);
						}
					}
				}
				// 输出key
				var obj = new VObject();
				if (HasKey)
				{
					if (SteamAppFinder.Instance.DepotDecryptionKeys.TryGetValue(GameId, out var decKey))
					{
						obj.Add(GameId.ToString(), new VObject { { "DecryptionKey", new VValue(decKey) } });
					}
				}
				if (includeSubDepots)
				{
					// 输出其下的所有depot的key
					foreach (DepotObj d in DepotList)
					{
						if (!d.HasKey) continue;
						if (SteamAppFinder.Instance.DepotDecryptionKeys.TryGetValue(d.DepotId, out var decKey))
						{
							obj.Add(d.DepotId.ToString(), new VObject { { "DecryptionKey", new VValue(decKey) } });
						}
					}
				}
				var str = VdfConvert.Serialize(new VProperty("depots", obj));
				File.WriteAllText(Path.Combine(depotTemp, "Key.vdf"), str);
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
			catch (System.Exception ex)
			{
				_ = OutAPI.MsgBox(ex.Message, "导出失败");
			}
		}
	}
}