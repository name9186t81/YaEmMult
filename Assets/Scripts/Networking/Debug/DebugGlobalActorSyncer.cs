using Core;

using System.Collections.Generic;

using UnityEngine;

namespace Networking
{
	public class DebugGlobalActorSyncer : MonoBehaviour
	{
		[SerializeField] private GameObject _movePrefab;
		private Dictionary<int, GameObject> _idToGameObject;

		private static DebugGlobalActorSyncer _instance;
		public static DebugGlobalActorSyncer Instance { get { return _instance; } }

		private void Awake()
		{
			_instance = this;
			_idToGameObject = new Dictionary<int, GameObject>();
		}

		public void ReceivePackage(ActorSyncFromServerPackage package)
		{
			if (ServiceLocator.TryGet<ListenersCombiner>(out var combiners) && combiners.Client != null)
			{
				int selfID = combiners.Client.OwnID;

				if (package.ID == selfID) return;

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
	}
}
