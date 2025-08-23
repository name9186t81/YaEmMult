using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace Networking
{
	[Processor(PackageType.ActorAction, ProcessorAttribute.ProcessorType.Both)]
	public sealed class ActorActionProcessor : IPackageProcessor
	{
		public Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			throw new NotImplementedException();
		}

		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			var package = new ActorActionPackage();
			package.Deserialize(data, NetworkUtils.GetOffset(package));

			if(receiver is DebugClient client)
			{
				if(NetworkManager.Instance.TryGetNetworkObject(package.NetworkID, out var obj))
				{
					obj.ApplyPackage(package);
				}
				else 
				{
					Debug.LogWarning("Received package for unknown id"); //todo request network sync for that id
				}
			}
			else
			{
				var server = receiver as DebugServer;
				if(server.TryGetObjectOwner(package.NetworkID, out var obj))
				{
					//todo add verification for object ownership
					if(server.TryGetUserByID(obj, out var user))
					{
						server.SendAsync(package, ListenerBase.PackageSendOrder.NextTick, ListenerBase.PackageSendDestination.EveryoneExcept, user);
					}
				}
				else
				{
					Debug.LogWarning("Unknown id");
				}
			}

			return true;
		}
	}
}
