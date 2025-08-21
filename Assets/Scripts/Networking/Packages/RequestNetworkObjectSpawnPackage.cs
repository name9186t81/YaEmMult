using System;

namespace Networking
{
	[Package(PackageFlags.NeedACK, PackageType.NetworkObjectRequest)]
	public struct RequestNetworkObjectSpawnPackage : IPackage
	{
		public PackageFlags Flags => PackageFlags.NeedACK;

		public PackageType Type => PackageType.NetworkObjectRequest;

		public int Size => sizeof(int);

		public int NetworkID;

		/// <summary>
		/// Use -1 to get all network objects.
		/// </summary>
		/// <param name="networkID"></param>
		public RequestNetworkObjectSpawnPackage(int networkID = -1)
		{
			NetworkID = networkID;
		}

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{
			NetworkID = BitConverter.ToInt32(buffer.Slice(offset));
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			throw new NotImplementedException();
		}

		public void Serialize(ref byte[] buffer, int offset)
		{
			NetworkID.Convert(ref buffer, offset);
		}
	}
}
