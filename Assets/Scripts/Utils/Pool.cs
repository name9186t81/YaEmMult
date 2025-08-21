using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public sealed class Pool<T>
{
	/// <summary>
	/// stores objects that are ready for use
	/// </summary>
	private readonly Queue<T> _ready;
	/// <summary>
	/// function for creation new objects
	/// </summary>
	private readonly Func<T> _createFunc;

	/// <summary>
	/// called whenever object returned back to pool
	/// </summary>
	public event Action<T> OnReturn;
	public event Action OnDestroy;

	public Pool(Func<T> createFunc)
	{
		_ready = new Queue<T>();
		_createFunc = createFunc;
	}

	/// <summary>
	/// returns object in pool
	/// </summary>
	/// <param name="item"></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ReturnToPool(T item)
	{
		_ready.Enqueue(item);
		OnReturn?.Invoke(item);
	}

	public void Destroy()
	{
		OnDestroy?.Invoke();
	}

	/// <summary>
	/// returns object from pool
	/// </summary>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Get()
	{
		if (_ready.Count != 0) return _ready.Dequeue();
		return _createFunc();
	}

	/// <summary>
	/// objects that are ready
	/// </summary>
	public IEnumerable<T> Stored => _ready;
}
