using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
	[Processor(PackageType.ActorSync, ProcessorAttribute.ProcessorType.Server)]
	public sealed class ActorSyncProcessor : IPackageProcessor
	{
		public async Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			if (receiver.TryGetUserID(sender, out var id)) 
			{
				var receivedPackage = new ActorSyncPackage();
				receivedPackage.Deserialize(ref data, NetworkUtils.PackageHeaderSize);

				await receiver.SendPackageToEveryoneExceptSender(new ActorSyncFromServerPackage(receivedPackage.Position, receivedPackage.Rotation, id), sender);
				return true;
			}
			return false;
		}
	}
}
