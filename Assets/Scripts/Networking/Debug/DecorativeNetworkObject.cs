using System.Collections.Generic;

namespace Networking
{
	public class DecorativeNetworkObject : NetworkMonoBehaviour
	{
		public override IEnumerable<int> GetAllRequiredSystemsForSnapshot()
		{
			if(NetworkBehaviourSynchronizer.TryGetSystemID<NetworkDebugColorSystem>(out var sys)) return new int[] { sys };
			return base.GetAllRequiredSystemsForSnapshot();
		}
	}
}