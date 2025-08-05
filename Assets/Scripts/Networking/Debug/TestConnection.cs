using UnityEngine;

namespace Networking
{
	public class TestConnection : MonoBehaviour
	{
		[SerializeField] private int _clientsCount;

		[SerializeField] private long _received;
		[SerializeField] private long _processed;
		[SerializeField] private long _difference;
		private ListenerBase _server;
		private TestClient[] _clients;

		private void Awake()
		{
			_clients = new TestClient[_clientsCount];

			_server = new ListenerBase(8808);
			for (int i = 0; i < _clientsCount; i++)
			{
				_clients[i] = new TestClient(28, 8808);
			}
		}

		private void Update()
		{
			_received = _server.ReceivedPackages;
			_processed = _server.ProcessedPackages;
			_difference = _received - _processed;
		}

		private void OnDisable()
		{
			_server.Dispose();
			for(int i = 0; i < _clientsCount; i++)
			{
				_clients[i].Kill();
			}
		}
	}
}
