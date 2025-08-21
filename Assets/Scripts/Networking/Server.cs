using Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using Debug = UnityEngine.Debug;

namespace Networking
{
	public sealed class Server : IService, IDisposable
	{
		private readonly int _listeningPort;
		private readonly int _listeningPortTCP;
		private readonly IPAddress _listeningAdress;
		private readonly UdpClient _serverUDP;
		private readonly TcpListener _serverTCP;
		private readonly Stopwatch _serverRuntime;
		private readonly IReadOnlyDictionary<PackageType, IPackageHandler> _customHandlers;
		private readonly LinkedList<IPEndPoint> _connectedClients;
		private readonly HashSet<IPEndPoint> _connectedClientsHash;
		private readonly Task _task;
		private readonly Task _taskTCP;

		private int _networkObjectID = 256;

		private bool _running = true;
		private bool _disposed;

		private Server(int listeningPort, IPAddress listeningAdress)
		{
			_connectedClients = new LinkedList<IPEndPoint>();
			_connectedClientsHash = new HashSet<IPEndPoint>();
			_listeningPort = listeningPort;
			_listeningPortTCP = _listeningPort + 1;
			_listeningAdress = listeningAdress;

			try
			{
				_serverUDP = new UdpClient(_listeningPort);
				_serverTCP = new TcpListener(IPAddress.Any, _listeningPortTCP);

				_serverRuntime = new Stopwatch();
				_serverRuntime.Start();

				_task = Task.Run(ReceiveMessages);

				_serverTCP.Start();
				_taskTCP = Task.Run(ReceiveMessagesTCP);
			}
			catch (Exception ex)
			{
				Debug.LogError($"SERVER: CRITICAL ERROR CANNOT START SERVER EX: {ex.Message}");
				_running = false;
			}
			finally
			{
				Debug.Log($"SERVER: Running UDP server on {_serverUDP.Client.LocalEndPoint.ToString()}");
				Debug.Log($"SERVER: Running TCP server on {_serverTCP.LocalEndpoint.ToString()}");
			}

			Dictionary<PackageType, IPackageHandler> handlers = new Dictionary<PackageType, IPackageHandler>();

			_customHandlers = handlers;
		}

		public event Action<Type> RemoveCallback;

		public static Server Create(IPAddress adress, int port)
		{
			var newServer = new Server(port, adress);
			if (ServiceLocator.TryGet<Server>(out var server))
			{
				server.Kill();
			}

			ServiceLocator.Register(newServer);
			return newServer;
		}

		private void Kill()
		{
			RemoveCallback?.Invoke(typeof(Server));
			_serverUDP.Close();
			_serverTCP.Stop(); //todo: send message about server closure.
			_serverRuntime.Stop();
			_task.Dispose();
			_taskTCP.Dispose();
		}

		private async Task ReceiveMessagesTCP()
		{
			while (true)
			{
				using TcpClient tcpClient = await _serverTCP.AcceptTcpClientAsync();
				Debug.Log($"SERVER: Incoming TCP connection from {tcpClient.Client.RemoteEndPoint}");
			}
		}

