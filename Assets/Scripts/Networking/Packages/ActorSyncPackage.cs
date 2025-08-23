using System;

using UnityEngine;

namespace Networking
{
	[Package(PackageFlags.None, PackageType.ActorSync)]
	public struct ActorSyncPackage : IPackage
	{
		public readonly PackageFlags Flags => PackageFlags.None;

		public readonly PackageType Type => PackageType.ActorSync;

		public readonly int Size => sizeof(float) * 3 + sizeof(int) * 2;

		public Vector2 Position;
		public float Rotation;
		public uint Tick;
		public int ID;

		public ActorSyncPackage(Vector2 position, float rotation, uint tick, int id)
		{
			Position = position;
			Rotation = rotation;
			Tick = tick;
			ID = id;
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			Position = NetworkUtils.GetVector2FromBuffer(buffer, offset);
			Rotation = BitConverter.ToSingle(buffer, offset + sizeof(float) * 2);
			Tick = (uint)BitConverter.ToInt32(buffer, offset + sizeof(float) * 3);
		}

		public readonly void Serialize(ref byte[] buffer, int offset)
		{
			Position.AddVector2ToBuffer(buffer, offset);
			BitConverter.SingleToInt32Bits(Rotation).Convert(ref buffer, offset + sizeof(float) * 2);
			Tick.Convert(ref buffer, offset + sizeof(float) * 3);
			ID.Convert(ref buffer, offset + sizeof(float) * 3 + sizeof(uint));
		}

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{
			Position = NetworkUtils.GetVector2FromBuffer(buffer, offset);
			Rotation = BitConverter.Int32BitsToSingle(BitConverter.ToInt32(buffer.Slice(offset + sizeof(float) * 2, sizeof(float))));
			Tick = (uint)BitConverter.ToInt32(buffer.Slice(offset + sizeof(float) * 3, sizeof(int)));
			ID = BitConverter.ToInt32(buffer.Slice(offset + sizeof(float) * 3 + sizeof(uint)));
		}
	}
}
