using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace Networking
{
	[Processor(PackageType.ConnectionRequest, ProcessorAttribute.ProcessorType.Server)]
	public sealed class ConnectionRequestProcessor : IPackageProcessor
	{
		public async Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			var package = new ConnectionResponsePackage(ConnectionResponseType.Success);
			Debug.Log("SENDER: " + sender.ToString());
			receiver.AddConnectedUser(sender);
			await receiver.SendPackage(package, sender);
			return true;
		}
	}
}
