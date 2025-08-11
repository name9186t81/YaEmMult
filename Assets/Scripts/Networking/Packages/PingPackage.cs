using System;

namespace Networking
{
	[Package(PackageFlags.None, PackageType.Ping)]
	public struct PingPackage : IPackage
	{
		readonly PackageType IPackage.Type => PackageType.Ping;

		PackageFlags IPackage.Flags =>  PackageFlags.None;

		public int Size => sizeof(long) + sizeof(int);

		public int SequanceNumber;
		public long Timestamp;

		public PingPackage(int sequanceNumber, long timestamp)
		{
			SequanceNumber = sequanceNumber;
			Timestamp = timestamp;
		}

		public readonly void Serialize(ref byte[] buffer, int offset)
		{
			SequanceNumber.Convert(ref buffer, offset);
			Timestamp.Convert(ref buffer, offset + sizeof(int));
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			SequanceNumber = BitConverter.ToInt32(buffer, offset);
			Timestamp = BitConverter.ToInt64(buffer, offset + sizeof(int));
		}

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{
			SequanceNumber = BitConverter.ToInt32(buffer.Slice(offset, sizeof(int)));
			Timestamp = BitConverter.ToInt64(buffer.Slice(offset + sizeof(int), sizeof(long)));
		}
	}
}
