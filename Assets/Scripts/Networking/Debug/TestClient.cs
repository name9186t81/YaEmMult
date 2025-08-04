using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
	public class TestClient
	{
		private UdpClient _client;
		private byte[] _package;
		private int _port;
		private CancellationTokenSource _cts;
		private Stopwatch _watch;
		private long _prevTime = UnityEngine.Random.Range(0, 6);

		public TestClient(int packageSize, int serverPort)
		{
			_client = new UdpClient();
			_port = serverPort;

			_package = new byte[packageSize];
			for (int i = 0; i < packageSize; i++) 
			{
				_package[i] = (byte)UnityEngine.Random.Range(0, 255);
			}

			_watch = Stopwatch.StartNew();
			_cts = new CancellationTokenSource();
			Task.Run(SendPackage);
		}

		private async Task SendPackage()
		{
			while (!_cts.IsCancellationRequested)
			{
				if (_watch.ElapsedMilliseconds - _prevTime > 5)
				{
					_prevTime = _watch.ElapsedMilliseconds;

					await _client.SendAsync(_package, _package.Length, new IPEndPoint(IPAddress.Loopback, _port));
				}
			}
		}

		public void Kill()
		{
			_cts.Cancel();
		}
	}
}
