using System;

namespace Networking
{
	[Package(PackageFlags.NeedACK, PackageType.ClientIDResponse)]
	public struct ClientIDPackageResponse : IPackage
	{
		public readonly PackageFlags Flags => PackageFlags.NeedACK;

		public readonly PackageType Type => PackageType.ClientIDResponse;

		public readonly int Size => sizeof(byte);

		public byte ID;

		public ClientIDPackageResponse(byte iD)
		{
			ID = iD;
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			ID = buffer[offset];
		}

		public readonly void Serialize(ref byte[] buffer, int offset)
		{
			buffer[offset] = ID;
		}

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{
			ID = buffer[offset];	
		}
	}
}
