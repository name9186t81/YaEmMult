public static class BitConverterNonAlloc
{
	public static void Convert(this byte value, ref byte[] array, int offset)
	{
		array[offset] = (byte)(value & 0xFF);
	}

	public static void Convert(this int value, ref byte[] array, int offset)
	{
		array[offset + 3] = (byte)(value & 0xFF);
		array[offset + 2] = (byte)((value >> 8) & 0xFF);
		array[offset + 1] = (byte)((value >> 16) & 0xFF);
		array[offset + 0] = (byte)((value >> 24) & 0xFF);
	}

	public static void Convert(this uint value, ref byte[] array, int offset)
	{
		array[offset] = (byte)(value & 0xFF);
		array[offset + 1] = (byte)(value & 0xFF00 >> 8);
		array[offset + 2] = (byte)(value & 0xFF0000 >> 16);
		array[offset + 3] = (byte)(value & 0xFF000000 >> 24);
	}

	public static void Convert(this long value, ref byte[] array, int offset)
	{
		array[offset] = (byte)(value & 0xFF);
		array[offset + 1] = (byte)(value & 0xFF00 >> 8);
		array[offset + 2] = (byte)(value & 0xFF0000 >> 16);
		array[offset + 3] = (byte)(value & 0xFF000000 >> 24);
		array[offset + 4] = (byte)(value & 0xFF00000000 >> 32);
		array[offset + 5] = (byte)(value & 0xFF0000000000 >> 40);
		array[offset + 6] = (byte)(value & 0xFF000000000000 >> 48);
		array[offset + 7] = (byte)((ulong)value & 0xFF00000000000000 >> 56);
	}

	public static void Convert(this ulong value, ref byte[] array, int offset)
	{
		array[offset] = (byte)(value & 0xFF);
		array[offset + 1] = (byte)(value & 0xFF00 >> 8);
		array[offset + 2] = (byte)(value & 0xFF0000 >> 16);
		array[offset + 3] = (byte)(value & 0xFF000000 >> 24);
		array[offset + 4] = (byte)(value & 0xFF00000000 >> 32);
		array[offset + 5] = (byte)(value & 0xFF0000000000 >> 40);
		array[offset + 6] = (byte)(value & 0xFF000000000000 >> 48);
		array[offset + 7] = (byte)(value & 0xFF00000000000000 >> 56);
	}
}