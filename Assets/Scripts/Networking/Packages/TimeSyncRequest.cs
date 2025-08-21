using System;

namespace Networking
{
	[Package(PackageFlags.NeedACK, PackageType.TimeSync)]
	public struct TimeSyncRequest : IPackage
	{
		public readonly PackageFlags Flags => PackageFlags.NeedACK;

		public readonly PackageType Type => PackageType.TimeSync;

		public int Size => sizeof(long);

		public long ClientStamp;

		public TimeSyncRequest(long clientStamp)
		{
			ClientStamp = clientStamp;
		}

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{
			ClientStamp = BitConverter.ToInt64(buffer.Slice(offset));
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			ClientStamp = BitConverter.ToInt64(buffer, offset);
		}

		public readonly void Serialize(ref byte[] buffer, int offset)
		{
			ClientStamp.Convert(ref buffer, offset);
		}
	}
}
