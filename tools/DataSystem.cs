﻿using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Models;
using CommunityToolkit.Mvvm.Messaging;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace CN_GreenLumaGUI.tools
{
	public class DataSystem
	{
		private static readonly DataSystem instance = new();
		public static DataSystem Instance { get { return instance; } }
		public static bool isLoaded = false;
		public static bool isLoadedEnd = false;
		public static bool isError = false;
		//Settings Data
		public bool NotNullConfig { get; set; }//日志中发现有无论如何，配置都是false的情况，写来测试
		public string LastVersion { get; set; }
		public long StartSuccessTimes { get; set; }

		private string? steamPath;
		public string? SteamPath
		{
			get { return steamPath; }
			set
			{
				steamPath = value;
				WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(SteamPath)));
			}
		}
		private bool isDarkTheme;
		public bool DarkMode
		{
			get
			{
				return isDarkTheme;
			}
			set
			{
				isDarkTheme = value;
				var paletteHelper = new PaletteHelper();
				var theme = paletteHelper.GetTheme();
				theme.SetBaseTheme(isDarkTheme ? Theme.Dark : Theme.Light);
				paletteHelper.SetTheme(theme);
				WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(DarkMode)));
			}
		}
		private bool hidePromptText;
		public bool HidePromptText
		{
			get
			{ return hidePromptText; }
			set
			{
				hidePromptText = value;
				WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(HidePromptText)));
			}
		}

		private bool startWithBak;
		public bool StartWithBak
		{
			get { return startWithBak; }
			set
			{
				startWithBak = value;
				WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(StartWithBak)));
			}
		}
		private bool haveTriedBak;
		public bool HaveTriedBak
		{
			get { return haveTriedBak; }
			set { haveTriedBak = value; }//内部隐藏变量不用发送消息
		}
		private bool scrollBarEcho;
		public bool ScrollBarEcho
		{
			get
			{ return scrollBarEcho; }
			set
			{
				scrollBarEcho = value;
				WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(ScrollBarEcho)));
			}
		}
		private bool modifySteamDNS;
		public bool ModifySteamDNS
		{
			get
			{ return modifySteamDNS; }
			set
			{
				modifySteamDNS = value;
				WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(ModifySteamDNS)));
			}
		}
		private bool runSteamWithAdmin;
		public bool RunSteamWithAdmin
		{
			get
			{ return runSteamWithAdmin; }
			set
			{
				runSteamWithAdmin = value;
				WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(RunSteamWithAdmin)));
			}
		}

		private bool newFamilyModel;
		public bool NewFamilyModel
		{
			get
			{ return newFamilyModel; }
			set
			{
				newFamilyModel = value;
				WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(NewFamilyModel)));
			}
		}

		private bool clearSteamAppCache;
		public bool ClearSteamAppCache
		{
			get
			{ return clearSteamAppCache; }
			set
			{
				clearSteamAppCache = value;
				WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(ClearSteamAppCache)));
			}
		}
		//添加完字段后记得看看LoadData()和SettingsPageViewModel第26行

		private DataSystem()
		{
			gameDatas = new();
			gameExist = new();
		}
		private readonly static string configFile = $"{OutAPI.TempDir}\\config.json";
		private readonly static string unlocklistFile = $"{OutAPI.TempDir}\\unlocklist.json";
		public void LoadData()
		{
			OutAPI.PrintLog("isLoaded");
			isLoaded = true;
			//读取软件配置文件
			dynamic? readConfig = null;
			if (File.Exists(configFile))
			{
				try
				{
					readConfig = File.ReadAllText(configFile).FromJSON<dynamic>();
				}
				catch
				{ }
			}
			//读取成功则设置，否则设为默认配置
			NotNullConfig = readConfig?.NotNullConfig ?? true;
			if (!NotNullConfig)
			{
				OutAPI.PrintLog("isError");
				isError = true;
				NotNullConfig = true;
				readConfig = null;
			}
			LastVersion = readConfig?.LastVersion ?? "null";
			StartSuccessTimes = readConfig?.StartSuccessTimes ?? 0;
			SteamPath = readConfig?.SteamPath;
			DarkMode = readConfig?.DarkMode ?? false;
			HidePromptText = readConfig?.HidePromptText ?? false;
			StartWithBak = readConfig?.StartWithBak ?? true;
			HaveTriedBak = readConfig?.HaveTriedBak ?? false;
			ScrollBarEcho = readConfig?.ScrollBarEcho ?? true;
			ModifySteamDNS = readConfig?.ModifySteamDNS ?? false;
			RunSteamWithAdmin = readConfig?.RunSteamWithAdmin ?? true;
			//NewFamilyModel = readConfig?.NewFamilyModel ?? false;
			ClearSteamAppCache = readConfig?.ClearSteamAppCache ?? true;
			//读取游戏列表文件
			if (File.Exists(unlocklistFile))
			{
				try
				{
					List<GameObj>? readGameDatas = File.ReadAllText(unlocklistFile).FromJSON<List<GameObj>>();
					checkedNum = 0;
					if (readGameDatas is not null)
					{
						foreach (var i in readGameDatas)
						{
							AddGame(i.GameName, i.GameId, i.IsSelected, i.DlcsList);
						}
					}
				}
				catch
				{

				}
			}
			OutAPI.PrintLog("isLoadedEnd");
			isLoadedEnd = true;
			WeakReferenceMessenger.Default.Send(new LoadFinishedMessage("DataSystem"));
		}
		public void SaveData()
		{
			//写入软件配置文件
			File.WriteAllText(configFile, this.ToJSON(true));
			//写入游戏列表至文件
			File.WriteAllText(unlocklistFile, gameDatas.ToJSON(true));
		}
		public void AddGame(string gameName, long gameId, bool isSelected, ObservableCollection<DlcObj> dlcsList)
		{
			lock (gameExist)
			{
				foreach (var dlc in dlcsList)
					if (dlc.IsSelected) CheckedNumInc(dlc.DlcId);
				if (IsGameExist(gameId))
				{
					GameObj? obj = gameExist[gameId];
					if (obj is not null)
					{
						foreach (var dlc in obj.DlcsList)
							if (dlc.IsSelected) CheckedNumDec(dlc.DlcId);
						obj.GameName = gameName;
						obj.DlcsList = dlcsList;
						obj.IsSelected = isSelected;
						return;
					}
				}
				var theGame = new GameObj(gameName, gameId, isSelected, dlcsList);
				foreach (var dlc in dlcsList)
					dlc.Master = theGame;
				gameExist[gameId] = theGame;
				gameDatas.Add(theGame);
				theGame?.UpdateCheckNum();
			}
			WeakReferenceMessenger.Default.Send(new GameListChangedMessage(gameId));
			DataSystem.Instance.SaveData();
		}
		public void RemoveGame(GameObj game)
		{
			lock (gameExist)
			{
				if (game.IsSelected) CheckedNumDec(game.GameId);
				foreach (var dlc in game.DlcsList)
					if (dlc.IsSelected) CheckedNumDec(dlc.DlcId);
				gameExist[game.GameId] = null;
				gameDatas.Remove(game);
			}
			WeakReferenceMessenger.Default.Send(new GameListChangedMessage(game.GameId));
			DataSystem.Instance.SaveData();

		}
		public bool IsGameExist(long gameId)
		{
			lock (gameExist)
			{
				if (!gameExist.ContainsKey(gameId))
					return false;
				return gameExist[gameId] != null;
			}
		}
		public GameObj? GetGameObjFromId(long id)
		{
			lock (gameExist)
			{
				if (gameExist.TryGetValue(id, out GameObj? value))
					return value;
				return null;
			}
		}
		public ObservableCollection<GameObj> GetGameDatas()
		{
			return gameDatas;
		}

		private long checkedNum = 0;
		[JsonIgnore]
		public long CheckedNum { get { return checkedNum; } }
		public void CheckedNumInc(long updateFrom)
		{
			checkedNum++;
			WeakReferenceMessenger.Default.Send(new CheckedNumChangedMessage(updateFrom));
		}
		public void CheckedNumDec(long updateFrom)
		{
			checkedNum--;
			WeakReferenceMessenger.Default.Send(new CheckedNumChangedMessage(updateFrom));
		}
		private readonly ObservableCollection<GameObj> gameDatas;
		private readonly Dictionary<long, GameObj?> gameExist;
	}
}
