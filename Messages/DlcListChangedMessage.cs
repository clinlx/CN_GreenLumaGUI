using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CN_GreenLumaGUI.Messages
{
	public class DlcListChangedMessage : ValueChangedMessage<long>
	{
		public long dlcId;
		public DlcListChangedMessage(long value) : base(value)
		{
			dlcId = value;
		}
	}
}
