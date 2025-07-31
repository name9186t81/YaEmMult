using Core;

using System;
using System.Runtime.CompilerServices;

namespace Networking
{
	public sealed class NetworkingInfoContainer : IService
	{
		private ConnectionData _connectionData;

		public event Action<Type> RemoveCallback;

		public static NetworkingInfoContainer Instance => ServiceLocator.Get<NetworkingInfoContainer>();
		static NetworkingInfoContainer()
		{
			ServiceLocator.Register(new  NetworkingInfoContainer());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void UpdateConnectionData(ref ConnectionData connectionData)
		{
			_connectionData = connectionData;
		}

		public ConnectionData ConnectionData => _connectionData;
	}
}
