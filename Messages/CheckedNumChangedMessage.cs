using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CN_GreenLumaGUI.Messages
{
	public class CheckedNumChangedMessage : ValueChangedMessage<long>
	{
		public long updateFrom;
		public CheckedNumChangedMessage(long value) : base(value)
		{
			updateFrom = value;
		}
	}
}
