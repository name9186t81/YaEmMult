using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace Networking
{
	public static class NetworkUtils
	{
		public static readonly int PackageHeaderSize;
		private static readonly uint[] _CRC32Table;

		public const int TIMEOUT_DELAY = 5000;
		private const float INVERSE_MAX_BYTE = 1f / byte.MaxValue;
		private const float INVERSE_MAX_USHORT = 1f / ushort.MaxValue;
		private const float INVERSE_MAX_UINT = 1f / uint.MaxValue;

		static NetworkUtils()
		{
			PackageHeaderSize = sizeof(PackageType);
			_CRC32Table = CreateCRC32Table();
		}

		public static int GetOffset(this IPackage package)
		{
			if (package.NeedACK) return PackageHeaderSize + sizeof(int);
			return PackageHeaderSize;
		}
		public static long GetTmeStamp(byte[] buffer)
		{
			return BitConverter.ToInt64(buffer, sizeof(PackageType));
		}
		/// <summary>
		/// Shortcut for creating UDP packages.
		/// </summary>
		/// <returns></returns>
		public static Socket CreateUDP()
		{
			return new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void AddVector2ToBuffer(this Vector2 vector, byte[] buffer, int offset)
		{
			BitConverter.SingleToInt32Bits(vector.x).Convert(ref buffer, offset);
			BitConverter.SingleToInt32Bits(vector.y).Convert(ref buffer, offset + sizeof(float));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 GetVector2FromBuffer(byte[] buffer, int offset)
		{
			return new Vector2(BitConverter.Int32BitsToSingle(BitConverter.ToInt32(buffer, offset)), BitConverter.Int32BitsToSingle(BitConverter.ToInt32(buffer, offset + sizeof(float))));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 GetVector2FromBuffer(ReadOnlySpan<byte> buffer, int offset)
		{
			return new Vector2(BitConverter.ToSingle(buffer.Slice(offset, sizeof(float))), BitConverter.ToSingle(buffer.Slice(offset + sizeof(float), sizeof(float))));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Quantitize(float value, float scaleFactor, out byte res)
		{
			res = (byte)(value * scaleFactor);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Quantitize(float value, float scaleFactor, out short res)
		{
			res = (short)(value * scaleFactor);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Quantitize(float value, float scaleFactor, out int res)
		{
			res = (int)(value * scaleFactor);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DeQuantitize(byte value, float scaleFactor, out float res)
		{
			res = (float)value / scaleFactor;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DeQuantitize(short value, float scaleFactor, out float res)
		{
			res = (float)value / scaleFactor;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DeQuantitize(int value, float scaleFactor, out float res)
		{
			res = (float)value / scaleFactor;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Quantitize(float value, float min, float max, out byte res)
		{
			res = (byte)(Mathf.InverseLerp(min, max, value) * byte.MaxValue);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Quantitize(float value, float min, float max, out ushort res)
		{
			res = (ushort)(Mathf.InverseLerp(min, max, value) * ushort.MaxValue);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Quantitize(float value, float min, float max, out uint res)
		{
			res = (uint)(Mathf.InverseLerp(min, max, value) * uint.MaxValue);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DeQuantitize(byte value, float min, float max, out float res)
		{
			res = Mathf.Lerp(min, max, value * INVERSE_MAX_BYTE);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DeQuantitize(ushort value, float min, float max, out float res)
		{
			res = Mathf.Lerp(min, max, value * INVERSE_MAX_USHORT);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DeQuantitize(uint value, float min, float max, out float res)
		{
			res = Mathf.Lerp(min, max, value * INVERSE_MAX_UINT);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EnsurePackageType(PackageType type, ref byte[] data)
		{
			return GetPackageType(data) == type;
		}

		public static int GetRealPackageSize(this IPackage package)
		{
			return package.Size + PackageHeaderSize;
		}
		/// <summary>
		/// Parses special IPs, like localhost and any.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="ip"></param>
		/// <returns></returns>
		public static bool TryParseSpecialIP(string text, out IPAddress ip)
		{
			switch (text)
			{
				case "localhost":
				{
					ip = IPAddress.Loopback;
					return true;
				}
				case "any":
				{
					ip = IPAddress.Any;
					return true;
				}
			}

			ip = default;
			return false;
		}

		/// <summary>
		/// Makes first bytes of data contain PackageType.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="data"></param>
		public static void PackageTypeToByteArray(PackageType type, ref byte[] data)
		{
			byte[] buffer = new byte[sizeof(PackageType)];
			try
			{
				var ttype = Enum.GetUnderlyingType(typeof(PackageType));

				if (ttype == typeof(byte))
				{
					buffer = BitConverter.GetBytes((byte)type);
				}
				else if (ttype == typeof(short))
				{
					buffer = BitConverter.GetBytes((short)type);
				}
				else if (ttype == typeof(int))
				{
					buffer = BitConverter.GetBytes((int)type);
				}
				else if (ttype == typeof(long))
				{
					buffer = BitConverter.GetBytes((long)type);
				}
			}
			catch(Exception ex)
			{
				Debug.LogError($"SERVER UTILS: Cannot convert package to byte array {ex.Message}");
			}

			Array.Copy(buffer, data, sizeof(PackageType));
		}

		/// <summary>
		/// Generates checksum, less reliable to calculate than CRC32.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static int Adler32(ref byte[] data) => Adler32(ref data, 0, data.Length);

		/// <summary>
		/// Generates checksum, less reliable to calculate than CRC32.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static int Adler32(ref byte[] data, int start) => Adler32(ref data, start, data.Length);

		/// <summary>
		/// Generates checksum, less reliable to calculate than CRC32.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static int Adler32(ref byte[] data, int start, int end)
		{
			int a = 1, b = 0;
			for (int i = start; i < end; ++i)
			{
				a = (a + data[i]) % 65521;
				b = (a + b) % 65521;
			}

			return (b << 16) | a;
		}

		/// <summary>
		/// Generates very reliable checksum, more expensive to calculate than Adler32.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static uint CRC32(ref byte[] data) => CRC32(ref data, 0, data.Length);

		/// <summary>
		/// Generates very reliable checksum, more expensive to calculate than Adler32.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static uint CRC32(ref byte[] data, int start) => CRC32(ref data, start, data.Length);


		/// <summary>
		/// Generates very reliable checksum, more expensive to calculate than Adler32.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static uint CRC32(ref byte[] data, int start, int end)
		{
			uint crc = uint.MaxValue;

			for (int i = start; i < end; ++i)
			{
				crc = (crc >> 8) ^ _CRC32Table[(crc ^ data[i]) & 0xFF];
			}
			return ~crc;
		}

		private static uint[] CreateCRC32Table()
		{
			uint[] table = new uint[32];
			for(uint i = 0; i < 256; ++i)
			{
				uint entry = i;
				for(int j = 0; j < 8; ++j)
				{
					if((entry & 1) == j)
					{
						entry = (entry >> 1) ^ 0xEDB88320;
					}
					else
					{
						entry >>= 1;
					}
				}
			}
			return table;
		}

		public static PackageType GetPackageType(byte[] data) => GetPackageType(ref data);
		/// <summary>
		/// Converts first bytes of package to PackageType.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static PackageType GetPackageType(ref byte[] data)
		{
			if(data.Length < sizeof(PackageType))
			{
				return PackageType.Invalid;
			}

			byte[] array = new byte[sizeof(PackageType)];
			Array.Copy(data, array, sizeof(PackageType));

			try
			{
				var type = Enum.GetUnderlyingType(typeof(PackageType));
				object enumObj = null;

				if (type == typeof(byte))
				{
					enumObj = array[0];
				}
				else if (type == typeof(short))
				{
					enumObj = BitConverter.ToInt16(data);
				}
				else if (type == typeof(int))
				{
					enumObj = BitConverter.ToInt32(data);
				}
				else if (type == typeof(long))
				{
					enumObj = BitConverter.ToInt64(data);
				}
				else
				{
					return PackageType.Invalid;
				}

				return (PackageType)Enum.ToObject(typeof(PackageType), enumObj);
			}
			catch (Exception ex)
			{
				return PackageType.Invalid;
			}
		}

		public static PackageType GetPackageType(ReadOnlySpan<byte> data)
		{
			if (data.Length < sizeof(PackageType))
			{
				return PackageType.Invalid;
			}

			try
			{
				var type = Enum.GetUnderlyingType(typeof(PackageType));
				object enumObj = null;

				if (type == typeof(byte))
				{
					enumObj = data[0];
				}
				else if (type == typeof(short))
				{
					enumObj = BitConverter.ToInt16(data);
				}
				else if (type == typeof(int))
				{
					enumObj = BitConverter.ToInt32(data);
				}
				else if (type == typeof(long))
				{
					enumObj = BitConverter.ToInt64(data);
				}
				else
				{
					return PackageType.Invalid;
				}

				return (PackageType)Enum.ToObject(typeof(PackageType), enumObj);
			}
			catch (Exception ex)
			{
				return PackageType.Invalid;
			}
		}
	}
}
