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
	}
}
