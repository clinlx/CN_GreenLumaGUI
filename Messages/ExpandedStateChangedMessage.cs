using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CN_GreenLumaGUI.Messages
{
	public class ExpandedStateChangedMessage : ValueChangedMessage<bool>
	{
		public bool isExpanded;
		public ExpandedStateChangedMessage(bool value) : base(value)
		{
			isExpanded = value;
		}
	}
}
