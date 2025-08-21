using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
	[Processor(PackageType.ClientIDRequest, ProcessorAttribute.ProcessorType.Server)]
	public sealed class ClientIDRequestProcessor : IPackageProcessor
	{
		public async Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			if (!receiver.TryGetUserID(sender, out byte id))
			{
				id = receiver.NextUserID;
				receiver.RegisterUser(sender, id);
			}

			var response = new ClientIDPackageResponse(id);
			await receiver.SendPackage(response, sender);
			return true;
		}

		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			var server = receiver as DebugServer;

			if (!server.TryGetUserID(sender, out byte id))
			{
				server.RegisterUser(sender, out id);
			}

			var response = new ClientIDPackageResponse(id);
			receiver.SendAsync(response, ListenerBase.PackageSendOrder.Instant, ListenerBase.PackageSendDestination.Concrete, sender);
			return true;
		}
	}
}
