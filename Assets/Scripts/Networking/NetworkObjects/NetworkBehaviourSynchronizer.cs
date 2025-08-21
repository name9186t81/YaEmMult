using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

namespace Networking
{
	public static class NetworkBehaviourSynchronizer
	{
		private class Comparer : IComparer<INetworkBehaviourSystem>
		{
			public int Compare(INetworkBehaviourSystem x, INetworkBehaviourSystem y)
			{
				return Math.Sign(x.GetType().Name[0] - y.GetType().Name[0]);
			}
		}

		private static ConcurrentDictionary<Type, INetworkBehaviourSystem> _typeToSystem;
		private static ConcurrentDictionary<INetworkBehaviourSystem, int> _systemToID;
		private static ConcurrentDictionary<int, INetworkBehaviourSystem> _idToSystem;

		private static ConcurrentDictionary<int, (byte[], int, int)> _awaitingSnapshots;

		public static byte SystemHeaderSizeInBytes { get; private set; }

		static NetworkBehaviourSynchronizer()
		{
			_typeToSystem = new ConcurrentDictionary<Type, INetworkBehaviourSystem>();
			_systemToID = new ConcurrentDictionary<INetworkBehaviourSystem, int>();
			_idToSystem = new ConcurrentDictionary<int, INetworkBehaviourSystem>();
			_awaitingSnapshots = new ConcurrentDictionary<int, (byte[], int, int)>();

			List<INetworkBehaviourSystem> unsorted = new List<INetworkBehaviourSystem>();
			var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(INetworkBehaviourSystem).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
			foreach (var type in types)
			{
				object instance = null;
				try
				{
					instance = Activator.CreateInstance(type);
				}
				catch (Exception ex)
				{
					Debug.LogError("Failed to create " + type.Name + " system");
				}

				if (instance != null)
				{
					unsorted.Add((INetworkBehaviourSystem)instance);
					_typeToSystem.TryAdd(type, (INetworkBehaviourSystem)instance);
				}
			}

			unsorted.Sort(new Comparer());
			SystemHeaderSizeInBytes = (unsorted.Count < 256) ? (byte)1 : (byte)2;

			for (int i = 0; i < unsorted.Count; i++)
			{
				_systemToID.TryAdd(unsorted[i], i);
				_idToSystem.TryAdd(i, unsorted[i]);
			}
		}

		public static bool TryGetSnapshot(int networkObjectID, out (byte[] data, int offset, int size) snapshot)
		{
			return _awaitingSnapshots.TryRemove(networkObjectID, out snapshot);
		}

		public static bool TryToApplySnapshot(int networkObjectID, byte[] data, int startOffset, int size)
		{
			if (!NetworkManager.Instance.TryGetNetworkObject(networkObjectID, out var networkObject))
			{
				Debug.Log("Added snapshot for " + networkObjectID);
				_awaitingSnapshots.TryAdd(networkObjectID, (data, startOffset, size)); //cache snapshot in case the network object wasnt created yet
				return true;
			}

			networkObject.ApplySnapshot(data, startOffset, size);
			return true;
		}

		public static bool TryGetSnapshot(int networkObjectID, byte source)
		{
			if(!NetworkManager.Instance.TryGetNetworkObject(networkObjectID, out var networkObject))
			{
				return false;
			}

			networkObject.AddSnapshotRequest(source);
			return true;
		}

		public static bool TryGetSystem(int id, out INetworkBehaviourSystem system)
		{
			return _idToSystem.TryGetValue(id, out system);
		}

		public static bool TryGetSystemID<T>(out int val) where T : INetworkBehaviourSystem
		{
			if(_typeToSystem.TryGetValue(typeof(T), out var system)) return _systemToID.TryGetValue(system, out val);
			val = -1;
			return false;
		}

		public static bool TryGetSystemID(INetworkBehaviourSystem system, out int val)
		{
			return _systemToID.TryGetValue(system, out val);
		}
	}
}