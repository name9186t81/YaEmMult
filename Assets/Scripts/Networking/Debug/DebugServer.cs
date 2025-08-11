using System;
using System.Collections.Concurrent;
using System.Net;

using UnityEngine;

namespace Networking
{
	public class DebugServer : ListenerBase
	{
		private readonly ConcurrentDictionary<IPEndPoint, byte> _userIDs;
		private readonly ConcurrentStack<byte> _freeIDs;
		private byte _currentID;

		public DebugServer(int port, int listenersCount = -1, int processingCount = -1, long tickFrequancy = -1) : base(port, listenersCount, processingCount, tickFrequancy)
		{
			_userIDs = new ConcurrentDictionary<IPEndPoint, byte>();
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
		}

		public override void RemoveConnected(IPEndPoint client)
		{
			if (_userIDs.TryRemove(client, out var userID))
			{
				_freeIDs.Push(userID);
			}
		}

		public bool TryGetUserID(IPEndPoint point, out byte id) => _userIDs.TryGetValue(point, out id);
		
		private byte GetNextID()
		{
			if(_freeIDs.TryPop(out var id))
			{
				return id;
			}

			return _currentID++;
		}
	}
}