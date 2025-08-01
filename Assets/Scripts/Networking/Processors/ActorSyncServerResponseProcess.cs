using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
	[Processor(PackageType.ActorSyncFromServer, ProcessorAttribute.ProcessorType.Client)]
	public sealed class ActorSyncServerResponseProcess : IPackageProcessor
	{
		public Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			var package = new ActorSyncFromServerPackage();
			package.Deserialize(ref data, NetworkUtils.PackageHeaderSize);

			DebugGlobalActorSyncer.Instance.ReceivePackage(package);
			return Task.FromResult(true);
		}
	}
}
