using System;

using UnityEngine;

namespace Networking
{
	[Package(PackageFlags.CompressHigh, PackageType.ActorSyncFromServer)]
	public struct ActorSyncFromServerPackage : IPackage
	{
		public readonly PackageFlags Flags => PackageFlags.CompressHigh;

		public readonly PackageType Type => PackageType.ActorSyncFromServer;

		public readonly int Size => sizeof(float) * 3;

		public Vector2 Position;
		public float Rotation;
		public byte ID;

		public ActorSyncFromServerPackage(Vector2 position, float rotation, byte id)
		{
			Position = position;
			Rotation = rotation;
			ID = id;
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			Position = NetworkUtils.GetVector2FromBuffer(buffer, offset);
			Rotation = BitConverter.ToSingle(buffer, offset + sizeof(float) * 2);
			ID = buffer[offset + sizeof(float) * 3];
		}

		public readonly void Serialize(ref byte[] buffer, int offset)
		{
			Position.AddVector2ToBuffer(buffer, offset);
			BitConverter.SingleToInt32Bits(Rotation).Convert(ref buffer, offset + sizeof(float) * 2);
			buffer[sizeof(float) * 3 + offset] = ID;
		}
	}
}
