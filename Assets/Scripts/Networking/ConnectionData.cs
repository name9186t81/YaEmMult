using System.Net;

namespace Networking
{
	public readonly struct ConnectionData
	{
		public readonly IPAddress IP;
		public readonly short Port;

		public ConnectionData(IPAddress iP, short port)
		{
			IP = iP;
			Port = port;
		}
	}
}