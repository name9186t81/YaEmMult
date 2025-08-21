using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace Networking
{
	[Processor(PackageType.NetworkObjectDestroy, ProcessorAttribute.ProcessorType.Both)]
	internal class NetworkObjectDestroyProcessor : IPackageProcessor
	{
		public Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			throw new NotImplementedException();
		}

		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			var package = new NetworkObjectDestroyPackage();
			package.Deserialize(data, NetworkUtils.GetOffset(package));

			if(receiver is DebugServer server)
			{
				if(!server.TryGetUserID(sender, out var userID))
				{
					Debug.LogWarning("Request from unknown user..." + sender.ToString());
					return false;
				}
				//todo add validation

				server.RemoveObjectOwner(package.ID);
				server.SendAsync(package, ListenerBase.PackageSendOrder.NextTick, ListenerBase.PackageSendDestination.EveryoneExcept, sender);
			}
			else
			{
				NetworkManager.Instance.DestroyBehaviour(package.ID);
			}

			return true;
		}
	}
}
