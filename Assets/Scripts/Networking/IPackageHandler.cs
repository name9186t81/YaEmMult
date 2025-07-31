namespace Networking
{
	public interface IPackageHandler
	{
		PackageType Type { get; }
		bool Handle(ref byte[] data);
	}
}
