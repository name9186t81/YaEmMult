using Core;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Net;

using UnityEngine;

namespace Networking
{
	public abstract class NetworkMonoBehaviour : MonoBehaviour
	{
		[Flags]
		public enum SyncSettings : byte
		{
			SyncPosition = 1 << 0,
			SyncRotation = 1 << 1,
			SyncCustomData = 1 << 2
		}

		public enum AllowedPermission
		{
			Everyone,
			OnlyMaster
		}

		[SerializeField] private AllowedPermission _permission;
		[SerializeField] private SyncSettings _settings = (SyncSettings)3;
		private ConcurrentQueue<byte> _snapshotRequest = new ConcurrentQueue<byte>();
		private ConcurrentQueue<IPackage> _pendingPackages = new ConcurrentQueue<IPackage>();
		private ConcurrentQueue<(byte[] data, int startOffset, int size)> _pendingSnapshot = new ConcurrentQueue<(byte[] data, int startOffset, int size)>();
		private int _netID = -1;
		private int _spawnID = -1;
		private byte _owner;
		private bool _isSynced;
		private bool _isOwner;
		private bool _sendMessage = true;

		public Vector2 Position { get; private set; }
		public float Rotation { get; private set; }

		protected virtual void Start()
		{
			Position = transform.position;
			Rotation = transform.eulerAngles.z;

			if (_netID == -1)
			{
				Debug.LogError("Do not manually spawn network objects prefabs use network manager instead");
				Destroy(gameObject);
				return;
			}
		}

		protected virtual void Update()
		{
			Position = transform.position;
			Rotation = transform.eulerAngles.z;

			if (_snapshotRequest.Count != 0)
			{
				var snapshot = GetSnapshot();

				while (_snapshotRequest.TryDequeue(out var point))
				{
					ServiceLocator.Get<ListenersCombiner>().Client.SendPackageToServerAsync(new SnapshotPackage(_netID, point, snapshot), ListenerBase.PackageSendOrder.Instant);
				}
			}

			while(_pendingSnapshot.TryDequeue(out var val))
			{
				TryProcessSnapshot(val.data, val.startOffset, val.size);
			}

			while(_pendingPackages.TryDequeue(out var val))
			{
				if (!ProcessPackageInternal(val))
				{
					Debug.LogError("Failed to process " + val.Type + " package");
				}
			}
		}

		protected void SendPackageToServer(IPackage package, ListenerBase.PackageSendOrder order)
		{
			if(ServiceLocator.Get<ListenersCombiner>().Client != null)
				ServiceLocator.Get<ListenersCombiner>().Client.SendPackageToServerAsync(package, ListenerBase.PackageSendOrder.Instant);
		}

		public void SetOwner(bool owner) => _isOwner = owner;
		public void SetOwnerID(byte id) => _owner = id;

		/*
		public void Sync()
		{
			if (_isSynced) return;

			var res = NetworkManager.Instance.TryToSpawn(this, transform.position, transform.rotation.eulerAngles.z);
			_isSynced = _isOwner = res == NetworkManager.SpawnResult.Success;
			if (res == NetworkManager.SpawnResult.Error)
			{
				Debug.LogError("Error ocurred while spawning " + gameObject.name + " object");
				DestroyBehaviour(false);
			}
			else if (res == NetworkManager.SpawnResult.NoPermission)
			{
				Debug.LogError("No permission to spawn " + gameObject.name + " object");
				DestroyBehaviour(false);
			}
		}
		*/

		public void AssignID(int id, bool isServerID)
		{
			_netID = id;

			if (isServerID)
			{
				if(NetworkBehaviourSynchronizer.TryGetSnapshot(_netID, out var snapshot))
				{
					ApplySnapshot(snapshot.data, snapshot.offset, snapshot.size);
				}
			}
		}
		public virtual byte[] GetInitData(object initData) { return Array.Empty<byte>(); }
		public virtual void Init(byte[] initData) { _isSynced = true; }
		public virtual IEnumerable<int> GetAllRequiredSystemsForSnapshot() { return Array.Empty<int>(); }

		public void ApplyPackage(IPackage package)
		{
			_pendingPackages.Enqueue(package);
		}

		public void DestroyBehaviour(bool sendMessage)
		{
			_sendMessage = sendMessage;
			Destroy(gameObject);
		}

