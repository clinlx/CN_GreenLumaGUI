using AngleSharp.Html.Parser;
using CN_GreenLumaGUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media.Imaging;

namespace CN_GreenLumaGUI.tools
{
	public class SteamWebData
	{
		private static readonly string steamAddress = "store.steampowered.com";
		private static readonly string steamStoreBaseAddress = $"https://{steamAddress}";
		private static readonly string steamStoreBaseAddressHttp = $"http://{steamAddress}";
		private static readonly SteamWebData instance = new();
		public static SteamWebData Instance { get { return instance; } }

		// 服务器设置
		public static readonly string ServerAddress = "cnglgui.birdry.cn";
		public static readonly int ServerPort = 9000;
		public static readonly string ServerAddressFull = $"{ServerAddress}:{ServerPort}";
		public static readonly string ServerBaseUrl = $"http://{ServerAddressFull}";
		public static readonly string LogUploadAddress = $"{ServerBaseUrl}/SoftLog";
		public static readonly string GetVersionAddress = $"{ServerBaseUrl}/SoftVersion";
		public static readonly string GetUpdateAddress = $"{ServerBaseUrl}/SoftUpdate";
		public static readonly string GetSteamProxyAddress = $"{ServerBaseUrl}/api/steamproxy";

		private readonly HttpClient httpClient;

		private SteamWebData()
		{
			var handler = new DnsHandler
			{
				//ConnectTime = TimeSpan.FromSeconds(8),
				AllowAutoRedirect = true,// 默认为true,是否允许重定向
				MaxAutomaticRedirections = 50,//最多重定向几次,默认50次
				MaxConnectionsPerServer = 100,//连接池中统一TcpServer的最大连接数
				UseCookies = false,// 是否自动处理cookie
				ClientCertificateOptions = ClientCertificateOption.Manual
			};
			handler.ServerCertificateCustomValidationCallback += (httpRequestMessage, cert, cetChain, policyErrors) =>
					{
						return true;
					};
			//socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback =
			//		(httpRequestMessage, cert, cetChain, policyErrors) =>
			//		{
			//			return true;
			//		};
			httpClient = new HttpClient(handler)
			{
				Timeout = TimeSpan.FromSeconds(15)
			};
			httpClient.DefaultRequestHeaders.Add("accept-language", "zh-cn");
			httpClient.DefaultRequestHeaders.Add("Cookie", "lastagecheckage=1-0-2000; birthtime=944000000;");
		}
		public async Task<string?> GetWebContent(string url)
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
		public async Task<string?> GetSteamStoreWebContent(string target)
		{
			if (DataSystem.Instance.ModifySteamDNS)
			{
				//从代理服务器获取信息
				Dictionary<string, string> dic = new()
						{
							{ "target", HttpUtility.UrlEncode(target) }
						};
				string result = await Task.Run(() =>
				{
					return OutAPI.Post(GetSteamProxyAddress, dic);
				});
				if (result == "exception") return null;
				//解码
				string decode = string.Empty;
				byte[] bytes = Convert.FromBase64String(result);
				try
				{
					decode = Encoding.UTF8.GetString(bytes);
				}
				catch
				{
					decode = result;
				}
				return decode;

			}
			else
			{
				//默认直接从Steam获取
				string url = steamStoreBaseAddress + '/' + target;
				return await GetWebContent(url);
			}
		}
		public static string? GetAppIdFromUrl(string url)
		{
			string[] strSplit = url.Split('/');
			if (strSplit[1] != "app" && strSplit[3] != "app")
			{
				return null;
			}
			if (strSplit[1] == "app" && int.TryParse(strSplit[2], out _))
			{
				return strSplit[2];
			}
			if (strSplit[3] == "app" && int.TryParse(strSplit[4], out _))
			{
				return strSplit[4];
			}
			return null;
		}
		public async Task<bool> AutoAddDlcsAsync(GameObj game)
		{
			//string url = "https://{steamAddress}/dlc/" + game.GameId;
			string target = $"dlc/{game.GameId}/_/ajaxgetfilteredrecommendations/render/?query=&start=0&count=128&tagids=&sort=newreleases&app_types=&curations=&reset=true";
			string? json = await GetSteamStoreWebContent(target);
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
		public AppAsyncEnumerable SearchGameAsync(string name, int resPage = 0, int pos = 0)
		{
			return new AppAsyncEnumerable(name, resPage, pos);
		}
		public async Task<AppModel?> GetAppInformAsync(string url)
		{
			string target;
			if (url.StartsWith(steamStoreBaseAddress))
			{
				//从url头部去掉steamStoreBaseAddress得到target
				target = url.Substring(steamStoreBaseAddress.Length);
			}
			else if (url.StartsWith(steamStoreBaseAddressHttp))
			{
				//从url头部去掉steamStoreBaseAddressHttp得到target
				target = url.Substring(steamStoreBaseAddressHttp.Length);
			}
			else return null;
			string? str = await GetSteamStoreWebContent(target);
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

		//private string? lastVersion;
		//public string? LastVersion { get { return lastVersion; } }
		public async Task UpdateLastVersion()
		{
			try
			{
				string? res = await GetWebContent(GetVersionAddress);
				if (res is null || res == "None") return;
				//lastVersion = res;
				var newVersionCut = Program.VersionCut(res);
				var nowVersionCut = Program.VersionCut(Program.Version);
				int length = Math.Min(newVersionCut.Length, nowVersionCut.Length);
				for (int i = 0; i < length; i++)
				{
					if (newVersionCut[i] > nowVersionCut[i])
					{
						//有新版本
						Program.needUpdate = true;
						return;
					}
					if (newVersionCut[i] < nowVersionCut[i])
					{
						//无新版本
						return;
					}
				}
			}
			catch { }
		}

		public struct ServerDownLoadObj
		{
			public ServerDownLoadObj(string version, string url)
			{
				this.LastVersion = version;
				this.DownUrl = url;
			}
			public string LastVersion { get; set; }
			public string DownUrl { get; set; }
		}

		public async Task<ServerDownLoadObj?> GetServerDownLoadObj()
		{
			try
			{
				string? res = await GetWebContent(GetUpdateAddress);
				if (res is null) return null;
				var obj = res.FromJSON<ServerDownLoadObj>();
				if (obj.DownUrl is null || obj.DownUrl == "None") return null;
				return obj;
			}
			catch { }
			return null;
		}
	}
	public class DnsHandler : HttpClientHandler
	{
		public DnsHandler()
		{
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			//var host = request.RequestUri.Host;
			//var addr = host;
			//var port = request.RequestUri.Port;
			//TODO: 转发Steam商店域名
			//if (DataSystem.Instance.ModifySteamDNS)
			//{
			//	if (host == "store.steampowered.com")
			//	{
			//		//CDN服务器
			//		addr = SteamWebData.ServerAddress;
			//		port = SteamWebData.ServerPort;
			//		//设置请求头
			//		request.Headers.Host = "store.steampowered.com";
			//	}
			//}
			//var builder = new UriBuilder(request.RequestUri)
			//{
			//	Host = addr,
			//	Port = port
			//};
			//request.RequestUri = builder.Uri;

			return base.SendAsync(request, cancellationToken);
		}
	}
}

