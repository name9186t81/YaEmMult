using System;

namespace Networking
{
	[Package(PackageFlags.NeedACK, PackageType.SnapshotRequest)]
	public struct SnapshotRequestPackage : IPackage
	{
		public PackageFlags Flags => PackageFlags.NeedACK;

		public PackageType Type => PackageType.SnapshotRequest;

		public int Size => sizeof(int) + sizeof(byte);

		public int NetworkObjectID;
		public byte UserID;

		/// <summary>
		/// Use -1 to request sync for all objects.
		/// </summary>
		/// <param name="networkObjectID"></param>
		public SnapshotRequestPackage(int networkObjectID = -1)
		{
			NetworkObjectID = networkObjectID;
			UserID = 0;
		}

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{
			NetworkObjectID = BitConverter.ToInt32(buffer.Slice(offset));
			UserID = buffer[offset + sizeof(int)];
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			throw new NotImplementedException();
		}

		public void Serialize(ref byte[] buffer, int offset)
		{
			NetworkObjectID.Convert(ref buffer, offset);
			buffer[offset + sizeof(int)] = UserID;
		}
	}
}
