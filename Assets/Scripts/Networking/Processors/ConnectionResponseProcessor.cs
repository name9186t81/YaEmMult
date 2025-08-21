using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Unity.VisualScripting;

using UnityEngine;

namespace Networking
{
	[Processor(PackageType.ConnectionResponse, ProcessorAttribute.ProcessorType.Client)]
	internal class ConnectionResponseProcessor : IPackageProcessor
	{
		public async Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			var package = new ConnectionResponsePackage();
			package.Deserialize(ref data, NetworkUtils.PackageHeaderSize);

			return true;
		}

		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			var package = new ConnectionResponsePackage();
			package.Deserialize(data, package.GetOffset());

			if (package.Result == ConnectionResponseType.Success)
			{
				Debug.Log("Connection established");
				(receiver as DebugClient).ConfirmConnection(sender);
				return true;
			}

			return false;
		}
	}
}
