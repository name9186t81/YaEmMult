using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace Networking
{
	[Processor(PackageType.NetworkObjectSpawn, ProcessorAttribute.ProcessorType.Both)]
	public class NetworkObjectSpawnProcessor : IPackageProcessor
	{
		public Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver)
		{
			throw new NotImplementedException();
		}

		public bool Process(ReadOnlySpan<byte> data, CancellationTokenSource cts, IPEndPoint sender, ListenerBase receiver)
		{
			var package = new NetworkObjectSpawnPackage();
			package.Deserialize(data, NetworkUtils.GetOffset(package));

			if (receiver is DebugClient)
			{
				SolveForClient(ref package);
			}
			else if (receiver is DebugServer server)
			{
				SolveForServer(ref package, sender, server);
			}

			return true;
		}

		private void SolveForServer(ref NetworkObjectSpawnPackage package, IPEndPoint sender, DebugServer server)
		{
			if(!server.TryGetUserID(sender, out var userID))
			{
				Debug.LogWarning("Request from unknown user..." + sender.ToString());
				return;
			}
			//todo add check for admin permissions

			int id = server.NetworkObjectID;
			int clientID = package.ClientID;
			package.ClientID = id;

			server.AddObjectOwner(id, userID);
			server.SendAsync(package, ListenerBase.PackageSendOrder.NextTick, ListenerBase.PackageSendDestination.EveryoneExcept, sender);
			server.SendAsync(new NetworkObjectIDAssignmentPackage(clientID, id), ListenerBase.PackageSendOrder.NextTick, ListenerBase.PackageSendDestination.Concrete, sender);
		}

		private void SolveForClient(ref NetworkObjectSpawnPackage package)
		{
			if(NetworkManager.Instance != null)
			{
				Debug.Log("Initing object at " + package.Position + " rotation: " + package.Rotation + " flags: " + (int)package.Flags);
				NetworkManager.Instance.TryToInit(package.SpawnID, package.ClientID, package.Position, NetworkUtils.GetRotation(package.Rotation), package.Client, package.CustomData);
			}
		}
	}
}
