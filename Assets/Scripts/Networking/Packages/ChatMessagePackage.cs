using System;
using System.Text;

using UnityEngine;

namespace Networking
{
	[Package(PackageFlags.NeedACK, PackageType.ChatMessage)]
	public struct ChatMessagePackage : IPackage
	{
		public PackageFlags Flags => PackageFlags.NeedACK;

		public PackageType Type => PackageType.ChatMessage;

		public int Size => Message.Length + 1;

		public string Message;

		public ChatMessagePackage(string message)
		{
			Message = message;
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			char c = (char)buffer[offset++];
			Message = "";

			try
			{
				while (c != '\0')
				{
					Message += c;
					c = (char)buffer[offset++];
				}
			}
			catch(Exception ex)
			{
				Debug.LogException(ex);
			}
			Message += '\0';
		}

		public void Serialize(ref byte[] buffer, int offset)
		{
			for (int i = 0; i < Message.Length; i++)
			{
				buffer[offset + i] = (byte)Message[i];
			}
			buffer[offset + Message.Length] = 0;
		}

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{
			char c = (char)buffer[offset++];
			Message = "";

			try
			{
				while (c != '\0')
				{
					Message += c;
					c = (char)buffer[offset++];
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
			Message += '\0';
		}
	}
}
