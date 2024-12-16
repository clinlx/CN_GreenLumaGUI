using Gameloop.Vdf.Linq;
using Gameloop.Vdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CN_GreenLumaGUI.tools
{
	internal class SteamVdfHandler
	{
		public SteamVdfHandler()
		{
			var steamPath = Path.GetDirectoryName(DataSystem.Instance.SteamPath);
			tail = "}";
			if (!string.IsNullOrEmpty(steamPath))
			{
				try
				{
					configFile = Path.Combine(steamPath, "config", "config.vdf");
					if (File.Exists(configFile))
					{
						var cuts = File.ReadAllText(configFile).Split("\"WebStorage\"");
						if (cuts.Length == 0) return;
						var vdfText = cuts[0] + "}";
						if (cuts.Length > 1) tail = cuts[1];
						content = VdfConvert.Deserialize(vdfText);
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
		public string Err { get; set; }
		public bool Success => content is not null;
		public VObject Depots => (content as dynamic)?.Value?.Software?.Valve?.Steam?.depots ?? throw new InvalidOperationException("Steam depot keys not found.");

		public void Save()
		{
			if (content is null) return;
			if (!File.Exists(configFile + ".bak"))
				File.Copy(configFile, configFile + ".bak", true);
			var vdfText = VdfConvert.Serialize(content).TrimEnd()[..^1] + "\t\"WebStorage\"" + tail;
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
					if (!line.StartsWith("addappid")) continue;
					var newLine = line.Replace("addappid", "").Replace("(", "").Replace(")", "").Replace("\"", "").Trim();
					var cuts = newLine.Split(",");
					if (cuts.Length < 3) continue;
					var appid = cuts[0].Trim();
					var key = cuts[2].Trim();
					if (long.TryParse(appid, out var id))
					{
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
				Merge(pair.Item1.ToString(), pair.Item2);
			}
			return keyPairs.Count;
		}
	}
}
