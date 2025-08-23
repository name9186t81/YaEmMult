using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
	[Package(PackageFlags.NeedACK, PackageType.ActorAction)]
	public struct ActorActionPackage : IPackage
	{
		public PackageFlags Flags => PackageFlags.NeedACK;

		public PackageType Type => PackageType.ActorAction;

		public byte Action;
		public byte IsActive;
		public int NetworkID;

		public ActorActionPackage(byte action, byte isActive, int networkID)
		{
			Action = action;
			IsActive = isActive;
			NetworkID = networkID;
		}

		public int Size => sizeof(byte) * 2 + sizeof(int);

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{
			Action = buffer[offset++];
			IsActive = buffer[offset++];
			NetworkID = BitConverter.ToInt32(buffer.Slice(offset));
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			throw new NotImplementedException();
		}

		public void Serialize(ref byte[] buffer, int offset)
		{
			buffer[offset++] = Action;
			buffer[offset++] = IsActive;
			NetworkID.Convert(ref buffer, offset);
		}
	}
}
