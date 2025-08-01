using System.Net;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace Networking
{
	[Processor(PackageType.ChatMessage, ProcessorAttribute.ProcessorType.Both)]
	public class ChatMessageProcessor : IPackageProcessor
	{
		public Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			var package = new ChatMessagePackage();
			package.Deserialize(ref data, NetworkUtils.PackageHeaderSize);

			Debug.Log("Received Message - " + package.Message);
			return Task.FromResult(true);
		}
	}
}
