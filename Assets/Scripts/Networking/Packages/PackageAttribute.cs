using System;

namespace Networking
{
	[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false)]
	public class PackageAttribute : Attribute
	{
		public PackageFlags Flags;
		public PackageType PackageType;

		public PackageAttribute(PackageFlags flags, PackageType packageType)
		{
			Flags = flags;
			PackageType = packageType;
		}
	}
}
