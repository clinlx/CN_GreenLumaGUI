using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CN_GreenLumaGUI.Models
{
	public enum SpanType
	{
		Normal,
		Bold,
		Italic,
		StrikeThrough,
		Link
	}

	public class MarkdownSpan
	{
		public SpanType Type { get; set; }
		public string Text { get; set; }
		public string Url { get; set; }

		public MarkdownSpan(SpanType type, string text, string url = null)
		{
			Type = type;
			Text = text;
			Url = url;
		}
	}

	public class TextItemModel
	{
		public string Text { get; set; }
		public int FontSize { get; set; }
		public string FontWeight { get; set; }
		public string Foreground { get; set; }
		public List<MarkdownSpan> Spans { get; set; }

		public TextItemModel() : this("") { }
		public TextItemModel(string text) : this(text, 14, "Normal", "Black") { }
		public TextItemModel(string text, int fontSize, string fontWeight, string foreground)
		{
			Text = text;
			FontSize = fontSize;
			FontWeight = fontWeight;
			Foreground = foreground;
			Spans = ParseSpans(text);
		}

		private static List<MarkdownSpan> ParseSpans(string text)
		{
			var spans = new List<MarkdownSpan>();
			if (string.IsNullOrEmpty(text)) return spans;

			// Regex for Link, Bold, Italic, StrikeThrough
			// Link: [text](url)
			// Bold: **text**
			// Italic: *text*
			// Strike: ~~text~~
			string pattern = @"(\[[^\]]+\]\([^\)]+\))|(\*\*[^\*]+\*\*)|(\*[^\*]+\*)|(~~[^~]+~~)";
			var matches = Regex.Matches(text, pattern);

			int currentIndex = 0;
			foreach (Match match in matches)
			{
				// Add normal text before match
				if (match.Index > currentIndex)
				{
					spans.Add(new MarkdownSpan(SpanType.Normal, text.Substring(currentIndex, match.Index - currentIndex)));
				}

				string value = match.Value;
				if (value.StartsWith("[") && value.Contains("](")) // Link
				{
					var matchLink = Regex.Match(value, @"\[([^\]]+)\]\(([^\)]+)\)");
					if (matchLink.Success)
					{
						spans.Add(new MarkdownSpan(SpanType.Link, matchLink.Groups[1].Value, matchLink.Groups[2].Value));
					}
					else
					{
						spans.Add(new MarkdownSpan(SpanType.Normal, value));
					}
				}
				else if (value.StartsWith("**")) // Bold
				{
					spans.Add(new MarkdownSpan(SpanType.Bold, value.Substring(2, value.Length - 4)));
				}
				else if (value.StartsWith("*")) // Italic
				{
					spans.Add(new MarkdownSpan(SpanType.Italic, value.Substring(1, value.Length - 2)));
				}
				else if (value.StartsWith("~~")) // StrikeThrough
				{
					spans.Add(new MarkdownSpan(SpanType.StrikeThrough, value.Substring(2, value.Length - 4)));
				}

				currentIndex = match.Index + match.Length;
			}

			// Add remaining text
			if (currentIndex < text.Length)
			{
				spans.Add(new MarkdownSpan(SpanType.Normal, text.Substring(currentIndex)));
			}

			return spans;
		}

		public static List<TextItemModel> CreateListFromMarkDown(string input)
		{
			string[] lines = input.Split('\n');
			List<TextItemModel> list = new();
			foreach (string rawLine in lines)
			{
				string line = rawLine.TrimEnd('\r', '\n');
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}
				// 忽略图片链接行，例如 ![界面图片0](./imgs/zh-cn/gui-0.png)
				else if (line.Trim().StartsWith("![") && line.Trim().Contains("]("))
				{
					continue;
				}
				// 忽略页面展示标题行，例如 "## 界面展示：" 或 "## Interface Display："
				else if (line.Trim().StartsWith("## 界面展示") || line.Trim().StartsWith("## Interface Display") || line.Trim().StartsWith("## 介面展示"))
				{
					continue;
				}
				// 忽略以 ~~ 开头的行（删除线行）
				else if (line.Trim().StartsWith("~~"))
				{
					continue;
				}
				else if (line.StartsWith("# "))
				{
					list.Add(new TextItemModel(line[2..], 26, "Bold", "Black"));
				}
				else if (line.StartsWith("## "))
				{
					list.Add(new TextItemModel(line[3..], 22, "Bold", "Black"));
				}
				else if (line.StartsWith("### "))
				{
					list.Add(new TextItemModel(line[4..], 18, "Bold", "Black"));
				}
				else if (line.StartsWith("#### "))
				{
					list.Add(new TextItemModel(line[5..], 16, "Bold", "Black"));
				}
				else if (line.StartsWith("##### "))
				{
					list.Add(new TextItemModel(line[6..], 14, "Bold", "Black"));
				}
				else if (line.StartsWith("###### "))
				{
					list.Add(new TextItemModel(line[7..], 12, "Bold", "Black"));
				}
				else if (line.StartsWith("> "))
				{
					// 处理引用块，使用显眼的红色粗体
					list.Add(new TextItemModel(line[2..], 16, "Bold", "Red"));
				}
				else
				{
					list.Add(new TextItemModel(line, 14, "Thin", "Gray"));
				}
			}
			return list;
		}
	}
}
