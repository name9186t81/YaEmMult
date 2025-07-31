namespace Networking
{
	public enum ConnectionResponseType : byte
	{
		Forbidden = 0,
		Dropped = 1,
		Success = 2,
		AlreadyConnected = 3
	}
}
