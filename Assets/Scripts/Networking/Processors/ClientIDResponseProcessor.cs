using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Unity.VisualScripting;

using UnityEngine;

namespace Networking
{
	[Processor(PackageType.ClientIDResponse, ProcessorAttribute.ProcessorType.Client)]
	public sealed class ClientIDResponseProcessor : IPackageProcessor
	{
		public Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			var package = new ClientIDPackageResponse();
			package.Deserialize(ref data, NetworkUtils.PackageHeaderSize);

			receiver.AssignID(package.ID);
			Debug.Log("Assigned ID - " + package.ID);
			return Task.FromResult(true);
		}

		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			var package = new ClientIDPackageResponse();
			package.Deserialize(data, package.GetOffset());

			(receiver as DebugClient).AssignID(package.ID);
			Debug.Log("Assigned ID - " + package.ID);
			return true;
		}
	}
}
