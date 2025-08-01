using Core;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using UnityEditor.PackageManager.UI;

using static UnityEngine.Rendering.ReloadAttribute;

using Debug = UnityEngine.Debug;

namespace Networking
{
	public class Listener : IService, IDisposable
	{
		protected readonly struct ACKInfo
		{
			public readonly int ID;
			public readonly long StartTime;

			public ACKInfo(int iD, long startTime)
			{
				ID = iD;
				StartTime = startTime;
			}
		}

		protected struct ACKData
		{
			public readonly byte[] Data;
			public readonly IPEndPoint EndPoint;
			public int Tries;

			public ACKData(byte[] data, IPEndPoint endPoint, int tries)
			{
				Data = data;
				EndPoint = endPoint;
				Tries = tries;
			}
		}


		private UdpClient _client;
		private IPAddress _serverAdress;
		private Stopwatch _clientTime;
		private int _serverPort;
		private int _selfPort;
		private IPAddress _selfAdress;
		private IPEndPoint _selfEndPoint;
		private bool _connected;
		private bool _disposed;
		private Task _listenTask;
		private Task _processTask;
		private Task _pingTask;
		private readonly bool _isClient;

		private readonly ConcurrentQueue<UdpReceiveResult> _receivedPackagesQueue;
		private readonly CancellationTokenSource _cancellation;
		private readonly Dictionary<int, byte[]> _cachedArrays;
		private readonly List<ACKInfo> _awaitingACKs;
		private readonly List<int> _alreadySentACKs;
		private readonly ConcurrentDictionary<int, long> _sentACKsToTime;
		private readonly ConcurrentDictionary<int, ACKInfo> _idToACK;
		private readonly ConcurrentDictionary<int, ACKData> _storedPackages;
		private readonly List<IPEndPoint> _connectedUsers;
		private readonly static ConcurrentDictionary<PackageType, PackageFlags> _typeToFlags;
		private static readonly ConcurrentDictionary<PackageType, IPackageProcessor> _processors;
		private const int IDLE_PROCESSING_DELAY = 10;
		private const long MAX_ACK_WAITING_TIME = 2000;
		private const int MAX_PACKAGE_SEND_TRIES = 5;

		private int _packageID;

		private Listener(bool isClient, int port)
		{
			_isClient = isClient;
			if (isClient)
			{
				_client = new UdpClient();
			}
			else
			{
				_client = new UdpClient(port);
			}
			_clientTime = new Stopwatch();
			_receivedPackagesQueue = new ConcurrentQueue<UdpReceiveResult>();
			_cancellation = new CancellationTokenSource();
			_cachedArrays = new Dictionary<int, byte[]>();
			_awaitingACKs = new List<ACKInfo>();
			_storedPackages = new ConcurrentDictionary<int, ACKData>();
			_idToACK = new ConcurrentDictionary<int, ACKInfo>();
			_connectedUsers = new List<IPEndPoint>();

			_alreadySentACKs = new List<int>();
			_sentACKsToTime = new ConcurrentDictionary<int, long>();

			_clientTime.Start();

			if (!isClient)
			{
				_listenTask = Task.Run(() => Listen(_cancellation.Token));
				_processTask = Task.Run(() => ProcessPackages(_cancellation.Token));
			}

			_packageID = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
			_selfEndPoint = (IPEndPoint)_client.Client.LocalEndPoint;

			Debug.Log($"Listener" + (_isClient ? "Client" : "Server") + ": starting client at " + _client.Client.LocalEndPoint);
		}

