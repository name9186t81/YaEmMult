using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

using UnityEngine;

namespace Networking
{
	public class DebugServer : ListenerBase
	{
		private readonly ConcurrentDictionary<IPEndPoint, byte> _userIDs;
		private readonly ConcurrentDictionary<byte, IPEndPoint> _idToUser;
		private readonly ConcurrentStack<byte> _freeIDs;
		private readonly ConcurrentDictionary<int, byte> _objectOwners = new ConcurrentDictionary<int, byte>();
		private byte _currentID;
		private int _networkObjectID = 255;

		public DebugServer(int port, int listenersCount = -1, int processingCount = -1, long tickFrequancy = -1) : base(port, listenersCount, processingCount, tickFrequancy)
		{
			_userIDs = new ConcurrentDictionary<IPEndPoint, byte>();
			_idToUser = new ConcurrentDictionary<byte, IPEndPoint>();
			_freeIDs = new ConcurrentStack<byte>();
		}

		protected override ProcessResult Process(PackageType type, ReadOnlySpan<byte> buffer, IPEndPoint point)
		{
			if (TryGetProcessorByType(type, out var processor))
			{
				try
				{
					processor.Process(buffer, CTS, point, this);
					return ProcessResult.Success;
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
					return ProcessResult.Fail;
				}
			}
			else
			{
				return ProcessResult.UnknowPackage;
			}
		}

		protected override void Ticked()
		{
			ListenerBase.ProcessTicked(this, 1f / 20f);
		}
		public void RegisterUser(IPEndPoint point, out byte id)
		{
			if (_userIDs.TryGetValue(point, out id)) return;

			id = GetNextID();
			_userIDs.TryAdd(point, id);
			_idToUser.TryAdd(id, point);
		}

		public override void RemoveConnected(IPEndPoint client)
		{
			if (_userIDs.TryRemove(client, out var userID))
			{
				_idToUser.TryRemove(userID, out _);
				_freeIDs.Push(userID);
			}
		}

		public bool TryGetUserID(IPEndPoint point, out byte id) => _userIDs.TryGetValue(point, out id);
		public bool TryGetUserByID(byte id, out IPEndPoint user) => _idToUser.TryGetValue(id, out user);
		
		private byte GetNextID()
		{
			if(_freeIDs.TryPop(out var id))
			{
				return id;
			}

			return _currentID++;
		}

		public void AddObjectOwner(int networkID, byte userID)
		{
			_objectOwners.TryAdd(networkID, userID);
		}

		public void RemoveObjectOwner(int networkID)
		{
			_objectOwners.TryRemove(networkID, out _);
		}

		public bool TryGetObjectOwner(int objectID, out byte userID) => _objectOwners.TryGetValue(objectID, out userID);

		public IEnumerable<KeyValuePair<int, byte>> AllNetworkObjects => _objectOwners;

		public int NetworkObjectID => _networkObjectID++;
	}
}