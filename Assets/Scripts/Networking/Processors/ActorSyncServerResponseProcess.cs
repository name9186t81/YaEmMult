using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Unity.VisualScripting;

using UnityEngine;

namespace Networking
{
	[Processor(PackageType.ActorSyncFromServer, ProcessorAttribute.ProcessorType.Client)]
	public sealed class ActorSyncServerResponseProcess : IPackageProcessor
	{
		public Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			int usedData = NetworkUtils.PackageHeaderSize;

			while (usedData < data.Length)
			{
				var package = new ActorSyncFromServerPackage();
				package.Deserialize(ref data, usedData);

				DebugGlobalActorSyncer.Instance.ReceivePackage(package);
				Debug.Log("READ SYNC");
				usedData += package.Size;
			}

			return Task.FromResult(true);
		}

		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			int usedData = NetworkUtils.PackageHeaderSize;

			while (usedData < data.Length)
			{
				var package = new ActorSyncFromServerPackage();
				package.Deserialize(data, usedData);

				if (NetworkManager.Instance.TryGetNetworkObject(package.ID, out var obj))
				{
					obj.ApplyPackage(package);
				}
				else
				{
					Debug.LogWarning("Unknown id - " +  package.ID);
				}
				usedData += package.Size;
			}

			return true;
		}
	}
}
