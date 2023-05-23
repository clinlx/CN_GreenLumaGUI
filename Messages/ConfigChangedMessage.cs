using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CN_GreenLumaGUI.Messages
{
	public class ConfigChangedMessage : ValueChangedMessage<string>
	{
		public string kind;
		public ConfigChangedMessage(string value) : base(value)
		{
			kind = value;
		}
	}
}
