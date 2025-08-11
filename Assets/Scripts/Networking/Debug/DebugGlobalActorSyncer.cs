using Core;

using System.Collections.Concurrent;
using System.Collections.Generic;

using UnityEngine;

namespace Networking
{
	public class DebugGlobalActorSyncer : MonoBehaviour
	{
		[SerializeField] private GameObject _movePrefab;
		private Dictionary<int, GameObject> _idToGameObject;
		private Dictionary<int, Vector2[]> _idToPositions = new Dictionary<int, Vector2[]>();
		private ConcurrentQueue<ActorSyncFromServerPackage> _pendingPackages;
		private long _prevTime;
		private float _delay;

		private static DebugGlobalActorSyncer _instance;
		public static DebugGlobalActorSyncer Instance { get { return _instance; } }

		private void Awake()
		{
			_instance = this;
			_idToGameObject = new Dictionary<int, GameObject>();
			_pendingPackages = new ConcurrentQueue<ActorSyncFromServerPackage>();
		}

		private void Update()
		{
			while(_pendingPackages.TryDequeue(out var res))
			{
				ProcessPackage(res);
			}
		}

		private void ProcessPackage(in ActorSyncFromServerPackage package)
		{
			if (ServiceLocator.TryGet<ListenersCombiner>(out var combiners) && combiners.Client != null)
			{
				if (_idToGameObject.TryGetValue(package.ID, out GameObject obj))
				{
					obj.transform.position = package.Position;
					obj.transform.rotation = Quaternion.Euler(0, 0, package.Rotation);
				}
				else
				{
					var instance = Instantiate(_movePrefab);
					_idToGameObject.Add(package.ID, instance);

					instance.transform.position = package.Position;
					instance.transform.rotation = Quaternion.Euler(0, 0, package.Rotation);
				}
			}
		}

		public void ReceivePackage(in ActorSyncFromServerPackage package)
		{
			if (ServiceLocator.TryGet<ListenersCombiner>(out var combiners) && combiners.Client != null)
			{
				var time = combiners.Client.RunTime;
				if(time - _prevTime > 2)
				{
					_delay = time - _prevTime;
					_prevTime = time;
				}

				if(_idToPositions.TryGetValue(package.ID, out var poss))
				{
					poss[2] = poss[1];
					poss[1] = poss[0];
					poss[0] = package.Position;
				}
				else
				{
					_idToPositions.Add(package.ID, new Vector2[3] {package.Position, package.Position, package.Position});
				}

				int selfID = combiners.Client.ID;

				if (package.ID == selfID)
				{
					DrawQuad(package.Position, Color.magenta);
					Vector2 p3 = _idToPositions[selfID][2];
					Vector2 p2 = _idToPositions[selfID][1];
					Vector2 p = _idToPositions[selfID][0];

					Vector2 vel = (p - p2) / (_delay * 0.01f);
					Vector2 vel2 = (p2 - p3) / (_delay * 0.01f);
					Vector2 ac = (vel - vel2) / (_delay * 0.01f);

					DrawQuad(p + vel * (_delay * 0.01f) + 0.5f * ac * (_delay * 0.01f) * (_delay * 0.01f), Color.yellow);
					DrawQuad(p + vel * (_delay * 0.01f), Color.green);
					Vector2 dir = new Vector2(Mathf.Cos(package.Rotation * Mathf.Deg2Rad), Mathf.Sin(package.Rotation * Mathf.Deg2Rad));

					Vector2 center = package.Position;
					Debug.DrawLine(center, center + dir, Color.green, 0.2f);
					return;
				}

				_pendingPackages.Enqueue(package);
			}
		}

		private void DrawQuad(in Vector2 pos, in Color c)
		{
			Vector2 center = pos;
			Vector2 up = center + Vector2.up;
			Vector2 down = center + Vector2.down;
			Vector2 left = center + Vector2.left;
			Vector2 right = center + Vector2.right;

			Debug.DrawLine(up, down, c, 0.2f);
			Debug.DrawLine(left, right, c, 0.2f);
			Debug.DrawLine(up, right, c, 0.2f);
			Debug.DrawLine(up, left, c, 0.2f);
			Debug.DrawLine(down, right, c, 0.2f);
			Debug.DrawLine(down, left, c, 0.2f);

		}
	}
}
