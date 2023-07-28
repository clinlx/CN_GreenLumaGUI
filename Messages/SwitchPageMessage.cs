using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CN_GreenLumaGUI.Messages
{
	public class SwitchPageMessage : ValueChangedMessage<object>
	{
		public int pageIndex;
		public SwitchPageMessage(int value) : base(value)
		{
			pageIndex = value;
		}
	}
}
