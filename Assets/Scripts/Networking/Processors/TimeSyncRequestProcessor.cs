using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
	[Processor(PackageType.TimeSync, ProcessorAttribute.ProcessorType.Server)]
	public class TimeSyncRequestProcessor : IPackageProcessor
	{
		public Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			throw new NotImplementedException();
		}

		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			var originalPackage = new TimeSyncRequest();
			originalPackage.Deserialize(data, NetworkUtils.GetOffset(originalPackage));

			var response = new TimeSyncResponse(originalPackage.ClientStamp, receiver.RunTime);
			receiver.SendAsync(response, ListenerBase.PackageSendOrder.Instant, ListenerBase.PackageSendDestination.Concrete, sender);
			return true;
		}
	}
}
