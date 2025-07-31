namespace Networking
{
	public interface IPackage
	{
		PackageFlags Flags { get; }
		PackageType Type { get; }

		/// <summary>
		/// The size of the package data NOT including the size of the package header and reliability.
		/// </summary>
		int Size { get; }

		void Serialize(ref byte[] buffer, int offset);
		void Deserialize(ref byte[] buffer, int offset);

		public bool NeedACK => (Flags & PackageFlags.NeedACK) != 0;
		public bool NeedCompress => (Flags & PackageFlags.Compress) != 0;
		public bool NeedHighCompress => (Flags & PackageFlags.CompressHigh) != 0;
		public bool IsReliable => (Flags  & PackageFlags.Reliable) != 0;
		public bool IsVeryReliable => (Flags & PackageFlags.VeryReliable) != 0;
	}
}
