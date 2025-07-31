using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace Networking
{
	[Processor(PackageType.PingResponse, ProcessorAttribute.ProcessorType.Both)]
	public sealed class PingProcessor : IPackageProcessor
	{
		private static Dictionary<IPEndPoint, int> _clientToSequenceNumber = new Dictionary<IPEndPoint, int>();
		private long _prevTime;

		public async Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener r)
		{
			PingResponsePackage package = default;

			package.Deserialize(ref data, NetworkUtils.PackageHeaderSize);

			var current = package.ServerTimestamp;
			_prevTime = package.ServerTimestamp;

			//if (GetSequanceNumber(r) > package.SequanceNumber) return; //old ping package

			PingPackage response = new PingPackage(0, r.Time);
			await r.SendPackage(response, sender);
			return true;
		}
		/*
		private int GetSequanceNumber(Listener r)
		{
			if(_clientToSequenceNumber.TryGetValue(r, out int sequanceNumber))
				return sequanceNumber;

			_clientToSequenceNumber.Add(r, 0);
			return 0;
		}*/
	}
}
