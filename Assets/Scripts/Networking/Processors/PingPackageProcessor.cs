using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Unity.VisualScripting;

namespace Networking
{
	[Processor(PackageType.Ping, ProcessorAttribute.ProcessorType.Both)]
	public class PingPackageProcessor : IPackageProcessor
	{
		private static Dictionary<IPEndPoint, int> _clientToSequenceNumber = new Dictionary<IPEndPoint, int>();
		private long _prevTime;

		public async Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener r)
		{
			PingPackage package = default;

			package.Deserialize(ref data, NetworkUtils.PackageHeaderSize);

			var current = package.Timestamp;
			_prevTime = package.Timestamp;

			//if (GetSequanceNumber(r) > package.SequanceNumber) return; //old ping package

			PingResponsePackage response = new PingResponsePackage(0, r.Time);
			await r.SendPackage(response, sender);
			return true;
		}

		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			PingPackage package = default;

			package.Deserialize(data, NetworkUtils.PackageHeaderSize);

			var current = package.Timestamp;
			_prevTime = package.Timestamp;

			//if (GetSequanceNumber(r) > package.SequanceNumber) return; //old ping package

			PingResponsePackage response = new PingResponsePackage(0, receiver.RunTime);
			receiver.SendPackage(response, ListenerBase.PackageSendOrder.AfterProcessing, ListenerBase.PackageSendDestination.Concrete, sender);
			return true;
		}
	}
}
