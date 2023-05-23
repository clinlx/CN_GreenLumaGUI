using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CN_GreenLumaGUI.Messages
{
	public class GameListChangedMessage : ValueChangedMessage<long>
	{
		public long gameId;
		public GameListChangedMessage(long value) : base(value)
		{
			gameId = value;
		}
	}
}
