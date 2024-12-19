using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CN_GreenLumaGUI.Messages
{
	public class ManifestListChangedMessage : ValueChangedMessage<long>
	{
		public long manifestId;
		public ManifestListChangedMessage(long value) : base(value)
		{
			manifestId = value;
		}
	}
}
