using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CN_GreenLumaGUI.Messages
{
	public class PageChangedMessage : ValueChangedMessage<object>
	{
		public int fromPageIndex;
		public int toPageIndex;
		public PageChangedMessage(int from, int to) : base(to)
		{
			fromPageIndex = from;
			toPageIndex = to;
		}
	}
}