		static Listener()
		{
			int size = Enum.GetValues(typeof(PackageType)).Length;
			ConcurrentDictionary<PackageType, IPackageProcessor> processors = new ConcurrentDictionary<PackageType, IPackageProcessor>();
			_typeToFlags = new ConcurrentDictionary<PackageType, PackageFlags>();

			var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(IPackageProcessor).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
			var packages = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(IPackage).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

			foreach (var type in types)
			{
				var prod = Activator.CreateInstance(type);
				if (prod != null && prod is IPackageProcessor proc)
				{
					var attribute = type.GetCustomAttribute<ProcessorAttribute>();
					if (attribute != null)
					{
						if (processors.TryGetValue(attribute.ProcessedType, out var processor))
						{
							Debug.LogWarning($"LISTENER WARNING: PROCESSOR OF A TYPE {attribute.ProcessedType} ALREADY EXISTS: {processor.GetType().Name}");
							processors[attribute.ProcessedType] = processor;

							var packageType = packages.First(t => (t.GetCustomAttribute<PackageAttribute>()).PackageType == attribute.ProcessedType);
							if (packageType != null)
							{
								_typeToFlags.AddOrUpdate(attribute.ProcessedType, packageType.GetCustomAttribute<PackageAttribute>().Flags, (PackageType tp, PackageFlags fl) => _typeToFlags[tp] = fl);
							}
							else
							{
								Debug.LogError($"LISTENER ERROR: CANNOT CREATE INSTANCE OF {type} TYPE: NO MATCHING PACKAGE TYPE");
							}
						}
						else
						{
							Debug.Log(type.FullName);
							var packageType = packages.First(t => (t.GetCustomAttribute<PackageAttribute>()).PackageType == attribute.ProcessedType);
							if (packageType != null)
							{
								_typeToFlags.AddOrUpdate(attribute.ProcessedType, packageType.GetCustomAttribute<PackageAttribute>().Flags, (PackageType tp, PackageFlags fl) => _typeToFlags[tp] = fl);
							}
							else
							{
								Debug.LogError($"LISTENER ERROR: CANNOT CREATE INSTANCE OF {type} TYPE: NO MATCHING PACKAGE TYPE");
							}
							processors.AddOrUpdate(attribute.ProcessedType, proc, (PackageType tp, IPackageProcessor ip) => processors[tp] = ip);
						}
					}
					else
					{
						Debug.LogError($"LISTENER ERROR: CANNOT CREATE INSTANCE OF {type} TYPE: NO ATTRIBUTE ASSIGNED");
					}
				}
				else
				{
					Debug.LogError($"LISTENER ERROR: CANNOT CREATE INSTANCE OF {type} TYPE: ACTIVATOR ERROR");
				}
			}

			_processors = processors;
		}

