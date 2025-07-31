using Networking;

using System;

namespace Core
{
	[Service(true)]
	public sealed class ListenersCombiner : IService
	{
		public Listener Server;
		public Listener Client;

		public event Action<Type> RemoveCallback;
	}
}
