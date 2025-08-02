using System;

using UnityEngine;

namespace Networking
{
	[Package(PackageFlags.None, PackageType.ActorSync)]
	public struct ActorSyncPackage : IPackage
	{
		public readonly PackageFlags Flags => PackageFlags.None;

		public readonly PackageType Type => PackageType.ActorSync;

		public readonly int Size => sizeof(float) * 3;

		public Vector2 Position;
		public float Rotation;

		public ActorSyncPackage(Vector2 position, float rotation)
		{
			Position = position;
			Rotation = rotation;
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			Position = NetworkUtils.GetVector2FromBuffer(buffer, offset);
			Rotation = BitConverter.ToSingle(buffer, offset + sizeof(float) * 2);
		}

		public readonly void Serialize(ref byte[] buffer, int offset)
		{
			Position.AddVector2ToBuffer(buffer, offset);
			BitConverter.SingleToInt32Bits(Rotation).Convert(ref buffer, offset + sizeof(float) * 2);
		}
	}
}
