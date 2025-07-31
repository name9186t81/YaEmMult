using System;

namespace Networking
{
	[Package(PackageFlags.None, PackageType.Ack)]
	public struct ACKPackage : IPackage
	{
		public enum ACKResult : byte
		{
			Success = 0,
			Failed = 1,
			FailedCorrupted = 2
		}

		public readonly PackageFlags Flags => PackageFlags.None;

		public readonly PackageType Type => PackageType.Ack;

		public readonly int Size => sizeof(byte) + sizeof(int);

		public ACKResult Result;
		public int ID;

		public ACKPackage(ACKResult result, int id)
		{
			Result = result;
			ID = id;
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			Result = (ACKResult)buffer[offset];
			ID = BitConverter.ToInt32(buffer, offset + 1);
		}

		public readonly void Serialize(ref byte[] buffer, int offset)
		{
			((byte)Result).Convert(ref buffer, offset);
			ID.Convert(ref buffer, offset + 1);
		}
	}
}
