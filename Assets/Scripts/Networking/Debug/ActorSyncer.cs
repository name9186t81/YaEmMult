using Core;

using UnityEngine;

namespace Networking
{
	public sealed class ActorSyncer : MonoBehaviour
	{
		[SerializeField, Range(10, 120)] private int _rate;
		[SerializeField] private long _serverListening;
		[SerializeField] private long _serverProcessed;
		[SerializeField] private long _clientListening;
		[SerializeField] private long _clientProcessed;
		private float _elapsed;
		private uint _tick;

		private void Update()
		{
			if(!ServiceLocator.TryGet<ListenersCombiner>(out var combiner) || combiner.Client == null || combiner.Client.ID == 255)
			{
				return;
			}

			_clientListening = combiner.Client.ReceivedPackages;
			_clientProcessed = combiner.Client.ProcessedPackages;

			if(combiner.Server != null)
			{
				_serverListening = combiner.Server.ReceivedPackages;
				_serverProcessed = combiner.Server.ProcessedPackages;
			}
			_elapsed += Time.deltaTime;
			while (_elapsed > 1f / _rate)
			{
				combiner.Client.SendPackage(new ActorSyncPackage(transform.position, transform.rotation.eulerAngles.z, _tick));
				_elapsed -= 1f / _rate;
				_tick++;
			}
		}
	}
}
