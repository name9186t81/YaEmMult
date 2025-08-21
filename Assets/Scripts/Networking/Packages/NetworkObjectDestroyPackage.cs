using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
	[Package(PackageFlags.NeedACK, PackageType.NetworkObjectDestroy)]
	public struct NetworkObjectDestroyPackage : IPackage
	{
		public PackageFlags Flags => PackageFlags.NeedACK;

		public PackageType Type => PackageType.NetworkObjectDestroy;

		public int Size => sizeof(int);

		public int ID;

		public NetworkObjectDestroyPackage(int iD)
		{
			ID = iD;
		}

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{
			ID = BitConverter.ToInt32(buffer.Slice(offset));
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			throw new NotImplementedException();
		}

		public void Serialize(ref byte[] buffer, int offset)
		{
			ID.Convert(ref buffer, offset);
		}
	}
}
