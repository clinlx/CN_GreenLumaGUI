using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CN_GreenLumaGUI.Messages
{
	public class CheckedNumChangedMessage : ValueChangedMessage<long>
	{
		public long targetId;
		public bool isDec = false;
		public CheckedNumChangedMessage(long value, bool dec) : base(value)
		{
			targetId = value;
			isDec = dec;
		}
	}
}
