using System;

public static class BitConverterNonAlloc
{
	public static void Convert(this byte value, ref byte[] array, int offset)
	{
		array[offset] = (byte)(value & 0xFF);
	}

	public static void Convert(this int value, ref byte[] array, int offset)
	{
		array[offset + 0] = (byte)(value & 0xFF);
		array[offset + 1] = (byte)((value >> 8) & 0xFF);
		array[offset + 2] = (byte)((value >> 16) & 0xFF);
		array[offset + 3] = (byte)((value >> 24) & 0xFF);
	}

	public static void Convert(this uint value, ref byte[] array, int offset)
	{
		array[offset + 0] = (byte)(value & 0xFF);
		array[offset + 1] = (byte)((value >> 8) & 0xFF);
		array[offset + 2] = (byte)((value >> 16) & 0xFF);
		array[offset + 3] = (byte)((value >> 24) & 0xFF);
	}

	public static void Convert(this long value, ref byte[] array, int offset)
	{
		array[offset + 0] = (byte)(value & 0xFF);
		array[offset + 1] = (byte)((value >> 8) & 0xFF);
		array[offset + 2] = (byte)((value >> 16) & 0xFF);
		array[offset + 3] = (byte)((value >> 24) & 0xFF);
		array[offset + 4] = (byte)((value >> 32) & 0xFF);
		array[offset + 5] = (byte)((value >> 40) & 0xFF);
		array[offset + 6] = (byte)((value >> 48) & 0xFF);
		array[offset + 7] = (byte)((value >> 56) & 0xFF);
	}

	public static void Convert(this ulong value, ref byte[] array, int offset)
	{
		array[offset + 0] = (byte)(value & 0xFF);
		array[offset + 1] = (byte)((value >> 8) & 0xFF);
		array[offset + 2] = (byte)((value >> 16) & 0xFF);
		array[offset + 3] = (byte)((value >> 24) & 0xFF);
		array[offset + 4] = (byte)((value >> 32) & 0xFF);
		array[offset + 5] = (byte)((value >> 40) & 0xFF);
		array[offset + 6] = (byte)((value >> 48) & 0xFF);
		array[offset + 7] = (byte)((value >> 56) & 0xFF);
	}
}