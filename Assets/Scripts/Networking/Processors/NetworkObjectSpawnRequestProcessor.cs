using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
	[Processor(PackageType.NetworkObjectRequest, ProcessorAttribute.ProcessorType.Server)]
	public class NetworkObjectSpawnRequestProcessor : IPackageProcessor
	{
		public Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			throw new NotImplementedException();
		}

		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			var package = new RequestNetworkObjectSpawnPackage();
			package.Deserialize(data, NetworkUtils.GetOffset(package));
			var server = receiver as DebugServer;

			if(package.NetworkID == -1)
			{
				List<NetworkMonoBehaviour> behaviours = new List<NetworkMonoBehaviour>();
				foreach(var obj in server.AllNetworkObjects) //todo might be a bad idea request each client to get init data instead
				{
					if(NetworkManager.Instance.TryGetNetworkObject(obj.Key, out var behaviour))
					{
						behaviours.Add(behaviour);
					}
				}

				//todo batch net objects
				foreach(var obj in behaviours)
				{
					float rotation = obj.Rotation;
					byte compressed = (byte)((rotation % MathF.PI * 2) * 255);
					byte[] objData = obj.GetInitData(null);
					var flags = obj.Settings;
					if(objData.Length == 0)
					{
						flags &= ~NetworkMonoBehaviour.SyncSettings.SyncCustomData;
					}

					receiver.SendAsync(new NetworkObjectSpawnPackage(flags, obj.ID, obj.SpawnID, obj.Position, compressed, objData), ListenerBase.PackageSendOrder.NextTick, ListenerBase.PackageSendDestination.Concrete, sender);
				}
			}
			else
			{
				if(NetworkManager.Instance.TryGetNetworkObject(package.NetworkID, out var behaviour))
				{
					float rotation = behaviour.Rotation;
					byte compressed = (byte)((rotation % MathF.PI * 2) * 255);
					byte[] objData = behaviour.GetInitData(null);
					var flags = behaviour.Settings;
					if (objData.Length == 0)
					{
						flags &= ~NetworkMonoBehaviour.SyncSettings.SyncCustomData;
					}

					receiver.SendAsync(new NetworkObjectSpawnPackage(flags, behaviour.ID, behaviour.SpawnID, behaviour.Position, compressed, objData), ListenerBase.PackageSendOrder.NextTick, ListenerBase.PackageSendDestination.Concrete, sender);
				}
			}

			return true;
		}
	}
}
