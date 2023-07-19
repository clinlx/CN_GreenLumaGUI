using AngleSharp.Html.Parser;
using CN_GreenLumaGUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CN_GreenLumaGUI.tools
{
	public class SteamWebData
	{
		private static readonly SteamWebData instance = new();
		public static SteamWebData Instance { get { return instance; } }

		private readonly HttpClient httpClient;

		private SteamWebData()
		{
			var socketsHttpHandler = new SocketsHttpHandler()
			{
				ConnectTimeout = TimeSpan.FromSeconds(5),
				AllowAutoRedirect = true,// 默认为true,是否允许重定向
				MaxAutomaticRedirections = 50,//最多重定向几次,默认50次
											  //MaxConnectionsPerServer = 100,//连接池中统一TcpServer的最大连接数
				UseCookies = true// 是否自动处理cookie
			};
			httpClient = new HttpClient(socketsHttpHandler)
			{
				Timeout = TimeSpan.FromSeconds(8)
			};
			httpClient.DefaultRequestHeaders.Add("accept-language", "zh-cn");
		}
		public async Task<string?> GetHtml(string url)
		{
			HttpResponseMessage response;
			//HttpStatusCode stateCode;
			//HttpResponseHeaders headers;
			try
			{
				response = await httpClient.GetAsync(url);
				//stateCode = response.StatusCode;
				//headers = response.Headers;
				response.EnsureSuccessStatusCode();
				return await response.Content.ReadAsStringAsync();
			}
			catch (Exception ex)
			{
				OutAPI.PrintLog(ex.Message);
				if (ex.StackTrace is not null)
					OutAPI.PrintLog(ex.StackTrace);
			}
			return null;
		}
		public static BitmapImage GetImageFromUrl(string imageUrl)
		{
			BitmapImage bitImage = new();
			bitImage.BeginInit();
			bitImage.UriSource = new Uri(imageUrl);
			bitImage.EndInit();
			return bitImage;
		}
		public static string? GetAppIdFromUrl(string url)
		{
			string[] strSplit = url.Split('/');
			if (strSplit[1] != "app" && strSplit[3] != "app")
			{
				return null;
			}

			if (int.TryParse(strSplit[4], out _))
			{
				return strSplit[4];
			}

			if (int.TryParse(strSplit[2], out _))
			{
				return strSplit[2];
			}
			return null;
		}
		public async Task<bool> AutoAddDlcsAsync(GameObj game)
		{
			//string url = "https://store.steampowered.com/dlc/" + game.GameId;
			string url = $"https://store.steampowered.com/dlc/{game.GameId}/_/ajaxgetfilteredrecommendations/render/?query=&start=0&count=128&tagids=&sort=newreleases&app_types=&curations=&reset=true";
			string? json = await GetHtml(url);
			if (json is null)
			{
				//无法从steam获取数据
				return false;
			}
			string? str = json.FromJSON<dynamic>()?.results_html;
			if (str is null)
			{
				//无法从数据获取信息
				return false;
			}
			//创建一个html的解析器
			var parser = new HtmlParser();
			//使用解析器解析文档
			var document = parser.ParseDocument(str);
			var DlcsResList = document.All.Where(m => m.LocalName == "a" && m.ClassList.Contains("recommendation_link")).ToList();

			Dictionary<long, int> haveDlcs = new();
			for (int i = 0; i < game.DlcsList.Count; i++)
			{
				haveDlcs.Add(game.DlcsList[i].DlcId, i);
			}
			ObservableCollection<DlcObj> dlcs = new(game.DlcsList);
			for (int i = 0; i < DlcsResList.Count; i++)
			{
				var item = DlcsResList[DlcsResList.Count - i - 1];
				string? dlcStoreUrl = item.GetAttribute("href");
				if (dlcStoreUrl is null) continue;
				string? dlcId = GetAppIdFromUrl(dlcStoreUrl);
				if (dlcId is null) continue;
				string? dlcName = item.Children[0].Children[0].Children[0].Children[0].TextContent;
				if (dlcName is null) continue;
				if (long.TryParse(dlcId, out long result))
				{
					if (haveDlcs.TryGetValue(result, out int value))
					{
						dlcs[value].DlcName = dlcName;
					}
					else
					{
						haveDlcs.Add(result, dlcs.Count);
						dlcs.Add(new(dlcName, result, game));
					}
				}
			}
			game.DlcsList = dlcs;
			return true;
		}
		public async Task<List<AppModel>?> SearchGameAsync(string name, int resPage = 0, int pos = 0)
		{
			const int maxGamePerPage = 25;//>=25
			string url = $"https://store.steampowered.com/search/results?term={name}&start={resPage * maxGamePerPage}&count={maxGamePerPage}";
			string? str = await GetHtml(url);
			if (str is null)
			{
				//搜索失败,无法从steam获取数据
				return null;
			}
			//创建一个html的解析器
			var parser = new HtmlParser();
			//使用解析器解析文档
			var document = parser.ParseDocument(str);
			var GamesResList = document.All.Where(m =>
			m.LocalName == "a" &&
			m.ClassList.Contains("search_result_row") &&
			!string.IsNullOrEmpty(m.GetAttribute("data-ds-appid")));//没有ID的是合集，滤掉
			if (!GamesResList.Any())
			{
				//无符合的游戏
				return new();
			}
			List<Task<AppModel?>> tasks = new();
			foreach (var item in GamesResList)
			{
				//网址
				var itemUrl = item.GetAttribute("href");
				if (itemUrl is null) continue;
				tasks.Add(GetAppInformAsync(itemUrl));
			}
			var gameInforms = await Task.WhenAll(tasks.ToArray());
			List<AppModel> appModels = new();
			foreach (var gameInform in gameInforms)
			{
				//只取有结果的
				if (gameInform is null) continue;
				//滤掉DLC和音乐
				if (!gameInform.IsGame) continue;
				//设置下标并添加
				gameInform.Index = pos + 1;
				pos++;
				appModels.Add(gameInform);
			}
			return appModels;
		}
		public async Task<AppModel?> GetAppInformAsync(string url)
		{
			string? str = await GetHtml(url);
			if (str is null)
			{
				//无法获取数据
				return null;
			}
			//创建一个html的解析器
			var parser = new HtmlParser();
			//使用解析器解析文档
			var document = parser.ParseDocument(str);
			var divs = document.All.Where(m => m.LocalName == "div");
			var nameElement = divs.Where(m => m.ClassList.Contains("apphub_AppName"));
			if (!nameElement.Any())
			{
				//网页没有apphub_AppName，不是商店界面
				return null;
			}
			string name = nameElement.First()?.TextContent ?? ""; ;
			string imgUrl = "";
			var imgElement = document.All.Where(m => m.LocalName == "img" && m.ClassList.Contains("game_header_image_full"));
			if (imgElement.Any())
			{
				imgUrl = imgElement.First()?.GetAttribute("src") ?? "";
			}
			long id = -1;
			if (!long.TryParse((GetAppIdFromUrl(url) ?? "").Trim(), out id))
			{
				return null;
			}
			string summary = "";
			var summaryElement = document.All.Where(m => m.LocalName == "span" && m.ClassList.Contains("game_review_summary"));
			if (summaryElement.Any())
			{
				summary = summaryElement.First()?.TextContent ?? "";
			}
			string weburl = url;
			AppModel res = new(-1, imgUrl.Trim(), name.Trim(), id, summary.Trim(), weburl.Trim());
			//如果是音乐集
			if (divs.Any(m => m.ClassList.Contains("game_area_soundtrack_bubble")))
			{
				res.IsGame = false;
			}
			//如果是DLC
			if (divs.Any(m => m.ClassList.Contains("game_area_dlc_bubble")))
			{
				res.IsGame = false;
			}
			return res;
		}

	}
}
