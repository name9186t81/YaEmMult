using Core;

using UnityEngine;

namespace Networking
{
	public class LoadTest : MonoBehaviour
	{
		[SerializeField, Range(10, 120)] private int _rate;
		[SerializeField] private int _bytes;
		[SerializeField] private bool _client;
		[SerializeField] private long _serverListening;
		[SerializeField] private long _serverProcessed;
		[SerializeField] private long _clientListening;
		[SerializeField] private long _clientProcessed;
		private byte[] _data;
		private float _elapsed;

		private void Awake()
		{
			_data = new byte[_bytes];
			for(int i = 0; i < _bytes; i++)
			{
				_data[i] = (byte)Random.Range(0, 255);
			}
		}

		private void Update()
		{
			ServiceLocator.TryGet<ListenersCombiner>(out var combiner);
			if (combiner.Client != null)
			{
				_clientListening = combiner.Client.ReceivedPackages;
				_clientProcessed = combiner.Client.ProcessedPackages;
			}
			if (combiner.Server != null)
			{
				_serverListening = combiner.Server.ReceivedPackages;
				_serverProcessed = combiner.Server.ProcessedPackages;
			}

			if (combiner.Client == null || combiner.Client.ID == 255)
			{
				return;
			}

			_elapsed += Time.deltaTime;
			while (_elapsed > 1f / _rate)
			{
				if (_client)
					combiner.Server.SendPackage(_data, ListenerBase.PackageSendOrder.Instant, ListenerBase.PackageSendDestination.Everyone);
				else
					combiner.Client.SendPackage(_data, ListenerBase.PackageSendOrder.Instant, ListenerBase.PackageSendDestination.Everyone);
				_elapsed -= 1f / _rate;
			}
		}

		private void OnDisable()
		{
			if(ServiceLocator.TryGet<ListenersCombiner>(out var comb))
			{
				comb.Client?.Dispose();
				comb.Server?.Dispose();

				comb.Client = null;
				comb.Server = null;
			}
		}
	}
}
