using System;

namespace Networking
{
	[Package(PackageFlags.NeedACK, PackageType.TimeSyncResponse)]
	public struct TimeSyncResponse : IPackage
	{
		public PackageFlags Flags => PackageFlags.NeedACK;

		public PackageType Type => PackageType.TimeSyncResponse;

		public int Size => sizeof(long) * 2;

		public long ClientTimeStamp;
		public long ServerTimeStamp;

		public TimeSyncResponse(long clientTimeStamp, long serverTimeStamp)
		{
			ClientTimeStamp = clientTimeStamp;
			ServerTimeStamp = serverTimeStamp;
		}

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{
			ClientTimeStamp = BitConverter.ToInt64(buffer.Slice(offset));
			ServerTimeStamp = BitConverter.ToInt64(buffer.Slice(offset + sizeof(long)));
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			ClientTimeStamp = BitConverter.ToInt64(buffer, offset);
			ServerTimeStamp = BitConverter.ToInt64(buffer, offset + sizeof(long));
		}

		public void Serialize(ref byte[] buffer, int offset)
		{
			ClientTimeStamp.Convert(ref buffer, offset);
			ServerTimeStamp.Convert(ref buffer, offset + sizeof(long));
		}
	}
}
