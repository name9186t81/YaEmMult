using System;
using System.Drawing;

using UnityEngine;

namespace Networking
{
	[Package(PackageFlags.NeedACK, PackageType.NetworkObjectSpawn)]
	public struct NetworkObjectSpawnPackage : IPackage
	{
		public readonly PackageFlags Flags => PackageFlags.NeedACK;

		public readonly PackageType Type => PackageType.NetworkObjectSpawn;

		public readonly int Size => sizeof(int) * 2 + sizeof(byte) + GetSize();

		public NetworkMonoBehaviour.SyncSettings SyncFlags;
		public int ClientID;
		public int SpawnID;
		public Vector2 Position;
		public byte Rotation;
		public ushort DataLength;
		public byte[] CustomData;

		public NetworkObjectSpawnPackage(NetworkMonoBehaviour.SyncSettings flags, int iD, int spawnID, Vector2 position, byte rotation = 0, byte[] customData = null)
		{
			SyncFlags = flags;
			ClientID = iD;
			SpawnID = spawnID;
			Position = position;
			Rotation = rotation;
			CustomData = customData;
			DataLength = (ushort)(customData == null ? 0 : customData.Length);
		}

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{
			SyncFlags = (NetworkMonoBehaviour.SyncSettings)buffer[offset];
			ClientID = BitConverter.ToInt32(buffer.Slice(offset + sizeof(byte)));
			SpawnID = BitConverter.ToInt32(buffer.Slice(offset + sizeof(byte) + sizeof(int)));
			int baseOffset = sizeof(byte) + sizeof(int) * 2;

			int addOffset = 0;
			if ((SyncFlags & NetworkMonoBehaviour.SyncSettings.SyncPosition) != 0)
			{
				Position = NetworkUtils.GetVector2FromBuffer(buffer, offset + baseOffset);
				addOffset += sizeof(float) * 2;
			}
			if ((SyncFlags & NetworkMonoBehaviour.SyncSettings.SyncRotation) != 0)
			{
				Rotation = buffer[offset + baseOffset + addOffset];
				addOffset += sizeof(byte);
			}
			if ((SyncFlags & NetworkMonoBehaviour.SyncSettings.SyncCustomData) != 0)
			{
				DataLength = BitConverter.ToUInt16(buffer.Slice(offset + addOffset + baseOffset));
				int size = DataLength;
				if (size > 0)
				{
					byte[] data = new byte[size];
					for (int i = 0; i < data.Length; i++)
					{
						data[i] = buffer[offset + i + baseOffset + addOffset + sizeof(short)];
					}
				}
			}
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			throw new NotImplementedException();
		}

		public readonly void Serialize(ref byte[] buffer, int offset)
		{
			buffer[offset] = (byte)SyncFlags;
			Debug.Log("FLAGS - " + buffer[offset] + " OG FLAGS - " + SyncFlags);
			ClientID.Convert(ref buffer, offset + sizeof(byte));
			SpawnID.Convert(ref buffer, offset + sizeof(byte) + sizeof(int));
			int addOffset = 0;
			int baseOffset = sizeof(byte) + sizeof(int) * 2;

			if ((SyncFlags & NetworkMonoBehaviour.SyncSettings.SyncPosition) != 0)
			{
				Position.AddVector2ToBuffer(buffer, offset + baseOffset);
				addOffset += sizeof(float) * 2;
			}
			if ((SyncFlags & NetworkMonoBehaviour.SyncSettings.SyncRotation) != 0)
			{
				buffer[offset + baseOffset + addOffset] = Rotation;
				addOffset += sizeof(byte);
			}
			if ((SyncFlags & NetworkMonoBehaviour.SyncSettings.SyncCustomData) != 0)
			{
				DataLength.Convert(ref buffer, offset + baseOffset + addOffset);
				for(int i = 0; i < DataLength; i++)
				{
					buffer[offset + baseOffset + addOffset + sizeof(short) + i] = CustomData[i];
				}
			}
		}

		private readonly int GetSize()
		{
			int size = 0; 
			if ((SyncFlags & NetworkMonoBehaviour.SyncSettings.SyncPosition) != 0)
			{
				size += sizeof(float) * 2;
			}
			if ((SyncFlags & NetworkMonoBehaviour.SyncSettings.SyncRotation) != 0)
			{
				size += sizeof(byte);
			}
			if ((SyncFlags & NetworkMonoBehaviour.SyncSettings.SyncCustomData) != 0)
			{
				size += CustomData.Length;
				size += sizeof(short);
			}
			return size;
		}
	}
}
