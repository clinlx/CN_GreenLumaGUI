using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CN_GreenLumaGUI.Messages
{
	public class LoadFinishedMessage : ValueChangedMessage<string>
	{
		public string messageText;
		public LoadFinishedMessage(string value) : base(value)
		{
			messageText = value;
		}
	}
}
