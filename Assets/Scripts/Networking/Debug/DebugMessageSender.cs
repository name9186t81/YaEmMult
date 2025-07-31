using Core;

using UnityEngine;
using UnityEngine.UI;

namespace Networking
{
	public sealed class DebugMessageSender : MonoBehaviour
	{
		[SerializeField] private TMPro.TMP_InputField _input;
		[SerializeField] private Button _sendToServer;
		[SerializeField] private Button _sendToClient;

		private void Start()
		{
			_sendToServer.onClick.AddListener(() => { ServiceLocator.Get<ListenersCombiner>().Client.SendPackage(new ChatMessagePackage(_input.text), ServiceLocator.Get<ListenersCombiner>().Client.ServerEndPoint); Debug.LogWarning(ServiceLocator.Get<ListenersCombiner>().Client.ServerEndPoint); });
			_sendToClient.onClick.AddListener(() => ServiceLocator.Get<ListenersCombiner>().Server.SendPackage(new ChatMessagePackage(_input.text), ServiceLocator.Get<ListenersCombiner>().Server.Connected[0]));
		}
	}
}
