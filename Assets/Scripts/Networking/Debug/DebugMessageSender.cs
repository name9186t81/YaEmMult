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
			_sendToServer.onClick.AddListener(() => { ServiceLocator.Get<ListenersCombiner>().Client.SendPackage(new ChatMessagePackage(_input.text)); });
			_sendToClient.onClick.AddListener(() => ServiceLocator.Get<ListenersCombiner>().Server.SendPackage(new ChatMessagePackage(_input.text), ListenerBase.PackageSendOrder.Instant, ListenerBase.PackageSendDestination.Everyone));
		}
	}
}
