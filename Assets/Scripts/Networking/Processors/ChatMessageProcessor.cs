using Core;

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Unity.VisualScripting;

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

		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			var package = new ChatMessagePackage();
			package.Deserialize(data,  package.GetOffset());

			if (receiver is DebugServer server)
			{
				if (!server.TryGetUserID(sender, out var id))
				{
					Debug.LogError("Not registered");
					return false;
				}

				package.Client = id;
				receiver.SendAsync(package, ListenerBase.PackageSendOrder.NextTick, ListenerBase.PackageSendDestination.Everyone, sender);
			}
			else
			{
				ServiceLocator.Get<ChatMessageListener>().AddMessage(package.Client, package.Message);
				Debug.Log("Received Message - " + package.Message);
			}	
			return true;
		}
	}
}
