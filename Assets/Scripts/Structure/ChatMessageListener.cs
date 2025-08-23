using System;

namespace Core
{
	[Service(true)]
	public sealed class ChatMessageListener : IService
	{
		public event Action<Type> RemoveCallback;
		public event Action<byte, string> OnMessage;

		public void AddMessage(byte sender, string message)
		{
			OnMessage?.Invoke(sender, message);
		}
	}
}
