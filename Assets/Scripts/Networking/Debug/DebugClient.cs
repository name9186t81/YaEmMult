using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using UnityEngine;

namespace Networking
{
	public class DebugClient : ListenerBase
	{
		private byte _id = 255;
		private bool _isMaster = false;
		private bool _inited = false;
		private IPEndPoint _server;

		public DebugClient() : base(0, 1, 1, 20) { }

		protected override ProcessResult Process(PackageType type, ReadOnlySpan<byte> buffer, IPEndPoint point)
		{
			if (TryGetProcessorByType(type, out var processor))
			{
				try
				{
					processor.Process(buffer, CTS, point, this);
					return ProcessResult.Success;
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
					return ProcessResult.Fail;
				}
			}
			else
			{
				return ProcessResult.UnknowPackage;
			}
		}

		public void AssignID(byte id)
		{
			_id = id;
		}

		public async Task ConfirmConnection(IPEndPoint server)
		{
			_server = server;
			AddConnected(server);
			RequestClientIDPackage request = new RequestClientIDPackage();
			Debug.Log("Connection established");

			_isMaster = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Contains(server.Address) || IPAddress.IsLoopback(server.Address);
			BeginSyncingTime();

			await SendPackage(request, PackageSendOrder.Instant, PackageSendDestination.Concrete, server);
		}

		public async Task<bool> TryToConnect(IPEndPoint target)
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(NetworkUtils.PackageHeaderSize);
			NetworkUtils.PackageTypeToByteArray(PackageType.ConnectionRequest, ref buffer);
			await SendPackage(buffer, PackageSendOrder.Instant, PackageSendDestination.Concrete, target, true);
			return true;
		}

		public async void SendPackageToServerAsync(IPackage package, PackageSendOrder order = PackageSendOrder.NextTick) => await SendPackage(package, order);
		public async Task SendPackage(IPackage package, PackageSendOrder order = PackageSendOrder.NextTick)
		{
			await SendPackage(package, order, PackageSendDestination.Concrete, _server);
		}

		public void BeginSyncingTime()
		{
			var package = new TimeSyncRequest(RunTime);
			SendPackageToServerAsync(package);
			Debug.Log("Begin syncing time...");
		}

		public void TimeSyncFinished()
		{
			if (!_inited)
			{
				var package = new RequestNetworkObjectSpawnPackage(-1);
				SendPackageToServerAsync(package);
				var package2 = new SnapshotRequestPackage(-1);
				SendPackageToServerAsync(package2);
				_inited = true;
			}
		}

		public new void SyncTime(long offset)
		{
			base.SyncTime(offset);
		}

		public IPEndPoint Server => _server;
		public byte ID => _id;
		public bool IsMasterClient => _isMaster;
	}
}
