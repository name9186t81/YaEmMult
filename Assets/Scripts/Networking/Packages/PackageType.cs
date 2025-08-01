namespace Networking
{
	public enum PackageType
	{
		Invalid,
		ConnectionRequest,
		ConnectionResponse,
		ClientShutdown,
		ClientShutdownNotification,
		ChatMessage,
		Ping,
		PingResponse,
		ServerFind,
		ServerFindResponse,
		ServerShutdown,
		TimeSync,
		ActorSync,
		ActorSyncFromServer,
		Ack,
		ClientIDRequest,
		ClientIDResponse,
	}
}