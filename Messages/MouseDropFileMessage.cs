using CommunityToolkit.Mvvm.Messaging.Messages;

namespace CN_GreenLumaGUI.Messages
{
	public class MouseDropFileMessage
	{
		public string name;
		public string path;
		public MouseDropFileMessage(string name, string path)
		{
			this.name = name;
			this.path = path;
		}
	}
}
