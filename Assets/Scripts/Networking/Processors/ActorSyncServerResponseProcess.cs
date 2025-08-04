using System.Net;
using System.Threading;
using System.Threading.Tasks;

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
	}
}
