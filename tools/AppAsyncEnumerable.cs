using AngleSharp.Html.Parser;
using CN_GreenLumaGUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CN_GreenLumaGUI.tools
{
	public class AppAsyncEnumerable : IAsyncEnumerable<AppModel?>
	{
		readonly string name;
		readonly int resPage;
		int pos;

		private bool cancelSearch;
		private bool stopBecauseNet = true;
		public bool StopBecauseNet { get { lock (this) { return stopBecauseNet; } } }

		private int finishedTaskNum = 0;
		private int totalNum = 0;

		public int GetProgress()
		{
			if (totalNum == 0) return 0;
			return (int)((double)finishedTaskNum / totalNum * 100);
		}
		public void CancelSearch()
		{
			lock (this)
			{
				cancelSearch = true;
			}
		}
		public AppAsyncEnumerable(string name, int resPage = 0, int pos = 0)
		{
			cancelSearch = false;
			this.name = name;
			this.resPage = resPage;
			this.pos = pos;
		}
		public async IAsyncEnumerator<AppModel?> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			const int maxGamePerPage = 25;//Steam接口限制需>=25
			string target = $"search/results?term={HttpUtility.UrlEncode(name)}&start={resPage * maxGamePerPage}&count={maxGamePerPage}";
			string? str = await SteamWebData.Instance.GetSteamStoreWebContent(target);
			if (str is null)
			{
				//搜索失败,无法从steam获取数据
				yield return null;
				yield break;
			}
			//创建一个html的解析器
			var parser = new HtmlParser();
			//使用解析器解析文档
			var document = await Task.Run(() =>
			{
				return parser.ParseDocument(str);
			});
			var GamesResList = await Task.Run(() =>
			{
				return document.All.Where(m =>
				m.LocalName == "a" &&
				m.ClassList.Contains("search_result_row") &&
				!string.IsNullOrEmpty(m.GetAttribute("data-ds-appid")));//没有ID的是合集，滤掉
			});
			if (!GamesResList.Any())
			{
				//无符合的游戏
				yield return new AppModel();
				yield break;
			}
			List<string> itemUrls = new();
			//网址提取
			foreach (var item in GamesResList)
			{
				var itemUrl = item.GetAttribute("href");
				if (itemUrl is null) continue;
				itemUrls.Add(itemUrl);
			}
			//定义Task数组
			var tasks = new Task<AppModel?>?[itemUrls.Count];
			const int maxTasksNum = 8;
			int leftTaskNumNow = 0;
			int taskQueuePos = finishedTaskNum;
			List<int> runningTask = new(maxTasksNum);
			totalNum = itemUrls.Count;
			//多线等待
			while (finishedTaskNum < itemUrls.Count)
			{
				if (leftTaskNumNow < maxTasksNum)
				{
					int newTaskNum = Math.Min(maxTasksNum - leftTaskNumNow, itemUrls.Count - taskQueuePos);
					for (int i = 0; i < newTaskNum; i++)
					{
						tasks[taskQueuePos + i] = SteamWebData.Instance.GetAppInformAsync(itemUrls[taskQueuePos + i]);
						runningTask.Add(taskQueuePos + i);
					}
					leftTaskNumNow += newTaskNum;
					taskQueuePos += newTaskNum;
				}
				AppModel? gameInform = null;
				int canFailTimes = 12;
				while (canFailTimes > 0)
				{
					for (int i = 0; i < runningTask.Count; i++)
					{
						int index = runningTask[i];
						var targetTask = tasks[index];
						if (targetTask is null) continue;
						if (targetTask.IsCompleted)
						{
							AppModel? taskResult = await targetTask;
							if (taskResult is null)
							{
								tasks[index] = SteamWebData.Instance.GetAppInformAsync(itemUrls[index]);
								canFailTimes--;
							}
							else
							{
								gameInform = taskResult;
								tasks[index] = null;
								runningTask.Remove(index);
								leftTaskNumNow--;
								finishedTaskNum++;
								break;
							}
						}
					}
					if (gameInform is not null)
						break;
					lock (this)
					{
						if (cancelSearch)
						{
							cancelSearch = false;
							stopBecauseNet = false;
							break;
						}
					}
					await Task.Delay(1, cancellationToken);
				}
				if (gameInform is null)
				{
					yield return null;
					lock (this)
					{
						cancelSearch = false;
						stopBecauseNet = true;
					}
					continue;
				}
				if (!gameInform.IsGame) continue;
				//设置下标并添加
				gameInform.Index = pos + 1;
				pos++;
				yield return gameInform;
				lock (this)
				{
					cancelSearch = false;
					stopBecauseNet = true;
				}
			}
		}
	}
}
