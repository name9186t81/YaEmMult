using System;
using System.Buffers;
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

using TMPro;

using Debug = UnityEngine.Debug;

namespace Networking
{
	public class ListenerBase : IDisposable
	{
		private class ACKInfo
		{
			public long Time;
			public byte Tries;
			public readonly IPEndPoint Destination;
			public readonly byte[] Data;

			public ACKInfo(long time, IPEndPoint destination, byte[] data)
			{
				Time = time;
				Destination = destination;
				Data = data;
				Tries = 0;
			}
		}

		protected struct ReceivedInfo
		{
			public byte[] Memory;
			public int Received;
			public IPEndPoint Sender;
		}

		protected readonly struct PendingPackage
		{
			public readonly IPackage Package;
			public readonly byte[] Data;
			public readonly PackageSendDestination Destination;
			public readonly IPEndPoint Point;
			public readonly bool NeedACK;

			public PendingPackage(IPackage package, PackageSendDestination destination, IPEndPoint point)
			{
				Package = package;
				Data = null;
				Destination = destination;
				Point = point;
				NeedACK = package.NeedACK;
			}

			public PendingPackage(byte[] package, PackageSendDestination destination, IPEndPoint point, bool needACK)
			{
				Package = null;
				Data = package;
				Destination = destination;
				Point = point;
				NeedACK = needACK;
			}
		}

		protected enum ProcessResult
		{
			Success,
			Fail,
			UnknowPackage
		}

		public enum PackageSendOrder
		{
			Instant,
			AfterProcessing,
			NextTick
		}

		public enum PackageSendDestination
		{
			Everyone,
			Concrete,
			EveryoneExcept
		}

		private readonly List<IPEndPoint> _connected = new List<IPEndPoint>();
		private readonly ReaderWriterLockSlim _connectedLock = new ReaderWriterLockSlim();

		private readonly ConcurrentDictionary<int, ACKInfo> _pendingACKs = new ConcurrentDictionary<int, ACKInfo>();
		private readonly ConcurrentDictionary<int, long> _receivedACKs = new ConcurrentDictionary<int, long>();

		private readonly ConcurrentQueue<ReceivedInfo> _pendingPackages = new ConcurrentQueue<ReceivedInfo>();
		private readonly ConcurrentQueue<PendingPackage> _awaitingPackagesNextTick = new ConcurrentQueue<PendingPackage>();
		private readonly ConcurrentQueue<PendingPackage> _awaitingPackagesNextProcess = new ConcurrentQueue<PendingPackage>();

		private readonly static ConcurrentBag<ITickBasedPackageProcessor> _tickProcessors;
		private readonly static ConcurrentDictionary<PackageType, PackageFlags> _typeToFlags;
		private readonly static ConcurrentDictionary<PackageType, IPackageProcessor> _typeToProcessor;
		private readonly static ConcurrentDictionary<PackageType, ProcessorAttribute> _typeToProcessorAttribte;

		private readonly CancellationTokenSource _cts = new CancellationTokenSource();
		private readonly Socket _listeningSocket;
		private readonly SemaphoreSlim _queueSignal;

		private readonly int _receivingThreadsCount = Environment.ProcessorCount / 2;
		private readonly int _processingThreadsCount = Environment.ProcessorCount;

		private readonly ArrayPool<ReceivedInfo> _processingPool;
		private volatile bool _isRunning;
		private int _ackID;

		private readonly Stopwatch _watch;
		private long _syncedTimeDifference;

		private long _receivedPackages;
		private long _processedPackages;

		private long _tickRate;
		private long _lastTick;
		private Task _tickThread;
		private Task _acksThread;

		public long ReceivedPackages => _receivedPackages;
		public long ProcessedPackages => _processedPackages;

		public const int MTU = 1400;
		public const int MAX_ACK_TRIES = 7;
		public const int MAX_ACK_TIMEOUT = 5000;

