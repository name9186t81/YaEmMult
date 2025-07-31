using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
	public interface IPackageProcessor
	{
		Task<bool> Process(byte[] data, CancellationTokenSource cts, IPEndPoint sender, Listener receiver);
	}
}
