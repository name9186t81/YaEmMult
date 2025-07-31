using System;

using UnityEngine;

namespace Networking
{
	[Package(PackageFlags.None, PackageType.PingResponse)]
	public struct PingResponsePackage : IPackage
	{
		public readonly PackageType Type => PackageType.PingResponse;

		public PackageFlags Flags => PackageFlags.None;

		public readonly int Size => sizeof(int) + sizeof(long);

		public int SequanceNumber;
		public long ServerTimestamp;

		public PingResponsePackage(int sequanceNumber, long timestamp)
		{
			SequanceNumber = sequanceNumber;
			ServerTimestamp = timestamp;
		}

		public void Serialize(ref byte[] buffer, int offset)
		{
			SequanceNumber.Convert(ref buffer, offset);
			ServerTimestamp.Convert(ref buffer, offset + sizeof(int));
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			SequanceNumber = BitConverter.ToInt32(buffer, offset);
			ServerTimestamp = BitConverter.ToInt64(buffer, offset + sizeof(int));
		}
	}
}
