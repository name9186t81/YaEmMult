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

		public int Size 
		{ 
			get
			{
				Debug.Log("SIZE: " + (Encoding.UTF8.GetByteCount(Message) + 1) + " / " + (Encoding.UTF8.GetBytes(Message).Length + 1) + " Message: "  + Message);
				return Encoding.UTF8.GetByteCount(Message) + 1;
			} 
		}

		public byte Client;
		public string Message;

		public ChatMessagePackage(string message)
		{
			Client = 0;
			Message = message;

			Debug.Log(message);
			Debug.Log(Encoding.UTF8.GetByteCount(Message) + " " + Encoding.UTF8.GetBytes(Message).Length + " " + NetworkUtils.GetRealPackageSize(this) + " " + sizeof(int));
		}

		public void Deserialize(ref byte[] buffer, int offset)
		{
			Client = buffer[offset++];
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
			buffer[offset++] = Client;
			var code = Encoding.UTF8.GetBytes(Message);

			for (int i = 0; i < code.Length; i++)
			{
				buffer[offset + i] = code[i];
			}
		}

		public void Deserialize(ReadOnlySpan<byte> buffer, int offset)
		{
			Client = buffer[offset++];
			Message = Encoding.UTF8.GetString(buffer.Slice(offset));

			for (int i = 0; i < Message.Length; i++)
			{
				Debug.Log((byte)Message[i]);
			}
		}
	}
}
