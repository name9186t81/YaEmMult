using Networking;

using System;

namespace Core
{
	[Service(true)]
	public sealed class ListenersCombiner : IService
	{
		public DebugServer Server;
		public DebugClient Client;

		public event Action<Type> RemoveCallback;
	}
}
