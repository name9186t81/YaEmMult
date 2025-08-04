using Core;

using UnityEngine;

namespace Networking
{
	public sealed class ActorSyncer : MonoBehaviour
	{
		[SerializeField, Range(10, 120)] private int _rate;
		private float _elapsed;
		private uint _tick;

		private void Update()
		{
			if(!ServiceLocator.TryGet<ListenersCombiner>(out var combiner) || combiner.Client == null || combiner.Client.OwnID == 255)
			{
				return;
			}

			_elapsed += Time.deltaTime;
			while (_elapsed > 1f / _rate)
			{
				combiner.Client.SendPackage(new ActorSyncPackage(transform.position, transform.rotation.eulerAngles.z, _tick), combiner.Client.ServerEndPoint);
				_elapsed -= 1f / _rate;
				_tick++;
			}
		}
	}
}
