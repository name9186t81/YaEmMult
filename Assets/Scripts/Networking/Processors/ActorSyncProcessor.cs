using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Unity.VisualScripting;

using UnityEngine;

namespace Networking
{
	[Processor(PackageType.ActorSync, ProcessorAttribute.ProcessorType.Server)]
	public sealed class ActorSyncProcessor : ITickBasedPackageProcessor
	{
		private readonly ConcurrentStack<IPEndPoint> _changedPositions = new ConcurrentStack<IPEndPoint>();
		private readonly ConcurrentDictionary<IPEndPoint, ActorSyncPackage> _latestPositions = new ConcurrentDictionary<IPEndPoint, ActorSyncPackage>();

		public bool NeedTick =>!_changedPositions.IsEmpty;

		public async Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			if (receiver.TryGetUserID(sender, out var id)) 
			{
				var receivedPackage = new ActorSyncPackage();
				receivedPackage.Deserialize(ref data, NetworkUtils.PackageHeaderSize);

				if (_latestPositions.TryGetValue(sender, out var latestPosition))
				{
					if(latestPosition.Tick < receivedPackage.Tick)
						_latestPositions[sender] = receivedPackage;
				}
				else
				{
					_changedPositions.Push(sender);
					_latestPositions.TryAdd(sender, receivedPackage);
				}
				return true;
			}
			return false;
		}
		
		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			if ((receiver as DebugServer).TryGetUserID(sender, out var id))
			{
				var receivedPackage = new ActorSyncPackage();
				receivedPackage.Deserialize(data, NetworkUtils.PackageHeaderSize);

				if (_latestPositions.TryGetValue(sender, out var latestPosition))
				{
					if (latestPosition.Tick < receivedPackage.Tick)
						_latestPositions[sender] = receivedPackage;
				}
				else
				{
					_changedPositions.Push(sender);
					_latestPositions.TryAdd(sender, receivedPackage);
				}
				return true;
			}
			return false;
		}

		public async Task Tick(float tickRate, Listener listener)
		{
			int singleSize = new ActorSyncFromServerPackage().Size;
			int totalSize = _changedPositions.Count * singleSize;

			Debug.Log("COUNT: " + _changedPositions.Count);

			while (!_changedPositions.IsEmpty)
			{
				int size = Math.Min(Listener.MAX_PACKAGE_SIZE, totalSize + NetworkUtils.PackageHeaderSize);
				byte[] data = new byte[size];

				int usedSize = NetworkUtils.PackageHeaderSize;
				NetworkUtils.PackageTypeToByteArray(PackageType.ActorSyncFromServer, ref data);

				while (usedSize < size)
				{
					if (!_changedPositions.TryPop(out var position))
					{
						await SendData(data, true, listener);
						return;
					}

					var val = _latestPositions[position];
					listener.TryGetUserID(position, out var id);
					var package = new ActorSyncFromServerPackage(val.Position, val.Rotation, id);
					package.Serialize(ref data, usedSize);
					usedSize += singleSize;
					_latestPositions.Remove(position, out _);
				}

				await SendData(data, false, listener);
			}
			return;
		}

		public async Task Tick(float tickRate, ListenerBase listener)
		{
			int singleSize = new ActorSyncFromServerPackage().Size;
			int totalSize = _changedPositions.Count * singleSize;

			Debug.Log("COUNT: " + _changedPositions.Count);

			while (!_changedPositions.IsEmpty)
			{
				int size = Math.Min(ListenerBase.MTU, totalSize + NetworkUtils.PackageHeaderSize);
				byte[] data = new byte[size];

				int usedSize = NetworkUtils.PackageHeaderSize;
				NetworkUtils.PackageTypeToByteArray(PackageType.ActorSyncFromServer, ref data);

				while (usedSize < size)
				{
					if (!_changedPositions.TryPop(out var position))
					{
						await SendData(data, true, listener);
						return;
					}

					var val = _latestPositions[position];
					(listener as DebugServer).TryGetUserID(position, out var id);
					var package = new ActorSyncFromServerPackage(val.Position, val.Rotation, id);
					package.Serialize(ref data, usedSize);
					usedSize += singleSize;
					_latestPositions.Remove(position, out _);
				}

				await SendData(data, false, listener);
			}
			return;
		}

		private async Task SendData(byte[] data, bool lastMessage, ListenerBase listener)
		{
			if (lastMessage)
			{
				_latestPositions.Clear();
				_changedPositions.Clear();
			}

			await listener.SendPackage(data, ListenerBase.PackageSendOrder.NextTick, ListenerBase.PackageSendDestination.Everyone);
		}

		private async Task SendData(byte[] data, bool lastMessage, Listener listener)
		{
			if (lastMessage)
			{
				_latestPositions.Clear();
				_changedPositions.Clear();
			}

			await listener.SendDataToEveryone(data);
		}
	}
}
