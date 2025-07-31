using System;

namespace Networking
{
	[Flags]
	public enum PackageFlags
	{
		None = 0,
		Compress = 1 << 0,
		CompressHigh = 1 << 1,
		
		Reliable = 1 << 2,
		VeryReliable = 1 << 3,

		NeedACK = 1 << 4
	}
}
