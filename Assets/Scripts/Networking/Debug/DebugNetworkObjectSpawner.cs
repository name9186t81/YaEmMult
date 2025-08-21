using Networking;

using UnityEngine;

namespace MyDebug
{
	public sealed class DebugNetworkObjectSpawner : MonoBehaviour
	{
		[SerializeField] private NetworkMonoBehaviour _prefab;
		[SerializeField] private bool _spawn;

		private void Update()
		{
			if (!_spawn) return;

			_spawn = false;
			NetworkManager.Instance.TryToSpawn(_prefab, transform.position, transform.rotation.eulerAngles.z, out _);
		}
	}
}
