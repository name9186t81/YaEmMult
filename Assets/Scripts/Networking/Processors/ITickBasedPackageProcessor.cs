using System.Threading.Tasks;

namespace Networking
{
	internal interface ITickBasedPackageProcessor : IPackageProcessor
	{
		bool NeedTick { get; }
		Task Tick(float tickRate, Listener listener);
	}
}
