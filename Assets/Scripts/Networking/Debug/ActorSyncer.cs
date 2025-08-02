using Core;

using UnityEngine;

namespace Networking
{
	public sealed class ActorSyncer : MonoBehaviour
	{
		[SerializeField, Range(10, 20)] private int _rate;
		private float _elapsed;

		private void Update()
		{
			if(!ServiceLocator.TryGet<ListenersCombiner>(out var combiner) || combiner.Client == null || combiner.Client.OwnID == 255)
			{
				return;
			}

			_elapsed += Time.deltaTime;
			while (_elapsed > 1f / _rate)
			{
				combiner.Client.SendPackage(new ActorSyncPackage(transform.position, transform.rotation.eulerAngles.z), combiner.Client.ServerEndPoint);
				_elapsed -= 1f / _rate;
			}
		}
	}
}
