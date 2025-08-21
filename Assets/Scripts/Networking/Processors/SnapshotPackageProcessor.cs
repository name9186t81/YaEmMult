using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace Networking
{
	[Processor(PackageType.Snapshot, ProcessorAttribute.ProcessorType.Both)]
	public class SnapshotPackageProcessor : IPackageProcessor
	{
		public Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			throw new NotImplementedException();
		}

		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			var package = new SnapshotPackage();
			package.Deserialize(data, NetworkUtils.GetOffset(package)); //todo combine multiple network objects snapshots

			if (receiver is DebugServer server)
			{
				if (server.TryGetUserByID(package.Requester, out var user))
				{
					Debug.Log("Sending snapshot");
					server.SendAsync(package, ListenerBase.PackageSendOrder.NextTick, ListenerBase.PackageSendDestination.Concrete, user);
				}
				else
				{
					Debug.LogWarning("Unknown user id: " + package.Requester);
				}
			}
			else if (receiver is DebugClient client)
			{
				if(!NetworkBehaviourSynchronizer.TryToApplySnapshot(package.NetworkID, package.Data, 0, package.DataSize))
				{
					Debug.LogWarning("Failed to process snapshot with id " + package.NetworkID);
				}
			}

			return true;
		}
	}
}
