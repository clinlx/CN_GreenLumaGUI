using System.Collections.Generic;

namespace CN_GreenLumaGUI.Models
{
	public class TextItemModel
	{
		public string Text { get; set; }
		public int FontSize { get; set; }
		public string FontWeight { get; set; }
		public string Foreground { get; set; }
		public TextItemModel() : this("") { }
		public TextItemModel(string text) : this(text, 14, "Normal", "Black") { }
		public TextItemModel(string text, int fontSize, string fontWeight, string foreground)
		{
			Text = text;
			FontSize = fontSize;
			FontWeight = fontWeight;
			Foreground = foreground;
		}

		public static List<TextItemModel> CreateListFromMarkDown(string input)
		{
			string[] lines = input.Split('\n');
			List<TextItemModel> list = new();
			foreach (string line in lines)
			{
				if (line.Trim() == "")
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
				else
				{
					list.Add(new TextItemModel(line, 14, "Thin", "Gray"));
				}
			}
			return list;
		}
	}
}
