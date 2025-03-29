using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace CN_GreenLumaGUI.tools
{
	internal class SteamVdfHandler
	{
		public SteamVdfHandler()
		{
			var steamPath = Path.GetDirectoryName(DataSystem.Instance.SteamPath);
			tail = "}";
			Err = "";
			configFile = "";
			if (!string.IsNullOrEmpty(steamPath))
			{
				try
				{
					//configFile = "D:\\Downloads\\config.vdf";
					configFile = Path.Combine(steamPath, "config", "config.vdf");
					if (File.Exists(configFile))
					{
						var cuts = File.ReadAllText(configFile).Split("\"WebStorage\"");
						if (cuts.Length == 0) return;
						var vdfText = cuts[0] + "}";
						if (cuts.Length > 1) tail = cuts[1];
						content = VdfConvert.Deserialize(vdfText);
						if (TryDepots is null) _ = OutAPI.MsgBox("Failed to fetch the manifest key from the Steam configuration!");
					}
				}
				catch
				{
					_ = OutAPI.MsgBox("Error while scanning Steam depot keys.");
				}
			}
		}
		private readonly string configFile;
		private readonly VProperty? content;
		private readonly string tail;
		private dynamic? depotsNodeParent = null;
		public string Err { get; set; }
		public bool Success => content is not null;
		private VObject? TryDepots
		{
			get
			{
				var root = (content as dynamic)?.Value;
				if (root is null) return null;
				var softwareNode = root?.Software ?? root?.software;
				var valveNode = softwareNode?.Valve ?? softwareNode?.valve;
				var depotsNode = valveNode?.depots ?? valveNode?.Depots;
				if (depotsNode is not null)
				{
					depotsNodeParent = valveNode;
					return depotsNode;
				}
				var steamNode = valveNode?.Steam ?? valveNode?.steam;
				depotsNode = steamNode?.depots ?? steamNode?.Depots;
				if (depotsNode is not null)
				{
					depotsNodeParent = steamNode;
					return depotsNode;
				}
				// DepotsNode Empty Then
				if (steamNode is not null)
				{
					steamNode.Add("depots", new VObject());
					depotsNode = steamNode?.depots ?? steamNode?.Depots;
					depotsNodeParent = steamNode;
					return depotsNode;
				}
				if (valveNode is not null)
				{
					valveNode.Add("depots", new VObject());
					depotsNode = valveNode?.depots ?? valveNode?.Depots;
					depotsNodeParent = valveNode;
					return depotsNode;
				}
				return null;
			}
		}
		public VObject Depots => TryDepots ?? throw new InvalidOperationException("Steam depot keys not found in vdf.");
		private VObject? TryManifests
		{
			get
			{
				if (TryDepots is null) return null;
				if (depotsNodeParent is null) return null;
				var manifestsNode = depotsNodeParent?.Manifests ?? depotsNodeParent?.manifests;
				if (manifestsNode is null)
				{
					depotsNodeParent!.Add("Manifests", new VObject());
					manifestsNode = depotsNodeParent?.Manifests ?? depotsNodeParent?.manifests;
				}
				return manifestsNode;
			}
		}
		public VObject Manifests => TryManifests ?? throw new InvalidOperationException("Manifests is null in vdf.");
		public void Save()
		{
			if (content is null) return;
			if (!File.Exists(configFile + ".bak"))
				File.Copy(configFile, configFile + ".bak", true);
			var vdfText = VdfConvert.Serialize(content).TrimEnd();
			if (!string.IsNullOrEmpty(tail) && tail.Trim() != "}")
			{
				vdfText = vdfText[..^1] + "\t\"WebStorage\"" + tail;
			}
			File.WriteAllText(configFile, vdfText);
		}
		public bool Merge(string key, string value)
		{
			if (Depots.ContainsKey(key))
			{
				var depot = Depots[key] as VObject;
				if (depot is null) return false;
				if (depot.ContainsKey("DecryptionKey"))
				{
					depot.Remove("DecryptionKey");
				}
				depot.Add("DecryptionKey", new VValue(value));
			}
			else
			{
				var newDepot = new VObject { { "DecryptionKey", new VValue(value) } };
				Depots.Add(key, newDepot);
			}
			return true;
		}
		public int MergeFile(string file)
		{
			Err = "";
			List<(long, string)> keyPairs = new();
			if (file.EndsWith(".st"))
			{
				_ = OutAPI.MsgBox("Manifest key files in ST format (.st) are not supported.");
				Err = "Manifest key files in ST format (.st) are not supported.";
				return -99;
			}
			if (file.EndsWith(".vdf"))
			{
				var vdfText = File.ReadAllText(file);
				if (VdfConvert.Deserialize(vdfText)?.Value is VObject vdf)
				{
					foreach (var pair in vdf)
					{
						if (pair.Value is not VObject depot) continue;
						if (!depot.ContainsKey("DecryptionKey")) continue;
						var decKey = depot["DecryptionKey"]?.ToString();
						if (decKey is null) continue;
						if (long.TryParse(pair.Key, out var id))
						{
							keyPairs.Add((id, decKey));
						}
					}
				}
			}
			else if (file.EndsWith(".lua"))
			{
				var luaText = File.ReadAllLines(file);
				foreach (var line in luaText)
				{
					//if (line.StartsWith("setManifestid"))
					//{
					//	var mLine = line.Replace("setManifestid", "").Replace("(", "").Replace(")", "").Replace("\"", "").Trim();
					//	var mCuts = mLine.Split(",");
					//	if (mCuts.Length >= 3)
					//	{

					//	}
					//}
					if (!line.StartsWith("addappid")) continue;
					var newLine = line.Replace("addappid", "").Replace("(", "").Replace(")", "").Replace("\"", "").Trim();
					var cuts = newLine.Split(",");
					if (cuts.Length <= 0) continue;
					var appid = cuts[0].Trim();
					if (long.TryParse(appid, out var id))
					{
						DataSystem.Instance.SetDepotUnlock(id, true);
						if (cuts.Length < 3) continue;
						var key = cuts[2].Trim();
						keyPairs.Add((id, key));
					}
				}
			}
			if (keyPairs.Count == 0)
			{
				Err = "No valid key pairs found in file.";
				return -1;
			}
			foreach (var pair in keyPairs)
			{
				DataSystem.Instance.SetDepotUnlock(pair.Item1 / 10 * 10, true);
				DataSystem.Instance.SetDepotUnlock(pair.Item1, true);
				Merge(pair.Item1.ToString(), pair.Item2);
			}
			return keyPairs.Count;
		}
		public bool MergeManifestItem(string depotId, string manifestId)
		{
			try
			{
				if (Manifests.ContainsKey(depotId))
				{
					Manifests.Remove(depotId);
					Manifests.Add(depotId, new VValue(manifestId));
				}
				else
				{
					Manifests.Add(depotId, new VValue(manifestId));
				}
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
