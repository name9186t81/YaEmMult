using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace Networking
{
	[Processor(PackageType.NetworkObjectIDAssignement, ProcessorAttribute.ProcessorType.Client)]
	public class NetworkIDAssignmentProcessor : IPackageProcessor
	{
		public Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			throw new NotImplementedException();
		}

		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			var package = new NetworkObjectIDAssignmentPackage();
			package.Deserialize(data, NetworkUtils.GetOffset(package));

			if(NetworkManager.Instance == null || !NetworkManager.Instance.TryAssignID(package.ClientID, package.ServerID))
			{
				Debug.LogError("Failed to assign ID: " + package.ClientID + "/" + package.ServerID);
				return false;
			}

			return true;
		}
	}
}
