namespace Networking
{
	public interface INetworkBehaviourSystem
	{
		public enum HeaderSize
		{
			Byte = 1,
			Short = 2
		}

		/// <summary>
		/// The size of the header for storing length of data.
		/// </summary>
		HeaderSize MaxSize { get; }
		/// <summary>
		/// Used to get the size of data in bytes for concrete network object.
		/// </summary>
		/// <param name="networkBehaviour"></param>
		/// <returns></returns>
		int GetSizeFor(NetworkMonoBehaviour networkBehaviour);
		bool TryTakeShapshot(NetworkMonoBehaviour behaviour, int offset, byte[] buffer);
		bool TryProcessShapshot(NetworkMonoBehaviour behaviour, int offset, int length, byte[] buffer);
	}
}
