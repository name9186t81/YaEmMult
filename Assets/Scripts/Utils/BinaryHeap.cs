using System;

public sealed class BinaryHeap<T> where T : IComparable<T>
{
	public enum Mode
	{
		Biggest = 1,
		Lowest = -1
	}

	private Mode _mode;
	private T[] _array;
	private int _capacity;
	private int _size;

	public BinaryHeap(int size, Mode mode)
	{
		_capacity = size;
		_array = new T[_capacity];
		_size = 0;
		_mode = mode;
	}

	public BinaryHeap(Mode mode)
	{
		_capacity = _size = 0;
		_array = new T[0];
		_mode = mode;
	}

	public void Insert(T item)
	{
		if(_size == _capacity)
		{
			if (_capacity == 0) _capacity = 1;
			_capacity <<= 1;
			T[] newArray = new T[_capacity];
			Array.Copy(_array, newArray, _array.Length);
			_array = newArray;
		}

		int end = _size;
		_array[end] = item;
		_size++;

		for(int i = end; i != 0 && _array[i].CompareTo(_array[GetParentIndex(i)]) == (int)_mode; i = GetParentIndex(i))
		{
			Swap(ref _array[i], ref _array[GetParentIndex(i)]);
		}
	}

	public T ExtractMinimal()
	{
		if(_size <= 0)
		{
			throw new InvalidOperationException("Heap is empty");
		}

		if(_size == 1)
		{
			_size = 0;
			return _array[0];
		}

		T root = _array[0];
		_array[0] = _array[_size - 1];
		_size--;
		Heapify(0);

		return root;
	}

	public T PeekMinimal()
	{
		if (_size <= 0)
		{
			throw new InvalidOperationException("Heap is empty");
		}

		return _array[0];
	}

	private void Heapify(int root)
	{
		int left = GetLeftIndex(root);
		int right = GetRightIndex(root);

		int comp = root;

		if(left < _size && _array[left].CompareTo(_array[comp]) == (int)_mode)
		{
			comp = left;
		}
		if (right < _size && _array[right].CompareTo(_array[comp]) == (int)_mode)
		{
			comp = right;
		}

		if(comp != root)
		{
			Swap(ref _array[root], ref _array[comp]);
			Heapify(comp);
		}
	}

	private int GetParentIndex(int key)
	{
		return (key - 1) >> 1;
	}

	private int GetLeftIndex(int key)
	{
		return 2 * key + 1;
	}

	private int GetRightIndex(int key)
	{
		return 2 * key + 2;
	}

	private static void Swap(ref T a, ref T b)
	{
		T temp = a;
		a = b;
		b = temp;
	}

	public int Size => _size;
}