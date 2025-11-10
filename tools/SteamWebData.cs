using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using CN_GreenLumaGUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CN_GreenLumaGUI.tools
{
    public class SteamWebData
    {
        private static readonly string steamAddress = "store.steampowered.com";
        private static readonly string steamStoreBaseAddress = $"https://{steamAddress}";
        private static readonly string steamStoreBaseAddressHttp = $"http://{steamAddress}";
        private static readonly string steamStoreBaseAddressHead = $"{steamStoreBaseAddress}/";
        private static readonly string steamStoreBaseAddressHttpHead = $"{steamStoreBaseAddressHttp}/";
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
        public static readonly int ApiPort = 5112;
        public static readonly string ApiBaseUrl = $"http://{ServerAddress}:{ApiPort}";
        public static readonly string ApiGetStatusAddress = $"{ApiBaseUrl}/status";
        public static readonly string ApiGetAppInfoAddress = $"{ApiBaseUrl}/appinfo";

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
            httpClient.DefaultRequestHeaders.Add("accept-language", "en-us");
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
            if (string.IsNullOrEmpty(imageUrl))
                return bitImage;
            try
            {
                bitImage.BeginInit();
                bitImage.UriSource = new Uri(imageUrl);
                bitImage.EndInit();
            }
            catch
            {
                return new BitmapImage();
            }
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
            try
            {
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
                    string? dlcStoreUrl = item?.GetAttribute("href");
                    if (dlcStoreUrl is null) continue;
                    string? dlcId = GetAppIdFromUrl(dlcStoreUrl);
                    if (dlcId is null) continue;
                    string? dlcName = item?.Children[0]?.Children[0]?.Children[0]?.Children[0]?.TextContent ?? "unknown";
                    if (long.TryParse(dlcId, out long result))
                    {
                        if (haveDlcs.TryGetValue(result, out int value))
                        {
                            dlcs[value].DlcName = dlcName;
                        }
                        else
                        {
                            haveDlcs.Add(result, dlcs.Count);
                            DlcObj dlcObj = new(dlcName, result, game);
                            dlcs.Add(dlcObj);
                            DataSystem.Instance.RegisterDlc(dlcObj);
                        }
                    }
                }
                game.DlcsList = dlcs;
                return true;
            }
            catch (Exception e)
            {
                OutAPI.PrintLog("Fail When GetDlcOfGame");
                OutAPI.PrintLog(e.Message);
                if (e.StackTrace is not null)
                    OutAPI.PrintLog(e.StackTrace);
                return false;
            }
        }
        public static AppAsyncEnumerable SearchGameAsync(string name, int resPage = 0, int pos = 0)
        {
            return new AppAsyncEnumerable(name, resPage, pos);
        }
        public enum GetAppInfoState
        {
            WrongUrl,
            WrongNetWork,
            WrongGameId,
            IsNotGame,
            WrongResponse,
            Success
        }
        public async Task<(string?, GetAppInfoState)> GetAppNameSimpleAsync(long appid)
        {
            string url = $"https://store.steampowered.com/widget/{appid}/?l=schinese";
            string target;
            if (url.StartsWith(steamStoreBaseAddressHead))
            {
                //从url头部去掉steamStoreBaseAddressHead得到target
                target = url[steamStoreBaseAddressHead.Length..];
            }
            else if (url.StartsWith(steamStoreBaseAddressHttpHead))
            {
                //从url头部去掉steamStoreBaseAddressHttpHead得到target
                target = url[steamStoreBaseAddressHttpHead.Length..];
            }
            else return (null, GetAppInfoState.WrongUrl);
            string? str = await GetSteamStoreWebContent(target);
            if (str is null)
            {
                //无法获取数据
                return (null, GetAppInfoState.WrongNetWork);
            }
            //创建一个html的解析器
            var parser = new HtmlParser();
            //使用解析器解析文档
            var document = parser.ParseDocument(str);
            var divs = document.All.Where(m => m.LocalName == "div");
            var divsArray = divs as IElement[] ?? divs.ToArray();
            var nameElement = divsArray.Where(m => m.ClassList.Contains("header_container"));
            var nameElementArray = nameElement as IElement[] ?? nameElement.ToArray();
            if (!nameElementArray.Any())
            {
                //不是Steam界面
                return (null, GetAppInfoState.WrongGameId);
            }
            // 获取header_container下面的 h1 标签下面的 a 标签里面的文本内容
            var name = nameElementArray.First()?.Children[0]?.Children[0]?.TextContent;
            return (name, GetAppInfoState.Success);
        }

        public async Task<(AppModel?, GetAppInfoState)> GetAppInformAsync(string url)
        {
            string target;
            if (url.StartsWith(steamStoreBaseAddressHead))
            {
                //从url头部去掉steamStoreBaseAddressHead得到target
                target = url[steamStoreBaseAddressHead.Length..];
            }
            else if (url.StartsWith(steamStoreBaseAddressHttpHead))
            {
                //从url头部去掉steamStoreBaseAddressHttpHead得到target
                target = url[steamStoreBaseAddressHttpHead.Length..];
            }
            else return (null, GetAppInfoState.WrongUrl);
            string? str = await GetSteamStoreWebContent(target);
            if (str is null)
            {
                //无法获取数据
                return (null, GetAppInfoState.WrongNetWork);
            }
            //创建一个html的解析器
            var parser = new HtmlParser();
            //使用解析器解析文档
            var document = parser.ParseDocument(str);
            var divs = document.All.Where(m => m.LocalName == "div");
            var divsArray = divs as IElement[] ?? divs.ToArray();
            var nameElement = divsArray.Where(m => m.ClassList.Contains("apphub_AppName"));
            var nameElementArray = nameElement as IElement[] ?? nameElement.ToArray();
            if (!nameElementArray.Any())
            {
                //网页没有apphub_AppName，不是商店界面
                return (null, GetAppInfoState.IsNotGame);
            }
            string name = nameElementArray.First()?.TextContent ?? ""; ;
            string imgUrl = "";
            var imgElement = document.All.Where(m => m.LocalName == "img" && m.ClassList.Contains("game_header_image_full"));
            var imgElementArray = imgElement as IElement[] ?? imgElement.ToArray();
            if (imgElementArray.Any())
            {
                imgUrl = imgElementArray.First()?.GetAttribute("src") ?? "";
            }
            if (!long.TryParse((GetAppIdFromUrl(url) ?? "").Trim(), out var id))
            {
                return (null, GetAppInfoState.WrongGameId);
            }
            string summary = "";
            var summaryElement = document.All.Where(m => m.LocalName == "span" && m.ClassList.Contains("game_review_summary"));
            var summaryElementArray = summaryElement as IElement[] ?? summaryElement.ToArray();
            if (summaryElementArray.Any())
            {
                summary = summaryElementArray.First()?.TextContent ?? "";
            }
            AppModel res = new(-1, imgUrl.Trim(), name.Trim(), id, summary.Trim(), url.Trim());
            //如果是音乐集
            if (divsArray.Any(m => m.ClassList.Contains("game_area_soundtrack_bubble")))
            {
                res.IsGame = false;
            }
            //如果是DLC
            if (divsArray.Any(m => m.ClassList.Contains("game_area_dlc_bubble")))
            {
                res.IsGame = false;
            }
            if (res.IsGame) return (res, GetAppInfoState.Success);
            // 获取DLC所属游戏
            var dlcDivs = divsArray.Where(m => m.ClassList.Contains("game_area_soundtrack_bubble") || m.ClassList.Contains("game_area_dlc_bubble")).ToArray();
            if (dlcDivs.Any())
            {
                // 获取第一个匹配的div 在div中查找a标签
                var aTag = dlcDivs.FirstOrDefault()?.QuerySelector("a");
                if (aTag != null)
                {
                    // 获取href属性
                    var parentGameUrl = aTag.GetAttribute("href");
                    if (parentGameUrl is not null)
                    {
                        var idStr = GetAppIdFromUrl(parentGameUrl);
                        if (idStr is not null && long.TryParse(idStr, out var parentGameId))
                        {
                            res.ParentId = parentGameId;
                        }
                    }
                }
            }
            return (res, GetAppInfoState.Success);
        }

        public async Task<(HttpStatusCode?, string?)> GetJson(string url)
        {
            HttpResponseMessage response;
            HttpStatusCode stateCode;
            try
            {
                response = await httpClient.GetAsync(url);
                stateCode = response.StatusCode;
                return (stateCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                OutAPI.PrintLog(ex.Message);
                if (ex.StackTrace is not null)
                    OutAPI.PrintLog(ex.StackTrace);
            }
            return (null, null);
        }
        public async Task<bool?> GetServerStatusFromApi()
        {
            (HttpStatusCode? code, string? json) = await GetJson(ApiGetStatusAddress);
            if (code is null || code != HttpStatusCode.OK || json is null) return null;
            var cuts = json.Split('/');
            if (cuts.Length >= 2)
            {
                if (int.TryParse(cuts[0], out var nowQueueNum) && int.TryParse(cuts[1], out var maxQueueNum))
                {
                    return nowQueueNum < maxQueueNum;
                }
            }
            return null;
        }
        public async Task<(ApiSimpleApp?, GetAppInfoState)> GetAppInformFromApi(long appid)
        {
            (HttpStatusCode? code, string? json) = await GetJson($"{ApiGetAppInfoAddress}/{appid}");
            if (code is null || json is null) return (null, GetAppInfoState.WrongNetWork);
            if (code == HttpStatusCode.BadGateway) return (null, GetAppInfoState.WrongNetWork);
            if (code == HttpStatusCode.NotFound) return (null, GetAppInfoState.WrongGameId);
            ApiSimpleApp? app = json.FromJSON<ApiSimpleApp>();
            if (app is null) return (null, GetAppInfoState.WrongResponse);
            return (app, GetAppInfoState.Success);
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
                        Program.NeedUpdate = true;
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

