using CN_GreenLumaGUI.Messages;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CN_GreenLumaGUI.tools
{
	/// <summary>
	/// “用于替换的app池”管理：
	/// 默认池 内置列表(AppPoolDefault)，不生成文件；若 apppool.txt 存在则用它替代内置列表(只读)
	/// 拓展池 apppool_ext.txt (首次启动时创建为空，由界面管理)
	/// 两者读取后用 set 去重合并，被禁用的项(以 # 开头)不计入可用池。
	/// 可用池的长度即为“解锁上限”。
	/// </summary>
	public class AppPoolSystem
	{
		/// <summary>可选的默认池覆盖文件，不存在时使用内置列表</summary>
		public static readonly string PoolFile = $"{OutAPI.TempDir}\\apppool.txt";
		public static readonly string PoolExtFile = $"{OutAPI.TempDir}\\apppool_ext.txt";

		private static AppPoolSystem? instance;
		public static AppPoolSystem Instance => instance ??= new AppPoolSystem();

		/// <summary>默认池(内置)中的全部app，含被禁用的</summary>
		private readonly List<long> defaultList = new();
		private readonly HashSet<long> defaultSet = new();
		/// <summary>拓展池中新增的app，含被禁用的</summary>
		private readonly List<long> extList = new();
		private readonly HashSet<long> extSet = new();
		/// <summary>被禁用的app，禁用状态统一记录在拓展池文件中</summary>
		private readonly HashSet<long> disabledSet = new();

		private readonly object fileLock = new();

		private AppPoolSystem()
		{
		}

		/// <summary>启动时调用：拓展池文件不存在则创建，然后读取</summary>
		public void Init()
		{
			lock (fileLock)
			{
				// 默认池不生成文件，仅在 apppool.txt 存在时用它替代内置列表
				try
				{
					if (!File.Exists(PoolExtFile))
						File.WriteAllText(PoolExtFile, "");
				}
				catch (Exception e)
				{
					OutAPI.PrintLog($"Create app pool ext file failed: {e.Message}");
				}
			}
			Reload();
		}

		public void Reload()
		{
			lock (fileLock)
			{
				defaultList.Clear();
				defaultSet.Clear();
				extList.Clear();
				extSet.Clear();
				disabledSet.Clear();
				// 默认池：有 apppool.txt 就用它替代内置列表，否则用内置列表
				if (File.Exists(PoolFile))
				{
					foreach (var (id, disabled) in ReadLines(PoolFile))
					{
						if (defaultSet.Add(id))
							defaultList.Add(id);
						if (disabled) disabledSet.Add(id);
					}
				}
				else
				{
					foreach (var id in AppPoolDefault.List)
					{
						if (defaultSet.Add(id))
							defaultList.Add(id);
					}
				}
				// 拓展池
				foreach (var (id, disabled) in ReadLines(PoolExtFile))
				{
					if (disabled) disabledSet.Add(id);
					else disabledSet.Remove(id);
					if (defaultSet.Contains(id)) continue;
					if (extSet.Add(id))
						extList.Add(id);
				}
			}
		}

		private static IEnumerable<(long id, bool disabled)> ReadLines(string path)
		{
			var result = new List<(long, bool)>();
			try
			{
				if (!File.Exists(path)) return result;
				foreach (var rawLine in File.ReadAllLines(path))
				{
					var line = rawLine.Trim();
					if (line.Length == 0) continue;
					bool disabled = false;
					if (line[0] == '#')
					{
						disabled = true;
						line = line[1..].Trim();
					}
					if (!long.TryParse(line, out long id) || id <= 0) continue;
					result.Add((id, disabled));
				}
			}
			catch (Exception e)
			{
				OutAPI.PrintLog($"Read app pool file failed({path}): {e.Message}");
			}
			return result;
		}

		/// <summary>保存拓展池文件：拓展项 + 全部禁用标记</summary>
		private void SaveExtFile()
		{
			lock (fileLock)
			{
				try
				{
					var sb = new StringBuilder();
					foreach (var id in extList)
						sb.Append(disabledSet.Contains(id) ? "#" : "").Append(id).Append("\r\n");
					// 默认池中被禁用的项也记录在此，默认池文件保持只读
					foreach (var id in defaultList)
					{
						if (disabledSet.Contains(id))
							sb.Append('#').Append(id).Append("\r\n");
					}
					File.WriteAllText(PoolExtFile, sb.ToString());
				}
				catch (Exception e)
				{
					OutAPI.PrintLog($"Save app pool ext file failed: {e.Message}");
					_ = OutAPI.MsgBox(string.Format(LocalizationService.GetString("AppPool_SaveFailedFormat"), e.Message));
				}
			}
		}

		private void NotifyChanged()
		{
			WeakReferenceMessenger.Default.Send(new AppPoolChangedMessage());
		}

		/// <summary>实际可用的app池(去重、去禁用)，按默认池、拓展池的顺序</summary>
		public List<long> GetAvailableList()
		{
			lock (fileLock)
			{
				var result = new List<long>(defaultList.Count + extList.Count);
				foreach (var id in defaultList)
					if (!disabledSet.Contains(id)) result.Add(id);
				foreach (var id in extList)
					if (!disabledSet.Contains(id)) result.Add(id);
				return result;
			}
		}

		/// <summary>解锁上限 = 实际可用池的长度</summary>
		public int AvailableCount
		{
			get
			{
				lock (fileLock)
				{
					int count = 0;
					foreach (var id in defaultList)
						if (!disabledSet.Contains(id)) count++;
					foreach (var id in extList)
						if (!disabledSet.Contains(id)) count++;
					return count;
				}
			}
		}

		/// <summary>界面展示用的全部条目(含被禁用与内置)</summary>
		public List<(long id, bool isBuiltIn, bool isDisabled)> GetAllItems()
		{
			lock (fileLock)
			{
				var result = new List<(long, bool, bool)>(defaultList.Count + extList.Count);
				foreach (var id in defaultList)
					result.Add((id, true, disabledSet.Contains(id)));
				foreach (var id in extList)
					result.Add((id, false, disabledSet.Contains(id)));
				return result;
			}
		}

		public bool Contains(long id)
		{
			lock (fileLock)
			{
				return defaultSet.Contains(id) || extSet.Contains(id);
			}
		}

		/// <summary>新增一个拓展app，已存在返回false</summary>
		public bool AddApp(long id)
		{
			if (id <= 0) return false;
			lock (fileLock)
			{
				if (defaultSet.Contains(id) || extSet.Contains(id))
				{
					// 已存在时，若处于禁用状态则视为重新启用
					if (!disabledSet.Contains(id)) return false;
					disabledSet.Remove(id);
				}
				else
				{
					extSet.Add(id);
					extList.Add(id);
				}
			}
			SaveExtFile();
			NotifyChanged();
			return true;
		}

		/// <summary>删除一个拓展app，内置项不可删除(只能禁用)</summary>
		public bool RemoveApp(long id)
		{
			lock (fileLock)
			{
				if (!extSet.Contains(id)) return false;
				extSet.Remove(id);
				extList.Remove(id);
				disabledSet.Remove(id);
			}
			SaveExtFile();
			NotifyChanged();
			return true;
		}

		public bool IsDisabled(long id)
		{
			lock (fileLock)
			{
				return disabledSet.Contains(id);
			}
		}

		public void SetDisabled(long id, bool disabled)
		{
			lock (fileLock)
			{
				if (!defaultSet.Contains(id) && !extSet.Contains(id)) return;
				if (disabled == disabledSet.Contains(id)) return;
				if (disabled) disabledSet.Add(id);
				else disabledSet.Remove(id);
			}
			SaveExtFile();
			NotifyChanged();
		}

		/// <summary>ini映射中的一条：池中的app被用来替换哪个解锁项</summary>
		public readonly struct MapEntry
		{
			public MapEntry(long poolAppId, string name, long appId, bool isDlc)
			{
				PoolAppId = poolAppId;
				Name = name;
				AppId = appId;
				IsDlc = isDlc;
			}
			public long PoolAppId { get; }
			public string Name { get; }
			public long AppId { get; }
			public bool IsDlc { get; }
		}

		/// <summary>
		/// 按写入ini时完全相同的顺序，算出“池app → 解锁项”的配对。
		/// GLFileTool 写配置和界面预览都走这里，保证两者永远一致。
		/// </summary>
		public static List<MapEntry> BuildMapping(out bool overflow)
		{
			var pool = Instance.GetAvailableList();
			var result = new List<MapEntry>();
			overflow = false;
			int pos = 0;
			foreach (var game in DataSystem.Instance.GetGameDatas())
			{
				if (!game.IsSelected) continue;
				if (pos >= pool.Count) { overflow = true; return result; }
				result.Add(new MapEntry(pool[pos], game.GameName, game.GameId, false));
				pos++;
				foreach (var dlc in game.DlcsList)
				{
					if (!dlc.IsSelected) continue;
					if (pos >= pool.Count) { overflow = true; return result; }
					result.Add(new MapEntry(pool[pos], dlc.DlcName, dlc.DlcId, true));
					pos++;
				}
			}
			foreach (var id in DataSystem.Instance.GetUnlockDepotList())
			{
				if (pos >= pool.Count) { overflow = true; return result; }
				result.Add(new MapEntry(pool[pos], LocalizationService.GetString("AppPool_MapDepot"), id, true));
				pos++;
			}
			return result;
		}
	}
}