		private async Task ReceiveMessages()
		{
			while (true)
			{
				var result = await _serverUDP.ReceiveAsync();

				var buffer = result.Buffer;
				var endPoint = result.RemoteEndPoint;

				var type = NetworkUtils.GetPackageType(ref buffer);
				long ms = _serverRuntime.ElapsedMilliseconds;

				switch (type)
				{
					case PackageType.Invalid:
					{
						Debug.LogError($"SERVER: Received invalid package type from {endPoint.ToString()} at {ms}...");
						break;
					}
					case PackageType.Ping:
					{
						Debug.Log($"SERVER: Received ping from {endPoint.ToString()} at {ms}");

						PingResponsePackage responsePackage = new PingResponsePackage();
						byte[] package = new byte[responsePackage.GetRealPackageSize()];
						responsePackage.Serialize(ref package, NetworkUtils.PackageHeaderSize);
						NetworkUtils.PackageTypeToByteArray(PackageType.PingResponse, ref package);
						await _serverUDP.SendAsync(package, package.Length, endPoint);
						break;
					}
					case PackageType.ConnectionRequest:
					{
						Debug.Log($"SERVER: Received connection request from {endPoint.Address.ToString()} at {ms}");
						byte[] response = new byte[NetworkUtils.PackageHeaderSize + 1 + sizeof(uint)];
						NetworkUtils.PackageTypeToByteArray(PackageType.ConnectionResponse, ref response);

						bool alreadyConnected = _connectedClientsHash.Contains(endPoint);

						response[NetworkUtils.PackageHeaderSize] = (byte)ConnectionResponseType.Success;
						int crc = NetworkUtils.Adler32(ref response, 0, NetworkUtils.PackageHeaderSize + 1);
						var array = BitConverter.GetBytes(crc);
						Array.Copy(array, 0, response, NetworkUtils.PackageHeaderSize + 1, sizeof(uint));

						_connectedClientsHash.Add(endPoint);
						_connectedClients.AddLast(endPoint);

						await _serverUDP.SendAsync(response, response.Length, result.RemoteEndPoint);
						break;
					}
					case PackageType.TimeSync:
					{
						Debug.Log($"SERVER: Received time sync request from {endPoint.Address.ToString()} at {ms}");

						long clientTime1;
						long clientTime2;
						byte[] clientTimeArray = new byte[sizeof(long)];
						Array.Copy(buffer, NetworkUtils.PackageHeaderSize, clientTimeArray, 0, sizeof(long));
						clientTime1 = BitConverter.ToInt64(clientTimeArray, 0);
						Array.Copy(buffer, NetworkUtils.PackageHeaderSize + sizeof(long), clientTimeArray, 0, sizeof(long));
						clientTime2 = BitConverter.ToInt64(clientTimeArray, 0);

						if(Math.Abs(clientTime1 - clientTime2)  > 5)
						{
							Debug.LogError($"SERVER: corrupted time sync received from client  {endPoint.Address.ToString()}");
						}
						else
						{
							if(Math.Abs(clientTime1 - ms) < 10)
							{
								Debug.Log($"SERVER: time sync finished with {endPoint.Address.ToString()}");
							}
						}

						byte[] package = new byte[NetworkUtils.PackageHeaderSize + sizeof(long) * 2]; //2 longs to ensure correct data is received
						NetworkUtils.PackageTypeToByteArray(PackageType.TimeSync, ref package);
						byte[] longBytes = BitConverter.GetBytes(ms);
						Array.Copy(longBytes, 0, package, NetworkUtils.PackageHeaderSize, sizeof(long));
						Array.Copy(longBytes, 0, package, NetworkUtils.PackageHeaderSize + sizeof(long), sizeof(long));

						await _serverUDP.SendAsync(package, package.Length);
						break;
					}
					case PackageType.ClientShutdown:
					{
						Debug.Log($"SERVER: client[{endPoint.ToString()}] disconnected...");
						await HandleDisconnect(endPoint, false);
						break;
					}
					default:
					{
						if (_customHandlers.ContainsKey(type))
						{

						}
						else
						{
							Debug.LogWarning($"SERVER: cannot proccess package type: {type.ToString()}");
						}
						break;
					}
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if(_disposed) return;

			if (disposing)
			{
			}

			if (_running)
			{
				var bytes = new byte[NetworkUtils.PackageHeaderSize];
				NetworkUtils.PackageTypeToByteArray(PackageType.ServerShutdown, ref bytes);

				foreach (var client in _connectedClients)
				{
					_serverUDP.Send(bytes, bytes.Length, client);
					Debug.Log($"SERVER: Farewell {client.ToString()} client!");
				}
			}

			_serverUDP.Dispose();
			RemoveCallback?.Invoke(typeof(Server));
			_running = false;
		}

		~Server()
		{
			Dispose(false);
		}

		private bool IsClientConnected(IPEndPoint ip) => _connectedClientsHash.Contains(ip);

		private async Task HandleDisconnect(IPEndPoint ip, bool forced)
		{
			_connectedClientsHash.Remove(ip);
			_connectedClients.Remove(ip);
		}

		public int NetworkObjectID => _networkObjectID++;
		public bool Running => _running;
	}
}
