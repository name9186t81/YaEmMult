using System;

namespace Networking
{
	[Package(PackageFlags.NeedACK, PackageType.ClientIDRequest)]
	public readonly struct RequestClientIDPackage : IPackage
	{
		public readonly PackageFlags Flags => PackageFlags.NeedACK;

		public readonly PackageType Type => PackageType.ClientIDRequest;

		public readonly int Size => 0;

		public readonly void Deserialize(ref byte[] buffer, int offset)
		{
		}

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{

		}

		public readonly void Serialize(ref byte[] buffer, int offset) 
		{ 
		}
	}
}
