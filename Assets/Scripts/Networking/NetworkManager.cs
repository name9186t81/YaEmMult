using Core;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using YaEm;

namespace Networking
{
	public sealed class NetworkManager : MonoBehaviour
	{
		public enum SpawnResult
		{
			Success,
			Error,
			NoPermission
		}

		[SerializeField] private NetworkMonoBehaviour[] _networkPrefabs;
		private Dictionary<NetworkMonoBehaviour, int> _behaviourToID = new Dictionary<NetworkMonoBehaviour, int>();
		private Dictionary<int, NetworkMonoBehaviour> _pendingingObjectsForServerID = new Dictionary<int, NetworkMonoBehaviour>();
		private Dictionary<int, NetworkMonoBehaviour> _spawnedBehaviours = new Dictionary<int, NetworkMonoBehaviour>();
		private ConcurrentQueue<(int, int, Vector2, float, byte[])> _awaitingSpawns = new ConcurrentQueue<(int, int, Vector2, float, byte[])>();
		private int _currentLocalID = 1;

		private static NetworkManager _instance;
		public static NetworkManager Instance => _instance;

		private void Awake()
		{
			gameObject.name = "NETWORK MANAGER";
			DontDestroyOnLoad(gameObject);

			if(_instance != null)
			{
				Debug.LogError("Instance of network manager already exists");
				Destroy(gameObject);
				return;
			}
			else
			{
				_instance = this;
			}

			int ind = 0;
			foreach (var instance in _networkPrefabs)
			{
				_behaviourToID.Add(instance, ind++);
			}
		}

		private void Update()
		{
			while(_awaitingSpawns.TryDequeue(out var element))
			{
				TryToInitInternal(element.Item1, element.Item2, element.Item3, element.Item4, element.Item5);
			}
		}

		public bool TryToInit(int spawnInd, int networkID, Vector2 position, float rotation, byte[] data)
		{
			_awaitingSpawns.Enqueue((spawnInd, networkID, position, rotation, data));
			return true;
		}

		private bool TryToInitInternal(int spawnInd, int networkID, Vector2 position, float rotation, byte[] data)
		{
			var obj = _networkPrefabs[spawnInd];
			if (obj == default) return false;

			var instance = Instantiate(obj, position, Quaternion.Euler(0, 0, rotation));

			try
			{
				instance.Init(data);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to instantiate " + obj.name + " error: " + e.Message);
				return false;
			}

			_spawnedBehaviours.Add(networkID, instance);
			instance.AssignID(networkID, true);
			return true;
		}

		public bool TryAssignID(int localID, int serverID)
		{
			var obj = _pendingingObjectsForServerID.TryGetValue(localID, out var val);
			if(obj == false) return false;

			_pendingingObjectsForServerID.Remove(localID);
			_spawnedBehaviours.Add(serverID, val);
			val.AssignID(serverID, true);
			return true;
		}

		public SpawnResult TryToSpawn(NetworkMonoBehaviour behaviourPrefab, Vector2 position, float rotation, out NetworkMonoBehaviour obj, object initArgs = null)
		{
			int ind = -1;
			obj = null;
			if (!_behaviourToID.TryGetValue(behaviourPrefab, out ind))
			{
				Debug.LogError("Cannot find index for object " + behaviourPrefab.name + " make sure you added prefab to network manager.");
				return SpawnResult.Error;
			}
			if (ServiceLocator.TryGet<ListenersCombiner>(out var combiner) && combiner.Client == null)
			{
				Debug.LogError("Client not initiated");
				return SpawnResult.Error;
			}

			if (behaviourPrefab.Permission == NetworkMonoBehaviour.AllowedPermission.OnlyMaster && !combiner.Client.IsMasterClient) return SpawnResult.NoPermission;

			var customData = behaviourPrefab.GetInitData(initArgs);
			if (customData.Length > ListenerBase.MTU)
			{
				Debug.LogError("Data overwflow");
				return SpawnResult.Error;
			}

			var clone = Instantiate(behaviourPrefab, position, Quaternion.Euler(0, 0, rotation));
			int id = CurrentID;
			clone.AssignSpawnID(ind);
			clone.AssignID(id, false);
			clone.SetOwner(true);
			_pendingingObjectsForServerID.Add(id, clone);

			var flags = clone.Settings;
			flags |= customData.Length > 0 ? NetworkMonoBehaviour.SyncSettings.SyncCustomData : 0;
			if (clone.transform.parent != null && clone.transform.parent.GetComponentInParent<NetworkMonoBehaviour>() != null)
			{
				Debug.Log("Parent has network monobehaviour");
				flags &= ~(NetworkMonoBehaviour.SyncSettings.SyncRotation | NetworkMonoBehaviour.SyncSettings.SyncPosition);
			}
			byte compressedRotation = NetworkUtils.CompressRotation(rotation);
			Debug.Log("Sending spawning flags: " + flags + " position: " + position + " rotation: " + rotation + " original settings: " + clone.Settings);
			combiner.Client.SendPackageToServerAsync(new NetworkObjectSpawnPackage(flags, id, ind, position, compressedRotation, customData));
			obj = clone;
			return SpawnResult.Success;
		}

		public void DestroyBehaviour(NetworkMonoBehaviour behaviour)
		{
			if (!behaviour.IsOwner)
			{
				Debug.LogError("Cannot destroy not owned objects");
				return;
			}

			if(ServiceLocator.TryGet<ListenersCombiner>(out var combiner) && combiner.Client != null)
			{
				combiner.Client.SendPackageToServerAsync(new NetworkObjectDestroyPackage(behaviour.ID));
			}
		}

		public void DestroyBehaviour(int id)
		{
			if (_spawnedBehaviours.TryGetValue(id, out var behaviour))
			{
				Destroy(behaviour.gameObject);
			}
			else
			{
				Debug.LogError("Cannot find network behaviour with id " + id);
			}
		}

		public bool TryGetNetworkObject(int id, out NetworkMonoBehaviour behaviour)
		{
			return _spawnedBehaviours.TryGetValue(id, out behaviour);
		}

		private int CurrentID => _currentLocalID++;
	}
}