		public ListenerBase(int port = 0, int listenersCount = -1, int processingCount = -1, long tickFrequancy = -1)
		{
			_listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_listeningSocket.Bind(new IPEndPoint(IPAddress.Any, port));
			_listeningSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, false);
			_listeningSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.NoDelay, true);
			_listeningSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1);
			_listeningSocket.ReceiveBufferSize = 1;
			_listeningSocket.SendBufferSize = 8 * 1024;
			_listeningSocket.Blocking = false;
			_listeningSocket.DontFragment = true;
			_ackID = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

			if (listenersCount != -1)
			{
				_receivingThreadsCount = Math.Clamp(listenersCount, 1, Environment.ProcessorCount);
			}
			if (processingCount != -1)
			{
				_processingThreadsCount = Math.Clamp(listenersCount, 1, Environment.ProcessorCount);
			}

			_queueSignal = new SemaphoreSlim(0);
			_watch = new Stopwatch();
			_watch.Start();

			_isRunning = true;
			_processingPool = ArrayPool<ReceivedInfo>.Create(MTU, 50);
			StartListening();
			StartProcessing();

			if (tickFrequancy != -1)
			{
				_tickRate = tickFrequancy;
				_tickThread = Task.Run(() => TickingLoop(_cts.Token), _cts.Token);
			}

			_acksThread = Task.Run(() => ACKsLoop(_cts.Token), _cts.Token);

			ActorSyncFromServerPackage test = new ActorSyncFromServerPackage(UnityEngine.Vector2.zero, 0f, 0);
			SerializePackage(test);

		}

		static ListenerBase()
		{
			var processorTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(IPackageProcessor).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
			var packagesTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(IPackage).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

			_tickProcessors = new ConcurrentBag<ITickBasedPackageProcessor>();
			_typeToProcessorAttribte = new ConcurrentDictionary<PackageType, ProcessorAttribute>();
			_typeToProcessor = new ConcurrentDictionary<PackageType, IPackageProcessor>();
			_typeToFlags = new ConcurrentDictionary<PackageType, PackageFlags>();

			foreach (var processorType in processorTypes)
			{
				var atr = processorType.GetCustomAttribute<ProcessorAttribute>();
				if (atr != null)
				{
					try
					{
						var instance = Activator.CreateInstance(processorType);
						_typeToProcessor.TryAdd(atr.ProcessedType, (IPackageProcessor)instance);
						_typeToProcessorAttribte.TryAdd(atr.ProcessedType, atr);

						if(instance is ITickBasedPackageProcessor tickBased)
						{
							_tickProcessors.Add(tickBased);
						}
					}
					catch (Exception ex)
					{
						Debug.LogException(ex);
					}
				}
			}

			foreach(var packageType in packagesTypes)
			{
				var atr = packageType.GetCustomAttribute<PackageAttribute>();
				if (atr != null)
				{
					if (!_typeToFlags.TryAdd(atr.PackageType, atr.Flags))
					{
						Debug.LogError("Failed to add package of a type " +  atr.PackageType);
					}
				}
			}
		}

		protected static void ProcessTicked(ListenerBase listener, float rate)
		{
			foreach(var proc in _tickProcessors)
			{
				proc.Tick(rate, listener);
			}
		}

		private void StartProcessing()
		{
			Debug.Log("Starting " + _receivingThreadsCount + " processing workers");
			for (int i = 0; i < _processingThreadsCount; i++)
			{
				Task.Factory.StartNew(() => ProcessLoop(_cts.Token), _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
			}
		}

		private void StartListening()
		{
			Debug.Log("Starting " + _receivingThreadsCount + " listening workers");
			for(int i = 0; i < _receivingThreadsCount; i++)
			{
				Task.Factory.StartNew(() => ListenLoop(_cts.Token), _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
			}
		}

		private async Task ListenLoop(CancellationToken ct)
		{
			const int BUFFER_COUNT = 32;
			byte[][] buffers = new byte[BUFFER_COUNT][];
			var tasks = new Task<SocketReceiveFromResult>[BUFFER_COUNT];

			for(int i = 0; i < BUFFER_COUNT; i++)
			{
				buffers[i] = ArrayPool<byte>.Shared.Rent(MTU);
			}

			EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
			int buffer = 0;

			while (_isRunning && !ct.IsCancellationRequested)
			{
				try
				{
					for(int i = 0; i < BUFFER_COUNT; i++)
					{
						if (tasks[i]?.IsCompleted != false)
							tasks[i] = _listeningSocket.ReceiveFromAsync(buffers[i], SocketFlags.None, sender);
					}

					var completed = Task.WhenAny(tasks);
					var res = await completed;
					int ind = Array.IndexOf(tasks, res);

					if (res.Result.ReceivedBytes > 0)
					{
						Debug.Log("Received " + res.Result.ReceivedBytes + " bytes avaible " + _listeningSocket.Available);
						_pendingPackages.Enqueue(new ReceivedInfo() { Memory = buffers[ind], Sender = (IPEndPoint)res.Result.RemoteEndPoint, Received = res.Result.ReceivedBytes });
						_queueSignal.Release();

						Interlocked.Increment(ref _receivedPackages);

						buffers[ind] = ArrayPool<byte>.Shared.Rent(MTU);
						tasks[ind] = _listeningSocket.ReceiveFromAsync(buffers[ind], SocketFlags.None, sender);
					}
				}
				catch (Exception e)
				{
					Debug.LogError(e);
				}
			}
		}

		private async Task ProcessLoop(CancellationToken ct)
		{
			ManualResetEventSlim reset = new ManualResetEventSlim(false);
			int batchCount = 64;
			var batch = _processingPool.Rent(batchCount);
				
			while (_isRunning && !ct.IsCancellationRequested)
			{
				_queueSignal.Wait(ct);

				int counter = 0;
				while(_pendingPackages.TryDequeue(out var package) && counter < batchCount)
				{
					batch[counter++] = package;
				}

				if(counter > 0)
				{
					if (counter > 2)
					{
						Parallel.For(0, counter, i =>
						{
							var package = batch[i];
							try
							{
								ProcessInternal(new ReadOnlySpan<byte>(package.Memory, 0, package.Received), package.Sender);
							}
							catch (Exception ex)
							{
								Debug.LogError("ERROR OCCURED WHILE PROCESSING PACKAGE WORKER ID " + Thread.CurrentThread.ManagedThreadId + " ERROR: " + ex.Message);
							}
							finally
							{
								Interlocked.Increment(ref _processedPackages);
							}
						});
					}
					else
					{
						for(int i = 0; i < counter; i++)
						{
							try
							{
								ProcessInternal(new ReadOnlySpan<byte>(batch[i].Memory, 0, batch[i].Received), batch[i].Sender);
							}
							catch(Exception ex)
							{
								Debug.LogError("ERROR OCCURED WHILE PROCESSING PACKAGE ERROR: " + ex.Message);
							}
							finally
							{
								Interlocked.Increment(ref _processedPackages);
							}
						}
					}

					_processingPool.Return(batch);
				}

				Debug.Log(_awaitingPackagesNextProcess.Count + " AWAITING");
				if(_awaitingPackagesNextProcess.Count > 0)
				{
					if(_awaitingPackagesNextProcess.Count > 3)
					{
						var batchProc = new List<Task>();
						while (_awaitingPackagesNextProcess.TryDequeue(out var package))
						{
							batchProc.Add(package.Package != null ? SendPackageInternal(package.Package, package.Destination, package.Point) : SendPackageInternal(package.Data, package.Destination, package.Point, package.NeedACK));
						}
						await Task.WhenAll(batchProc);
					}
					else
					{
						while (_awaitingPackagesNextProcess.TryDequeue(out var package))
						{
							await (package.Package != null ? SendPackageInternal(package.Package, package.Destination, package.Point) : SendPackageInternal(package.Data, package.Destination, package.Point, package.NeedACK));
						}
					}
				}
				reset.Wait(TimeSpan.FromMilliseconds(10));
				reset.Reset();
			}
		}

		private async Task ACKsLoop(CancellationToken ct)
		{
			ManualResetEventSlim reset = new ManualResetEventSlim(false);

			while (!ct.IsCancellationRequested)
			{
				if (_pendingACKs.Count > 0)
				{
					LinkedList<int> toRemove = new LinkedList<int>();

					foreach (var pair in _pendingACKs)
					{
						if ((RunTime - pair.Value.Time) > MAX_ACK_TIMEOUT)
						{
							int tries = pair.Value.Tries;
							if (tries == MAX_ACK_TRIES)
							{
								LostConnection(pair.Value.Destination);
								toRemove.AddLast(pair.Key);
								continue;
							}

							pair.Value.Tries++;
							pair.Value.Time = RunTime;
							await SendPackageInternal(pair.Value.Data, pair.Value.Destination, false);
						}
					}

					foreach(var ids in toRemove)
					{
						_pendingACKs.TryRemove(ids, out _);
					}
				}
				if(_receivedACKs.Count > 0)
				{
					LinkedList<int> toRemove = new LinkedList<int>();

					foreach (var ack in _receivedACKs)
					{
						if(RunTime - ack.Value >  MAX_ACK_TIMEOUT * MAX_ACK_TRIES)
						{
							toRemove.AddLast(ack.Key);
						}
					}

					foreach(var ids in toRemove)
					{
						_receivedACKs.TryRemove(ids, out _);
					}
				}
				reset.Wait(TimeSpan.FromMilliseconds(10));
				reset.Reset();
			}
		}

		private async Task TickingLoop(CancellationToken ct)
		{
			Stopwatch watch = Stopwatch.StartNew();
			long error = 0;

			while (!ct.IsCancellationRequested)
			{
				if(watch.ElapsedMilliseconds - _lastTick - error > _tickRate)
				{
					error = watch.ElapsedMilliseconds - _lastTick - error - _tickRate;

					_lastTick = watch.ElapsedMilliseconds;

					Ticked();
					await TickedInternal();
				}
			}
		}

		private void ProcessInternal(ReadOnlySpan<byte> buffer, IPEndPoint point)
		{
			var header = NetworkUtils.GetPackageType(buffer);
			Debug.Log("Processing - " +  header);
			if(header == PackageType.Ack)
			{
				ACKPackage package = new ACKPackage();
				package.Deserialize(buffer, NetworkUtils.PackageHeaderSize);
				if(!_pendingACKs.TryRemove(package.ID, out _))
				{
					Debug.LogError("Failed to remove ACK");
				}
				_receivedACKs.TryAdd(package.ID, RunTime);
				return;
			}

			if (_typeToProcessor.TryGetValue(header, out var processor))
			{
				if(_typeToFlags.TryGetValue(header, out var flags) && (flags & PackageFlags.Compress) != 0)
				{
					var dBuffer = ArrayPool<byte>.Shared.Rent(MTU);

					try
					{
						using MemoryStream output = new MemoryStream();
						using MemoryStream ms = new MemoryStream(buffer.Slice(NetworkUtils.PackageHeaderSize, buffer.Length - NetworkUtils.PackageHeaderSize).ToArray());
						using (DeflateStream compression = new DeflateStream(ms, CompressionMode.Decompress))
						{
							int read = 0;
							while ((read = compression.Read(dBuffer, 0, dBuffer.Length)) > 0)
							{
								compression.Write(dBuffer, 0, read);
							}
						}
					}
					finally
					{
						ArrayPool<byte>.Shared.Return(dBuffer);
					}
				}

				if((flags & PackageFlags.NeedACK) != 0)
				{
					int id = BitConverter.ToInt32(buffer.Slice(NetworkUtils.PackageHeaderSize, sizeof(int)));
					if (_receivedACKs.TryGetValue(id, out var time))
					{
						if(RunTime - time < MAX_ACK_TRIES * MAX_ACK_TIMEOUT)
						{
							ACKPackage package = new ACKPackage(ACKPackage.ACKResult.Success, id);
							SendPackageInternal(package, PackageSendDestination.Concrete, point);
							Debug.Log("Dropped");
							return;
						}
					}
					else
					{
						ACKPackage package = new ACKPackage(ACKPackage.ACKResult.Success, id);
						SendPackageInternal(package, PackageSendDestination.Concrete, point);
						_receivedACKs.TryAdd(id, RunTime);
					}
				}

				if(_typeToProcessorAttribte.TryGetValue(header, out var att) && att.Type == ProcessorAttribute.ProcessorType.Both)
				{
					processor.Process(buffer, _cts, point, this);
				}
				else
				{
					Debug.Log("Internal process result " + Process(header, buffer, point));
				}
			}
			else
			{
				Debug.LogError("No processor for type " + header);
			}
		}

		/// <summary>
		/// Instantly sends a package to concrete user.
		/// </summary>
		/// <param name="package"></param>
		/// <param name="point"></param>
		/// <param name="needACK"></param>
		/// <returns></returns>
		private async Task SendPackageInternal(byte[] package, IPEndPoint point, bool needACK)
		{
			if (needACK)
			{
				int id = BitConverter.ToInt32(package, NetworkUtils.PackageHeaderSize);
				if (!_pendingACKs.TryAdd(id, new ACKInfo(RunTime, point, package)))
				{
					Debug.LogError("Failed to add ack");
				}
			}

			Debug.Log("Sending raw " + package.Length.ToString() + " bytes to " + point.ToString());
			await _listeningSocket.SendToAsync(package, SocketFlags.None, point);
		}

		/// <summary>
		/// Serializes package and adds it in a batch to send later.
		/// </summary>
		/// <param name="package"></param>
		/// <param name="destination"></param>
		/// <param name="point"></param>
		/// <returns></returns>
		private async Task SendPackageInternal(IPackage package, PackageSendDestination destination, IPEndPoint point)
		{
			var buffer = SerializePackage(package);
			await SendPackageInternal(buffer, destination, point, package.NeedACK);
		}

		/// <summary>
		/// Adds package in batches to send them later. See PackageSendDestination.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="destination"></param>
		/// <param name="point"></param>
		/// <param name="needACK"></param>
		/// <returns></returns>
		private async Task SendPackageInternal(byte[] buffer, PackageSendDestination destination, IPEndPoint point, bool needACK)
		{
			try
			{
				switch (destination)
				{
					case PackageSendDestination.Concrete:
					{
						await SendPackageInternal(buffer, point, needACK);
						break;
					}
					case PackageSendDestination.Everyone:
					{
						var tasks = new List<Task>();
						for (int i = 0; i < _connected.Count; i++)
						{
							tasks.Add(SendPackageInternal(buffer, GetConnected(i), needACK));
						}
						await Task.WhenAll(tasks);
						break;
					}
					case PackageSendDestination.EveryoneExcept:
					{
						var tasks = new List<Task>();
						int n = 0;
						for (; n < _connected.Count && _connected[n] != point; n++)
						{
							tasks.Add(SendPackageInternal(buffer, GetConnected(n), needACK));
						}
						for (int i = n + 1; i < _connected.Count; i++)
						{
							tasks.Add(SendPackageInternal(buffer, GetConnected(i), needACK));
						}
						await Task.WhenAll(tasks);
						break;
					}
				}
			}
			finally
			{
				try
				{
					ArrayPool<byte>.Shared.Return(buffer);
				}
				catch (Exception)
				{
				}
			}
		}

		private byte[] SerializePackage(in IPackage package)
		{
			int size = package.GetRealPackageSize();
			if (package.NeedACK) 
				size += sizeof(int);

			var buffer = ArrayPool<byte>.Shared.Rent(size);

			NetworkUtils.PackageTypeToByteArray(package.Type, ref buffer);
			if (package.NeedACK)
			{
				Interlocked.Increment(ref _ackID).Convert(ref buffer, NetworkUtils.PackageHeaderSize);
			}

			package.Serialize(ref buffer, NetworkUtils.PackageHeaderSize + (package.NeedACK ? sizeof(int) : 0));

			if (package.NeedCompress)
			{
				using MemoryStream ms = new MemoryStream();
				using (DeflateStream compression = new DeflateStream(ms, CompressionMode.Compress))
				{
					compression.Write(buffer, NetworkUtils.PackageHeaderSize, buffer.Length - NetworkUtils.PackageHeaderSize);
				}

				buffer = ms.ToArray();
			}

			return buffer;
		}

		private async Task TickedInternal()
		{
			Debug.Log("TICK :" + _awaitingPackagesNextTick.Count);
			if(_awaitingPackagesNextTick.Count > 0)
			{
				if(_awaitingPackagesNextTick.Count > 3)
				{
					var batch = new List<Task>();
					while(_awaitingPackagesNextTick.TryDequeue(out var package))
					{
						batch.Add(package.Package != null ? SendPackageInternal(package.Package, package.Destination, package.Point) : SendPackageInternal(package.Data, package.Destination, package.Point, package.NeedACK));
					}
					await Task.WhenAll(batch);
				}
				else
				{
					while(_awaitingPackagesNextTick.TryDequeue(out var package))
					{
						await (package.Package != null ? SendPackageInternal(package.Package, package.Destination, package.Point) : SendPackageInternal(package.Data, package.Destination, package.Point, package.NeedACK));
					}
				}
			}
		}

		protected virtual ProcessResult Process(PackageType type, ReadOnlySpan<byte> buffer, IPEndPoint point) { return ProcessResult.Success; }
		/// <summary>
		/// Called every tick.
		/// </summary>
		protected virtual void Ticked() { }

		protected virtual void LostConnection(IPEndPoint target) { }

		protected void SyncTime(long time)
		{
			_syncedTimeDifference = time - _watch.ElapsedMilliseconds;
		}

		public async Task SendPackage(IPackage package, PackageSendOrder order, PackageSendDestination destination, IPEndPoint end = null)
		{
			Debug.Log("Sending package: " + package.Type + " order: " + order + " destination: " + destination + " point: " + (end != null ? end.ToString() : ""));
			switch (order)
			{
				case PackageSendOrder.Instant:
				{
					await SendPackageInternal(package, destination, end);
					break;
				}
				case PackageSendOrder.NextTick:
				{
					_awaitingPackagesNextTick.Enqueue(new PendingPackage(package, destination, end));
					break;
				}
				case PackageSendOrder.AfterProcessing:
				{
					throw new Exception();
					Debug.Log(_awaitingPackagesNextProcess.Count + " " + _cts.Token.IsCancellationRequested);
					_awaitingPackagesNextProcess.Enqueue(new PendingPackage(package, destination, end));
					break;
				}
			}
		}

		public async Task SendPackage(byte[] package, PackageSendOrder order, PackageSendDestination destination, IPEndPoint end = null, bool needACK = false)
		{
			switch (order)
			{
				case PackageSendOrder.Instant:
				{
					await SendPackageInternal(package, destination, end, needACK);
					break;
				}
				case PackageSendOrder.NextTick:
				{
					_awaitingPackagesNextTick.Enqueue(new PendingPackage(package, destination, end, needACK));
					break;
				}
				case PackageSendOrder.AfterProcessing:
				{
					throw new Exception();
					_awaitingPackagesNextProcess.Enqueue(new PendingPackage(package, destination, end, needACK));
					break;
				}
			}
		}

		public bool TryGetProcessorByType(PackageType type, out IPackageProcessor processor) => _typeToProcessor.TryGetValue(type, out processor);
		public bool TryGetPackageFlags(PackageType type) => _typeToFlags.TryGetValue(type, out var flags);

		public IPEndPoint GetConnected(int index)
		{
			_connectedLock.EnterReadLock();
			try
			{
				return _connected[index];
			}
			finally
			{
				_connectedLock.ExitReadLock();
			}
		}

		public virtual void AddConnected(IPEndPoint client)
		{
			_connectedLock.EnterWriteLock();
			try
			{
				_connected.Add(client);
			}
			finally
			{ 
				_connectedLock.ExitWriteLock(); 
			}
		}

		public virtual void RemoveConnected(IPEndPoint client)
		{
			_connectedLock.EnterWriteLock();
			try
			{
				_connected.Remove(client);
			}
			finally
			{
				_connectedLock.ExitWriteLock();
			}
		}

		public virtual void Dispose()
		{
			_isRunning = false;
			_cts.Cancel();

			_listeningSocket?.Dispose();
			_cts?.Dispose();
			_tickThread?.Dispose();
			_acksThread?.Dispose();
		}

		protected CancellationTokenSource CTS => _cts;

		public int ConnectedCount => _connected.Count;
		public long RunTime => _watch.ElapsedMilliseconds;
		public long RunTimeSynced => _watch.ElapsedMilliseconds + _syncedTimeDifference;
	}
}
