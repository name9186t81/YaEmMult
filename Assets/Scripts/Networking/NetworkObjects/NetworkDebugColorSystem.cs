using System;

using UnityEngine;

namespace Networking
{
	public class NetworkDebugColorSystem : INetworkBehaviourSystem
	{
		public INetworkBehaviourSystem.HeaderSize MaxSize => INetworkBehaviourSystem.HeaderSize.Byte;

		public int GetSizeFor(NetworkMonoBehaviour networkBehaviour)
		{
			if (networkBehaviour.TryGetComponent<SpriteRenderer>(out _)) return sizeof(int);
			return 0;
		}

		public bool TryProcessShapshot(NetworkMonoBehaviour behaviour, int offset, int length, byte[] buffer)
		{
			if (behaviour.TryGetComponent<SpriteRenderer>(out var renderer))
			{
				renderer.color = Color.HSVToRGB(BitConverter.Int32BitsToSingle(BitConverter.ToInt32(buffer, offset)), 1, 1);
				return true;
			}
			return false;
		}

		public bool TryTakeShapshot(NetworkMonoBehaviour behaviour, int offset, byte[] buffer)
		{
			if(behaviour.TryGetComponent<SpriteRenderer>(out var renderer))
			{
				Color.RGBToHSV(renderer.color, out var h, out var s, out var v);
				BitConverter.SingleToInt32Bits(h).Convert(ref buffer, offset);
				return true;
			}
			return false;
		}
	}
}
