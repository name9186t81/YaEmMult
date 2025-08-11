using System;

namespace Networking
{
	[Package(PackageFlags.NeedACK, PackageType.ConnectionRequest)]
	public struct ConnectionRequestPackage : IPackage
	{
		public readonly PackageFlags Flags => PackageFlags.NeedACK;

		public readonly PackageType Type => PackageType.ConnectionRequest;

		public readonly int Size => sizeof(long);

		public long IP;

		public ConnectionRequestPackage(long iP)
		{
			IP = iP;
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			IP = BitConverter.ToInt64(buffer, offset);
		}

		public readonly void Serialize(ref byte[] buffer, int offset)
		{
			IP.Convert(ref buffer, offset);
		}

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{
			IP = BitConverter.ToInt64(buffer.Slice(offset, sizeof(int)));
		}
	}
}
