using System;
using System.Buffers;
using System.Net;
using System.Threading.Tasks;

using UnityEngine;

namespace Networking
{
	public class DebugClient : ListenerBase
	{
		private byte _id = 255;
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
			await SendPackage(request, PackageSendOrder.Instant, PackageSendDestination.Concrete, server);
		}

		public async Task<bool> TryToConnect(IPEndPoint target)
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(NetworkUtils.PackageHeaderSize);
			NetworkUtils.PackageTypeToByteArray(PackageType.ConnectionRequest, ref buffer);
			await SendPackage(buffer, PackageSendOrder.Instant, PackageSendDestination.Concrete, target, true);
			return true;
		}

		public async Task SendPackage(IPackage package, PackageSendOrder order = PackageSendOrder.NextTick)
		{
			await SendPackage(package, order, PackageSendDestination.Concrete, _server);
		}

		public IPEndPoint Server => _server;
		public byte ID => _id;
	}
}
