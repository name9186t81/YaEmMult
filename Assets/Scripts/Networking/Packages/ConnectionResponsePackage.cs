using System;

namespace Networking
{
	[Package(PackageFlags.None, PackageType.ConnectionResponse)]
	public struct ConnectionResponsePackage : IPackage
	{
		public readonly PackageFlags Flags => PackageFlags.None;

		public readonly PackageType Type => PackageType.ConnectionResponse;

		public readonly int Size => sizeof(byte);

		public ConnectionResponseType Result;

		public ConnectionResponsePackage(ConnectionResponseType result)
		{
			Result = result;
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			Result = (ConnectionResponseType)buffer[offset];
		}

		public readonly void Serialize(ref byte[] buffer, int offset)
		{
			buffer[offset] = (byte)Result;
		}
	}
}
