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

using Debug = UnityEngine.Debug;

namespace Networking
{
	public class ListenerBase : IDisposable
	{
		protected struct ReceivedInfo
		{
			public byte[] Memory;
			public int Received;
			public IPEndPoint Sender;
		}

		protected readonly struct PendingPackage
		{
			public readonly IPackage Package;
			public readonly PackageSendDestination Destination;
			public readonly IPEndPoint Point;

			public PendingPackage(IPackage package, PackageSendDestination destination, IPEndPoint point)
			{
				Package = package;
				Destination = destination;
				Point = point;
			}
		}

		protected enum ProcessResult
		{
			Success,
			Fail,
			UnknowPackage
		}

		protected enum PackageSendOrder
		{
			Instant,
			AfterProcessing,
			NextTick
		}

		protected enum PackageSendDestination
		{
			Everyone,
			Concrete,
			EveryoneExcept
		}

		private readonly List<IPEndPoint> _connected = new List<IPEndPoint>();
		private readonly ReaderWriterLockSlim _connectedLock = new ReaderWriterLockSlim();

		private readonly ConcurrentQueue<ReceivedInfo> _pendingPackages = new ConcurrentQueue<ReceivedInfo>();
		private readonly ConcurrentQueue<PendingPackage> _awaitingPackagesNextTick = new ConcurrentQueue<PendingPackage>();
		private readonly ConcurrentQueue<PendingPackage> _awaitingPackagesNextProcess = new ConcurrentQueue<PendingPackage>();

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

		public long ReceivedPackages => _receivedPackages;
		public long ProcessedPackages => _processedPackages;

		public const int MTU = 1400;

		public ListenerBase(int port, int listenersCount = -1, int processingCount = -1, long tickFrequancy = -1)
		{
			_listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_listeningSocket.Bind(new IPEndPoint(IPAddress.Any, port));
			_listeningSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
			_listeningSocket.ReceiveBufferSize = 1024 * 1024;
			_listeningSocket.SendBufferSize = 1024 * 1024;

			if(listenersCount != -1)
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

			ActorSyncFromServerPackage test = new ActorSyncFromServerPackage(UnityEngine.Vector2.zero, 0f, 0);
			SerializePackage(test);
		}

		static ListenerBase()
		{
			var processorTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(IPackageProcessor).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
			var packagesTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(IPackage).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

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

		private void ListenLoop(CancellationToken ct)
		{
			EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
			while (_isRunning && !ct.IsCancellationRequested)
			{
				try
				{
					var buffer = ArrayPool<byte>.Shared.Rent(MTU);
					int received = _listeningSocket.ReceiveFrom(buffer, ref sender);

					if (received > 0)
					{
						_pendingPackages.Enqueue(new ReceivedInfo() { Memory = buffer, Sender = (IPEndPoint)sender, Received = received });
						_queueSignal.Release();

						Interlocked.Increment(ref _receivedPackages);
					}
					else
					{
						ArrayPool<byte>.Shared.Return(buffer);
					}
					sender = new IPEndPoint(IPAddress.Any, 0);
				}
				catch (Exception e)
				{
					Debug.LogError(e);
				}
			}
		}

		private void ProcessLoop(CancellationToken ct)
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
								Process(new ReadOnlySpan<byte>(package.Memory, 0, package.Received), package.Sender);
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
								Process(new ReadOnlySpan<byte>(batch[i].Memory, 0, batch[i].Received), batch[i].Sender);
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

					Ticked();
					await TickedInternal();
				}
			}
		}

		private void ProcessInternal(ReadOnlySpan<byte> buffer, IPEndPoint point)
		{
			var header = NetworkUtils.GetPackageType(buffer);
		}

		private async Task SendPackageInternal(byte[] package, IPEndPoint point)
		{
			await _listeningSocket.SendToAsync(package, SocketFlags.None, point);
		}

		private async Task SendPackageInternal(IPackage package, PackageSendDestination destination, IPEndPoint point)
		{
			var buffer = SerializePackage(package);
			try
			{
				switch (destination)
				{
					case PackageSendDestination.Concrete:
					{
						await SendPackageInternal(buffer, point);
						break;
					}
					case PackageSendDestination.Everyone:
					{
						var tasks = new List<Task>();
						for (int i = 0; i < _connected.Count; i++)
						{
							tasks.Add(SendPackageInternal(buffer, GetConnected(i)));
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
							tasks.Add(SendPackageInternal(buffer, GetConnected(n)));
						}
						for (int i = n + 1; i < _connected.Count; i++)
						{
							tasks.Add(SendPackageInternal(buffer, GetConnected(i)));
						}
						await Task.WhenAll(tasks);
						break;
					}
				}
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		private byte[] SerializePackage(in IPackage package)
		{
			int size = package.GetRealPackageSize();
			if (package.NeedACK) 
				size += sizeof(int);

			var buffer = ArrayPool<byte>.Shared.Rent(size);

			NetworkUtils.PackageTypeToByteArray(package.Type, ref buffer);
			if(package.NeedACK)
				Interlocked.Increment(ref _ackID).Convert(ref buffer, NetworkUtils.PackageHeaderSize);

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
			if(_awaitingPackagesNextTick.Count > 0)
			{
				if(_awaitingPackagesNextTick.Count > 3)
				{
					var batch = new List<Task>();
					while(_awaitingPackagesNextTick.TryDequeue(out var package))
					{
						batch.Add(SendPackageInternal(package.Package, package.Destination, package.Point));
					}
					await Task.WhenAll(batch);
				}
				else
				{
					while(_awaitingPackagesNextTick.TryDequeue(out var package))
					{
						await SendPackageInternal(package.Package, package.Destination, package.Point);
					}
				}
			}
		}

		protected virtual ProcessResult Process(ReadOnlySpan<byte> buffer, IPEndPoint point) { return ProcessResult.Success; }
		protected virtual void Ticked() { }

		protected void SyncTime(long time)
		{
			_syncedTimeDifference = time - _watch.ElapsedMilliseconds;
		}

		protected async Task SendPackage(IPackage package, PackageSendOrder order, PackageSendDestination destination, IPEndPoint end = null)
		{
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
					_awaitingPackagesNextProcess.Enqueue(new PendingPackage(package, destination, end));
					break;
				}
			}
		}

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

		public void AddConnected(IPEndPoint client)
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

		public void RemoveConnected(IPEndPoint client)
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

		public void Dispose()
		{
			_isRunning = false;
			_cts.Cancel();

			_listeningSocket?.Dispose();
			_cts?.Dispose();
			_tickThread?.Dispose();
		}

		public long RunTime => _watch.ElapsedMilliseconds;
		public long RunTimeSynced => _watch.ElapsedMilliseconds + _syncedTimeDifference;
	}
}
