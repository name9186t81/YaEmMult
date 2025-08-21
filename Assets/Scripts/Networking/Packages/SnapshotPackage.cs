using System;

namespace Networking
{
	[Package(PackageFlags.NeedACK, PackageType.Snapshot)]
	public struct SnapshotPackage : IPackage
	{
		public PackageFlags Flags => PackageFlags.NeedACK;

		public PackageType Type => PackageType.Snapshot;

		public int Size => sizeof(int) + sizeof(short) + sizeof(byte) + Data.Length;

		public int NetworkID;
		public short DataSize;
		public byte Requester;
		public byte[] Data;

		public SnapshotPackage(int networkID, byte userID, byte[] data)
		{
			NetworkID = networkID;
			Requester = userID;
			Data = data;
			DataSize = (short)Data.Length;
		}

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{
			NetworkID = BitConverter.ToInt32(buffer.Slice(offset));
			DataSize = BitConverter.ToInt16(buffer.Slice(offset + sizeof(int)));
			Requester = buffer[sizeof(int) + sizeof(short) + offset];
			Data = new byte[DataSize];
			buffer.Slice(offset + sizeof(int) + sizeof(short) + sizeof(byte), DataSize).CopyTo(Data);
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			throw new NotImplementedException();
		}

		public void Serialize(ref byte[] buffer, int offset)
		{
			NetworkID.Convert(ref buffer, offset);
			DataSize.Convert(ref buffer, offset + sizeof(int));
			buffer[sizeof(int) + sizeof(short) + offset] = Requester;
			for (int i = 0; i < Data.Length; i++)
			{
				buffer[offset + i + sizeof(int) + sizeof(short) + sizeof(byte)] = Data[i];
			}
		}
	}
}
