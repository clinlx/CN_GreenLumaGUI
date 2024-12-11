using Gameloop.Vdf.Linq;
using Gameloop.Vdf;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace CN_GreenLumaGUI.tools
{
	class SteamAppFinder
	{
		private static readonly SteamAppFinder instance = new();
		public static SteamAppFinder Instance => instance;
		public Dictionary<long, string> DepotDecryptionKeys { get; } = new();
		public Dictionary<long, string> GameInstall { get; } = new();
		public Dictionary<long, long> FindGameByDepotId { get; } = new();
		public HashSet<long> Excluded = new() { };
		private SteamAppFinder()
		{
			Scan();
		}
		public void Scan()
		{
			DepotDecryptionKeys.Clear();
			List<string> libPaths = ScanLibrary();
			if (!string.IsNullOrEmpty(DataSystem.Instance.SteamPath))
			{
				var steamPath = Path.GetDirectoryName(DataSystem.Instance.SteamPath);
				if (!string.IsNullOrEmpty(steamPath))
				{
					try
					{
						var configFile = Path.Combine(steamPath, "config", "config.vdf");
						if (File.Exists(configFile))
						{
							var vdfText = File.ReadAllText(configFile).Split("\"WebStorage\"")[0] + "}";
							dynamic vdf = VdfConvert.Deserialize(vdfText);
							var depots = (VObject)vdf.Value.Software.Valve.Steam.depots;
							foreach (var depotPair in depots)
							{
								if (!long.TryParse(depotPair.Key, out var depotId)) continue;
								var depotValue = (VObject)depotPair.Value;
								if (depotValue.TryGetValue("DecryptionKey", out var key))
								{
									DepotDecryptionKeys[depotId] = key.ToString();
								}
							}
						}
					}
					catch
					{
						_ = OutAPI.MsgBox("Error while scanning Steam depot keys.");
					}
					var rootLib = Path.Combine(steamPath, "steamapps");
					if (Directory.Exists(rootLib))
						libPaths.Add(rootLib);
				}
			}
			foreach (string libPath in libPaths)
			{
				ScanApp(libPath);
			}
		}
		private List<string> ScanLibrary()
		{
			List<string> libPaths = new List<string>();
			DriveInfo[] s = DriveInfo.GetDrives();
			foreach (DriveInfo i in s)
			{
				if (i.DriveType != DriveType.Fixed) continue;
				string steamLibrary = Path.Combine(i.Name, "SteamLibrary", "steamapps");
				if (Directory.Exists(steamLibrary))
				{
					libPaths.Add(steamLibrary);
				}
			}
			return libPaths;
		}
		private void ScanApp(string libDir)
		{
			foreach (string file in Directory.GetFiles(libDir))
			{
				if (file.EndsWith(".acf"))
				{
					var name = Path.GetFileNameWithoutExtension(file);
					var cuts = name.Split('_');
					if (cuts.Length != 2) continue;
					if (cuts[0] != "appmanifest") continue;
					var appIdStr = cuts[1];
					if (!int.TryParse(appIdStr, out var appId)) continue;
					if (appId < 230000) continue;
					if (Excluded.Contains(appId)) continue;
					var vdfText = File.ReadAllText(file).Split("\"WebStorage\"")[0] + "}";
					dynamic vdf = VdfConvert.Deserialize(vdfText);
					string gameName = ((VToken)vdf.Value.name)?.ToString() ?? "";
					GameInstall[appId] = gameName;
					var depots = (VObject?)vdf.Value.InstalledDepots;
					if (depots != null)
					{
						foreach (var depotPair in depots)
						{
							if (!long.TryParse(depotPair.Key, out var depotId)) continue;
							// if (depotId == appId) continue;
							FindGameByDepotId[depotId] = appId;
						}
					}
				}
			}
		}
	}
}
