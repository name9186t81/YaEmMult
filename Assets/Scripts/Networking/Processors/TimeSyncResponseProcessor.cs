using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace Networking
{
	[Processor(PackageType.TimeSyncResponse, ProcessorAttribute.ProcessorType.Client)]
	public sealed class TimeSyncResponseProcessor : IPackageProcessor
	{
		private int _currentIteration;
		private readonly long[] _offsets = new long[SYNC_TIME_ITERATIONS];

		public const int SYNC_TIME_ITERATIONS = 1;

		public Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			throw new NotImplementedException();
		}

		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			var response = new TimeSyncResponse();
			response.Deserialize(data, NetworkUtils.GetOffset(response));

			long currentTime = receiver.RunTime;
			long halfTime = currentTime / 2;

			long serverTime = response.ServerTimeStamp + halfTime;
			long clientTime = response.ClientTimeStamp - halfTime;
			_offsets[_currentIteration++] = serverTime - clientTime;

			if (_currentIteration == SYNC_TIME_ITERATIONS)
			{
				_currentIteration = 0;

				long sum = 0;
				for (int i = 0; i < _offsets.Length; i++)
				{
					sum += _offsets[i];
				}
				long averageOffset = sum / _offsets.Length;
				(receiver as DebugClient).SyncTime(averageOffset);
				Debug.Log("Time sync finished offset: " + averageOffset);
				(receiver as DebugClient).TimeSyncFinished();
			}
			else
			{
				var request = new TimeSyncRequest(receiver.RunTime);
				receiver.SendAsync(request, ListenerBase.PackageSendOrder.Instant, ListenerBase.PackageSendDestination.Concrete, sender);
			}

			return true;
		}
	}
}
