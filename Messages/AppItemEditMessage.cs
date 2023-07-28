using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CN_GreenLumaGUI.Messages
{
	public class AppItemEditMessage : ValueChangedMessage<object>
	{
		public object appitem;
		public AppItemEditMessage(object value) : base(value)
		{
			appitem = value;
		}
	}
}
