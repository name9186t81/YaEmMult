using Core;

using System.Collections.Generic;
using System.Net;

using UnityEngine;

namespace Networking
{
	public class ServerDebugInfo : MonoBehaviour
	{
		[SerializeField] private string[] _ips;
		[SerializeField] private byte[] _ids;
		private void Update()
		{
			if(ServiceLocator.Get<ListenersCombiner>().Server != null)
			{
				var server = ServiceLocator.Get<ListenersCombiner>().Server;

				var users = server.ConnectedUsers;
				List<KeyValuePair<IPEndPoint, byte>> pairs = new List<KeyValuePair<IPEndPoint, byte>>();
				foreach (var user in users)
				{
					pairs.Add(user);
				}

				_ips = new string[pairs.Count];
				_ids = new byte[pairs.Count];

				for(int i = 0;  i < pairs.Count; i++)
				{
					_ids[i] = pairs[i].Value;
					_ips[i] = pairs[i].Key.ToString();
				}
			}
		}
	}
}