		private async Task Listen(CancellationToken cancellationToken)
		{
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					var result = await _client.ReceiveAsync();
					_receivedPackagesQueue.Enqueue(result);

					Debug.Log($"Listener" + (_isClient ? "Client" : "Server") + $": received package {NetworkUtils.GetPackageType(result.Buffer)} size {result.Buffer.Length} bytes");
				}
			}
			catch(SocketException ex)
			{
				Debug.LogError($"Listener" + (_isClient ? "Client" : "Server") + ": CRITICAL ERROR WHILE LISTENING: " + ex.Message);
			}
			catch(ObjectDisposedException ex2)
			{
				Debug.LogWarning($"Listener" + (_isClient ? "Client" : "Server") + ": SOCKET CLOSED");
			}
		}

		private async Task ProcessPackages(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if(_receivedPackagesQueue.TryDequeue(out var result))
				{
					var buffer = result.Buffer;
					var type = NetworkUtils.GetPackageType(buffer);
					Debug.Log($"Listener" + (_isClient ? "Client" : "Server") + ": processing package - " + type);

					if(type == PackageType.Ack)
					{
						int size = buffer.Length;
						ACKPackage sample = new ACKPackage();

						if(size != sample.GetRealPackageSize()) 
						{
							Debug.LogError($"Listener" + (_isClient ? "Client" : "Server") + ": received wrong ACK package size");
							continue;
						}

						sample.Deserialize(ref buffer, NetworkUtils.PackageHeaderSize);

						if(_idToACK.TryGetValue(sample.ID, out var ack))
						{
							Debug.Log(ListenerName + $": ack{sample.ID} received");
							_awaitingACKs.Remove(ack);
							_storedPackages.TryRemove(ack.ID, out _);
							_idToACK.TryRemove(sample.ID, out _);
						}
						else
						{
							Debug.LogWarning($"Listener" + (_isClient ? "Client" : "Server") + $": received ACK with unknown ID({sample.ID})");
						}

						continue;
					}

					if (_processors.TryGetValue(type, out var processor))
					{
						Debug.Log(processor.GetType().Name);
						var flags = _typeToFlags[type];
						bool needACK = (flags & PackageFlags.NeedACK) != 0;

						if(needACK)
						{
							int id = GetACKID(buffer);
							if (_alreadySentACKs.Contains(id))
							{
								await SendACK(ACKPackage.ACKResult.Success, buffer, result.RemoteEndPoint);
								_sentACKsToTime[id] = Time;
								continue;
							}
						}

						if (GetRawDataInternal(buffer, flags, out var data, out _))
						{
							var res = await processor.Process(data, _cancellation, result.RemoteEndPoint, this);

							if (res)
							{
								if (needACK)
								{
									int id = await SendACK(ACKPackage.ACKResult.Success, buffer, result.RemoteEndPoint);
									_alreadySentACKs.Add(id);
									_sentACKsToTime.AddOrUpdate(id, Time, (newID, newTime) => _sentACKsToTime[newID] = newTime);
								}
							}
							else
							{
								Debug.LogWarning("Listener" + (_isClient ? "Client" : "Server") + ": failed to process package");

								if (needACK)
								{
									await SendACK(ACKPackage.ACKResult.Failed, buffer, result.RemoteEndPoint);
								}
							}
						}
						else
						{
							Debug.Log("Listener" + (_isClient ? "Client" : "Server") + ": package was corrupted");

							if (needACK)
							{
								await SendACK(ACKPackage.ACKResult.FailedCorrupted, buffer, result.RemoteEndPoint);
							}
						}
					}
					else
					{
						Debug.LogError("LISTENER: CANNOT PROCESS PACKAGE WITH A TYPE " + type);
					}
				}
				else
				{
					try
					{
						await ProcessAwaitingACKs();
					}
					catch (Exception ex)
					{
						Debug.LogError(ListenerName + ": ERROR WHILE PROCESSING ACKS - " + ex.Message);
					}
					await Task.Delay(IDLE_PROCESSING_DELAY);
				}
			}
		}

		private async Task<int> SendACK(ACKPackage.ACKResult result, byte[] buffer, IPEndPoint sender)
		{
			int id = GetACKID(buffer);

			var package = new ACKPackage(result, id);
			await SendPackage(package, sender);
			return id;
		}

		private int GetACKID(byte[] buffer)
		{
			return BitConverter.ToInt32(buffer, buffer.Length - sizeof(int));
		}

		private async Task ProcessAwaitingACKs()
		{
			for(int i = 0; i < _awaitingACKs.Count; i++)
			{
				//Debug.Log(ListenerName + "Processing ACKs" + _awaitingACKs.Count);
				//Debug.Log("processing " + _awaitingACKs[i].ID);
				var ACK = _awaitingACKs[i];
				long difference = Time - ACK.StartTime;

				if(difference > MAX_ACK_WAITING_TIME)
				{
					if(!_storedPackages.TryGetValue(ACK.ID, out var info))
					{
						Debug.LogError(ListenerName + $": ACKS list contains unknown id... removing");
						_storedPackages.TryRemove(ACK.ID, out _);
						_idToACK.TryRemove(ACK.ID, out _);
						_awaitingACKs.Remove(ACK);
						i = Math.Max(i - 1, 0);

						continue;
					}
					int tries = info.Tries;

					if(tries == MAX_PACKAGE_SEND_TRIES)
					{
						Debug.LogError($"Listener" + (_isClient ? "Client" : "Server") + ": failed to received ACK after 5 tries");

						_storedPackages.TryRemove(ACK.ID, out _);
						_idToACK.TryRemove(ACK.ID, out _);
						_awaitingACKs.Remove(ACK);

						i = Math.Max(i - 1, 0);
						continue;
					}

					_storedPackages[ACK.ID] = new(info.Data, info.EndPoint, info.Tries++);
					_awaitingACKs[i] = new(ACK.ID, Time);
					Debug.LogWarning(ListenerName + ": sending package again");
					await SendAsync(info.Data, info.EndPoint);
				}
			}

			for (int i = 0; i < _alreadySentACKs.Count; i++)
			{
				int id = _alreadySentACKs[i];

				long difference = Time - _sentACKsToTime[id];

				if(difference > MAX_ACK_WAITING_TIME * MAX_PACKAGE_SEND_TRIES)
				{
					_alreadySentACKs.Remove(id);
					_sentACKsToTime.TryRemove(id, out _);

					i = Math.Max(i - 1, 0);
				}
			}
		}

		public async Task<bool> TryConnect(IPAddress adress, int port)
		{
			//if (adress == IPAddress.Loopback) return true; //always connect to loopback

			byte[] buffer = new byte[NetworkUtils.PackageHeaderSize];
			NetworkUtils.PackageTypeToByteArray(PackageType.ConnectionRequest, ref buffer);
			Task[] receiveTimeout = new Task[2];

			Debug.Log($"CLIENT: Attempting to connect to server {adress.ToString()}:{port}");
			for (int i = 0; i < 5; i++)
			{
				await _client.SendAsync(buffer, buffer.Length, new IPEndPoint(adress, port));
				var receiveTask = _client.ReceiveAsync();
				receiveTimeout[0] = Task.Delay(NetworkUtils.TIMEOUT_DELAY);
				receiveTimeout[1] = receiveTask;

				var result = await Task.WhenAny(receiveTimeout);
				if(result == receiveTimeout[0])
				{
					Debug.LogError($"CLIENT: Connection timeout occured");
					continue;
				}
				var response = receiveTask.Result.Buffer;

				/*
				int checkSum = NetworkUtils.Adler32(ref response, 0, NetworkUtils.PackageSize + 1);
				int receivedCheckSum = BitConverter.ToInt32(response, NetworkUtils.PackageSize + 1);

				if (checkSum != receivedCheckSum)
				{
					Debug.LogError("CLIENT: Received package is corrupted attempting connection again...");
					continue;
				}
				*/

				if (NetworkUtils.GetPackageType(ref response) != PackageType.ConnectionResponse)
				{
					Debug.Log("CLIENT: Connection failed: Received wrong package type from server");
					return false;
				}

				var responseType = (ConnectionResponseType)response[NetworkUtils.PackageHeaderSize];
				switch (responseType)
				{
					case ConnectionResponseType.Success:
					{
						Debug.Log($"CLIENT: Connection established!");
						_serverAdress = adress;
						_serverPort = port;
						_connected = true;

						PingPackage pingPackage = new PingPackage();
						var bytes = new byte[pingPackage.GetRealPackageSize()];
						pingPackage.Serialize(ref bytes, NetworkUtils.PackageHeaderSize);

						_listenTask = Task.Run(() => Listen(_cancellation.Token));
						_processTask = Task.Run(() => ProcessPackages(_cancellation.Token));

						NetworkUtils.PackageTypeToByteArray(PackageType.Ping, ref bytes);
						//await _client.SendAsync(bytes, bytes.Length, new IPEndPoint(_serverAdress, _serverPort));

						return true;
					}
					case ConnectionResponseType.Dropped: { Debug.Log($"CLIENT: Connection from {adress}:{port} dropped by server!"); return false; }
					case ConnectionResponseType.Forbidden: { Debug.Log($"CLIENT: Connection to {adress}:{port} is forbidden!"); return false; }
				}
			}

			Debug.LogError($"CLIENT: Failed to connect after 5 attempts");
			return false;
		}

		public async Task Disconnect(bool forced)
		{
			if (!_connected) return;

			if (!forced)
			{
				var bytes = FormDisconnectMessage();
				NetworkUtils.PackageTypeToByteArray(PackageType.ClientShutdown, ref bytes);
				Debug.Log($"CLIENT: Sending last message to {_serverAdress.ToString()}:{_serverPort} server...");
				await _client.SendAsync(bytes, bytes.Length, new IPEndPoint(_serverAdress, _serverPort));
			}

			_cancellation.Cancel();
			_serverAdress = default;
			_serverPort = default;
			_connected = false;

			_listenTask.Dispose();
			_pingTask.Dispose();
		}

		/*
		private async Task Listen()
		{
			Task[] tasks = new Task[2];
			int pingTries = 0;

			while (_connected)
			{
				var result = await _client.ReceiveAsync();

				var buffer = result.Buffer;
				var endPoint = result.RemoteEndPoint;

				var type = NetworkUtils.GetPackageType(ref buffer);

				switch (type)
				{
					case PackageType.ServerShutdown:
					{
						Debug.Log("CLIENT: Remote server shutting down...");
						await Disconnect(true);
						break;
					}
					case PackageType.PingResponse:
					{
						

						break;
					}
				}

				await Task.Delay(200); //check ping only every 200ms or so
				var bytes = new byte[NetworkUtils.PackageSize];
				NetworkUtils.PackageTypeToByteArray(PackageType.Ping, ref bytes);
				await _client.SendAsync(bytes, bytes.Length, new IPEndPoint(_serverAdress, _serverPort));
				long time = _clientTime.ElapsedMilliseconds;

				var receiveTask = _client.ReceiveAsync();
				var delayTask = Task.Delay(NetworkUtils.TIMEOUT_DELAY);
				tasks[0] = receiveTask;
				tasks[1] = delayTask;

				var taskRes = await Task.WhenAny(tasks);
				if (taskRes == delayTask)
				{
					Debug.LogWarning("CLIENT: No ping response from a server trying again...");
					if (pingTries++ > 5)
					{
						Debug.LogError("CLIENT: No server response after 5 tries");
						await Disconnect(false);
						return;
					}
					continue;
				}

				pingTries = 0;
				long receiveTime = _clientTime.ElapsedMilliseconds;
				long diff = (receiveTime - time) / 2;
				Debug.Log($"CLIENT: ping: {diff}ms");
			}
		}
		*/

		public static void Create(int port, bool isClient)
		{
			if (ServiceLocator.TryGet<ListenersCombiner>(out var combiner))
			{
				if (isClient)
				{
					if(combiner.Client != null)
					{
						combiner.Client._client = new UdpClient();
					}
					else
					{
						combiner.Client = new Listener(isClient, port);
					}
				}
				else
				{
					if (combiner.Server != null)
					{
						combiner.Server._client = new UdpClient(port);
					}
					else
					{
						combiner.Server = new Listener(isClient, port);
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

			if (_connected)
			{
				var bytes = FormDisconnectMessage();
				NetworkUtils.PackageTypeToByteArray(PackageType.ClientShutdown, ref bytes);
				Debug.Log($"CLIENT: Farewell {_serverAdress.ToString()}:{_serverPort} server!");
				_client.Send(bytes, bytes.Length, new IPEndPoint(_serverAdress, _serverPort));
			}

			_client.Dispose();
			RemoveCallback?.Invoke(typeof(Listener));
			_pingTask.Dispose();
			_listenTask.Dispose();
			_disposed = true;
		}

		private byte[] FormDisconnectMessage()
		{
			var bytes = new byte[NetworkUtils.PackageHeaderSize];
			NetworkUtils.PackageTypeToByteArray(PackageType.ClientShutdown, ref bytes);
			return bytes;
		}

		~Listener()
		{
			Dispose(false);
		}

		public void GetCachedBuffer(IPackage package, out byte[] buffer)
		{
			int size = package.GetRealPackageSize();
			if ((package.Flags & PackageFlags.Reliable) != 0 || (package.Flags & PackageFlags.VeryReliable) != 0) size += sizeof(int);

			if (_cachedArrays.TryGetValue(size, out buffer)) return;

			buffer = new byte[size];
			_cachedArrays[size] = buffer;
			return;
		}

		private bool GetRawDataInternal(byte[] data, PackageFlags flags, out byte[] newData, out int end)
		{
			byte[] result = data;
			if ((flags & PackageFlags.Compress) != 0 || (flags & PackageFlags.CompressHigh) != 0)
			{
				using (MemoryStream compressed = new MemoryStream(data))
				using (DeflateStream deflate = new DeflateStream(compressed, CompressionMode.Decompress))
				using (MemoryStream decompressed = new MemoryStream())
				{
					deflate.CopyTo(decompressed);
					newData = decompressed.ToArray();
				}
			}
			else
			{
				newData = data;
			}

			end = data.Length;
			if ((flags & PackageFlags.Reliable) != 0)
			{
				int check = NetworkUtils.Adler32(ref data, 0, data.Length - sizeof(int) - ((flags & PackageFlags.NeedACK) != 0 ? sizeof(int) : 0));
				return check == BitConverter.ToInt32(data, data.Length - sizeof(int) - ((flags & PackageFlags.NeedACK) != 0 ? sizeof(int) : 0));
			}
			else if ((flags & PackageFlags.VeryReliable) != 0)
			{
				uint check = NetworkUtils.CRC32(ref data, 0, data.Length - sizeof(int) - ((flags & PackageFlags.NeedACK) != 0 ? sizeof(int) : 0));
				return check == BitConverter.ToUInt32(data, data.Length - sizeof(int) - ((flags & PackageFlags.NeedACK) != 0 ? sizeof(int) : 0));
			}
			Debug.Log(data.Length + " " + newData.Length);
			return true;
		}

		public bool GetRawData(byte[] data, IPackage package, out byte[] newData, out int end) => GetRawDataInternal(data, package.Flags, out newData, out end);

		public void SerializePackage(IPackage package, out byte[] serialized)
		{
			GetCachedBuffer(package, out serialized);
			package.Serialize(ref serialized, NetworkUtils.PackageHeaderSize);
			NetworkUtils.PackageTypeToByteArray(package.Type, ref serialized);

			if((package.Flags & PackageFlags.Reliable) != 0)
			{
				NetworkUtils.Adler32(ref serialized, 0, serialized.Length - sizeof(int)).Convert(ref serialized, serialized.Length - sizeof(int));
			}
			else if((package.Flags& PackageFlags.VeryReliable) != 0)
			{
				NetworkUtils.CRC32(ref serialized, 0, serialized.Length - sizeof(int)).Convert(ref serialized, serialized.Length - sizeof(int));
			}

			if (package.NeedCompress)
			{
				using (MemoryStream stream = new MemoryStream(serialized))
				{
					using (DeflateStream deflateStream = new DeflateStream(stream, CompressionLevel.Fastest))
					{
						deflateStream.Write(serialized);
					}
					serialized = stream.ToArray();
				}
			}
			else if(package.NeedHighCompress)
			{
				using (MemoryStream stream = new MemoryStream(serialized))
				{
					using (DeflateStream deflateStream = new DeflateStream(stream, CompressionLevel.Optimal))
					{
						deflateStream.Write(serialized);
					}
					serialized = stream.ToArray();
				}
			}
		}

		public async Task SendPackage(IPackage package, IPEndPoint point)
		{
			try
			{
				SerializePackage(package, out var bytes);

				if (package.NeedACK)
				{
					byte[] newArray = new byte[bytes.Length + sizeof(int)];
					Array.Copy(bytes, newArray, bytes.Length);

					int id = CurrentPackageID;
					id.Convert(ref newArray, bytes.Length);

					var info = new ACKInfo(id, Time);
					_awaitingACKs.Add(info);
					_storedPackages.AddOrUpdate(info.ID, new ACKData(newArray, point, 0), (info, data) => _storedPackages[info] = data);
					Debug.Log(info.ID + " was added");
					_idToACK.AddOrUpdate(id, info, (sID, sACK) => _idToACK[sID] = sACK);

					bytes = newArray;
				}

				Debug.LogWarning(ListenerName + ": sending package " + package.Type.ToString() + " to the " + point.Address);
				await SendAsync(bytes, point);
			}
			catch (Exception e)
			{
				Debug.LogError(ListenerName + $": ERROR WHEN SENDING PACKAGE to the {point.Address} - " + e.Message);
				Debug.LogError(package);
			}
		}

		public async Task SendAsync(byte[] buffer) => await SendAsync(buffer, new IPEndPoint(_serverAdress, _serverPort));
		public async Task SendAsync(byte[] buffer, IPEndPoint endPoint)
		{
			await _client.SendAsync(buffer, buffer.Length, endPoint);
		}

		public void AddConnectedUser(IPEndPoint endPoint)
		{
			_connectedUsers.Add(endPoint);
		}

		public IReadOnlyList<IPEndPoint> Connected => _connectedUsers;
		private string ListenerName => $"Listener" + (_isClient ? "Client" : "Server");
		public int CurrentPackageID => _packageID++;
		public IPEndPoint ServerEndPoint => new IPEndPoint(_serverAdress, _serverPort);
		public long Time => _clientTime.ElapsedMilliseconds;
		public IPEndPoint SelfEndPoint => _selfEndPoint;
		public event Action<Type> RemoveCallback;
	}
}