		protected virtual bool ProcessPackageInternal(IPackage package) {  return true; }

		protected virtual void OnDestroy()
		{
			if (_sendMessage)
			{
				NetworkManager.Instance.DestroyBehaviour(this);
			}
		}

		public void AddSnapshotRequest(byte source)
		{
			_snapshotRequest.Enqueue(source);
		}

		public void ApplySnapshot(byte[] data, int startOffset, int size)
		{
			_pendingSnapshot.Enqueue((data, startOffset, size));
		}

		private bool TryProcessSnapshot(byte[] data, int startOffset, int size)
		{
			int systemOffset = 0;

			while (systemOffset < size)
			{
				int type = (NetworkBehaviourSynchronizer.SystemHeaderSizeInBytes == 1 ? data[systemOffset + startOffset] : BitConverter.ToInt16(data, systemOffset + startOffset));

				if (!NetworkBehaviourSynchronizer.TryGetSystem(type, out var system))
				{
					Debug.LogError("Failed to find type - " + type);
					return false;
				}

				int dataSize = system.MaxSize == INetworkBehaviourSystem.HeaderSize.Byte ? data[systemOffset + startOffset + NetworkBehaviourSynchronizer.SystemHeaderSizeInBytes] : BitConverter.ToInt16(data, systemOffset + startOffset + NetworkBehaviourSynchronizer.SystemHeaderSizeInBytes);
				system.TryProcessShapshot(this, systemOffset + startOffset + NetworkBehaviourSynchronizer.SystemHeaderSizeInBytes + (int)system.MaxSize, dataSize, data);

				systemOffset += dataSize + NetworkBehaviourSynchronizer.SystemHeaderSizeInBytes + (int)system.MaxSize;
			}

			return true;
		}

		private byte[] GetSnapshot()
		{
			var systems = GetAllRequiredSystemsForSnapshot();
			List<INetworkBehaviourSystem> activeSystems = new List<INetworkBehaviourSystem>();
			Dictionary<INetworkBehaviourSystem, int> systemToSize = new Dictionary<INetworkBehaviourSystem, int>();
			int size = 0;

			foreach (var system in systems)
			{
				if (system == -1) continue;

				if (!NetworkBehaviourSynchronizer.TryGetSystem(system, out var localSystem))
				{
					Debug.LogError("Failed to add system with id - " + system);
					continue;
				}

				int locSize = localSystem.GetSizeFor(this);
				if(locSize == 0) continue;

				activeSystems.Add(localSystem);
				size += NetworkBehaviourSynchronizer.SystemHeaderSizeInBytes;
				size += (int)localSystem.MaxSize;
				systemToSize.Add(localSystem, locSize);
				size += locSize;
			}

			if (size > ListenerBase.MTU)
			{
				Debug.LogError("Data overflow when creating snapshot for " + gameObject.name + " data size: " + size);
				return null;
			}

			byte[] array = new byte[size];
			int systemOffset = 0;
			foreach (var system in activeSystems)
			{
				if(!NetworkBehaviourSynchronizer.TryGetSystemID(system, out var val))
				{
					Debug.LogError("Failed to find system with id " + val);
					continue;
				}
				val.Convert(ref array, systemOffset, NetworkBehaviourSynchronizer.SystemHeaderSizeInBytes);
				systemToSize[system].Convert(ref array, systemOffset + NetworkBehaviourSynchronizer.SystemHeaderSizeInBytes, (int)system.MaxSize);

				if (!system.TryTakeShapshot(this, systemOffset + NetworkBehaviourSynchronizer.SystemHeaderSizeInBytes + (int)system.MaxSize, array))
				{
					Debug.LogError("Failed to take snapshot system: " + system.ToString() + " for object: " + gameObject.name);
				}

				systemOffset += systemToSize[system] + NetworkBehaviourSynchronizer.SystemHeaderSizeInBytes + (int)system.MaxSize;
			}

			return array;
		}

		public void AssignSpawnID(int id)
		{
			_spawnID = id;
		}

		public bool IsOwner => _isOwner;
		public int ID => _netID;
		public int OwnerID => _owner;
		public int SpawnID => _spawnID;
		public AllowedPermission Permission => _permission;
		public SyncSettings Settings => _settings;
	}
}