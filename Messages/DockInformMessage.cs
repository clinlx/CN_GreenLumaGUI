using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CN_GreenLumaGUI.Messages
{
	public class DockInformMessage : ValueChangedMessage<string>
	{
		public string messageText;
		public DockInformMessage(string value) : base(value)
		{
			messageText = value;
		}
	}
}
