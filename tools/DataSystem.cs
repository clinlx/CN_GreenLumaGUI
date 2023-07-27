using CN_GreenLumaGUI.Messages;
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

		//Settings Data
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
				WeakReferenceMessenger.Default.Send(new ConfigChangedMessage(nameof(DarkMode)));
				var paletteHelper = new PaletteHelper();
				var theme = paletteHelper.GetTheme();
				theme.SetBaseTheme(isDarkTheme ? Theme.Dark : Theme.Light);
				paletteHelper.SetTheme(theme);
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
			set { startWithBak = value; }
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
		private DataSystem()
		{
			gameDatas = new();
			gameExist = new();
		}
		private readonly static string configFile = $"{OutAPI.TempDir}\\config.json";
		private readonly static string unlocklistFile = $"{OutAPI.TempDir}\\unlocklist.json";
		public void LoadData()
		{
			//读取软件配置文件
			if (File.Exists(configFile))
			{
				try
				{
					dynamic? readConfig = File.ReadAllText(configFile).FromJSON<dynamic>();
					if (readConfig is not null)
					{
						SteamPath = readConfig.SteamPath;
						DarkMode = readConfig.DarkMode;
						HidePromptText = readConfig.HidePromptText;
						StartWithBak = readConfig.StartWithBak;
						ScrollBarEcho = readConfig.ScrollBarEcho;
					}
				}
				catch
				{

				}
			}
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
