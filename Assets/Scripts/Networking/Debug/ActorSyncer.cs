using Core;

using UnityEngine;

namespace Networking
{
	public sealed class ActorSyncer : MonoBehaviour
	{
		[SerializeField, Range(10, 120)] private int _rate;
		[SerializeField] private long _serverListening;
		[SerializeField] private long _serverProcessed;
		[SerializeField] private long _serverTicks;
		[SerializeField] private long _clientListening;
		[SerializeField] private long _clientProcessed;
		[SerializeField] private long _clientTicks;
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
			_clientTicks = combiner.Client.Ticks;

			if(combiner.Server != null)
			{
				_serverListening = combiner.Server.ReceivedPackages;
				_serverProcessed = combiner.Server.ProcessedPackages;
				_serverTicks = combiner.Server.Ticks;
			}
			_elapsed += Time.deltaTime;
			while (_elapsed > 1f / _rate)
			{
				combiner.Client.SendPackage(new ActorSyncPackage(transform.position, transform.rotation.eulerAngles.z, _tick));
				_elapsed -= 1f / _rate;
				_tick++;
			}
		}

		private void OnDisable()
		{
			if (ServiceLocator.TryGet<ListenersCombiner>(out var comb))
			{
				comb.Client?.Dispose();
				comb.Server?.Dispose();

				comb.Client = null;
				comb.Server = null;
			}
		}
	}
}
