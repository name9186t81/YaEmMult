using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
	[Package(PackageFlags.NeedACK, PackageType.NetworkObjectIDAssignement)]
	public struct NetworkObjectIDAssignmentPackage : IPackage
	{
		public readonly PackageFlags Flags => PackageFlags.NeedACK;

		public readonly PackageType Type => PackageType.NetworkObjectIDAssignement;

		public readonly int Size => sizeof(int) * 2;

		public int ClientID;
		public int ServerID;

		public NetworkObjectIDAssignmentPackage(int clientID, int serverID)
		{
			ClientID = clientID;
			ServerID = serverID;
		}

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{
			ClientID = BitConverter.ToInt32(buffer.Slice(offset));
			ServerID = BitConverter.ToInt32(buffer.Slice(offset + sizeof(int)));
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			throw new NotImplementedException();
		}

		public void Serialize(ref byte[] buffer, int offset)
		{
			ClientID.Convert(ref buffer, offset);
			ServerID.Convert(ref buffer, offset + sizeof(int));
		}
	}
}
