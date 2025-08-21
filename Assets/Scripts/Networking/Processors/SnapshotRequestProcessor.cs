using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace Networking
{
	[Processor(PackageType.SnapshotRequest, ProcessorAttribute.ProcessorType.Both)]
	public class SnapshotRequestProcessor : IPackageProcessor
	{
		public Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			throw new NotImplementedException();
		}

		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			var package = new SnapshotRequestPackage();
			package.Deserialize(data, NetworkUtils.GetOffset(package));

			if(receiver is DebugServer server)
			{
				if (!server.TryGetUserID(sender, out var senderID))
				{
					Debug.LogWarning("Request from unknown user " + sender.ToString());
					return false;
				}

				if (package.NetworkObjectID == -1)
				{
					foreach(var pair in server.AllNetworkObjects)
					{
						if (server.TryGetUserByID(pair.Value, out var user))
						{
							Debug.Log("Sending snapshot request to " + user + " net id: " + pair.Key + " user id: " + pair.Value + " sender ID: " + senderID);
							receiver.SendAsync(new SnapshotRequestPackage(pair.Key)
							{
								UserID = senderID
							}, ListenerBase.PackageSendOrder.NextTick, ListenerBase.PackageSendDestination.Concrete, user);
						}
						else
						{
							Debug.LogWarning("Unknown user with id " + senderID);
						}
					}
				}
				else
				{
					if(server.TryGetObjectOwner(package.NetworkObjectID, out var owner))
					{
						if (server.TryGetUserByID(owner, out var user))
						{
							receiver.SendAsync(new SnapshotRequestPackage(package.NetworkObjectID)
							{
								UserID = senderID
							}, ListenerBase.PackageSendOrder.NextTick, ListenerBase.PackageSendDestination.Concrete, user);
						}
						else
						{
							Debug.LogWarning("Unknown user with id " + owner);
						}
					}
					else
					{
						Debug.LogWarning("Unknown object id " + package.NetworkObjectID);
					}
				}
			}
			else if(receiver is DebugClient client)
			{
				Debug.Log("Getting id for " + package.NetworkObjectID + " user id - " + package.UserID);
				if(!NetworkBehaviourSynchronizer.TryGetSnapshot(package.NetworkObjectID, package.UserID))
				{
					Debug.LogError("failed to get snapshot");
					return false;
				}
			}

			return true;
		}
	}
}
