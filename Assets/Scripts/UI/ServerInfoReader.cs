using Core;

using Networking;

using System.Net;

using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public sealed class ServerInfoReader : MonoBehaviour
	{
		[SerializeField] private TMPro.TMP_InputField _serverIP;
		[SerializeField] private TMPro.TMP_InputField _serverPort;
		[SerializeField] private Button _confirmButton;
		[SerializeField] private Button _connectButton;

		private void Start()
		{
			_confirmButton.onClick.AddListener(Confirm);
			_connectButton.onClick.AddListener(Connect);
		}

		private void Connect()
		{
			if (!(IPAddress.TryParse(_serverIP.text, out var adress) || NetworkUtils.TryParseSpecialIP(_serverIP.text, out adress)))
			{
				_serverIP.text = "Айпи адрес пиши баран";
				return;
			}

			if (!short.TryParse(_serverPort.text, out var port))
			{
				_serverPort.text = "Баран число пиши";
				return;
			}
	
			var data = new ConnectionData(adress, port);
			NetworkingInfoContainer.Instance.UpdateConnectionData(ref data);
			ServiceLocator.Get<ListenersCombiner>().Client = new DebugClient();
			ConnectAsync(adress, port);
		}

		private async void ConnectAsync(IPAddress adress, int port)
		{
			await ServiceLocator.Get<ListenersCombiner>().Client.TryToConnect(new IPEndPoint(adress, port));
		}

		private void Confirm()
		{
			if (!(IPAddress.TryParse(_serverIP.text, out var adress) || NetworkUtils.TryParseSpecialIP(_serverIP.text, out adress)))
			{
				_serverIP.text = "Айпи адрес пиши баран";
				return;
			}

			if (!short.TryParse(_serverPort.text, out var port))
			{
				_serverPort.text = "Баран число пиши";
				return;
			}

			var data = new ConnectionData(adress, port);
			NetworkingInfoContainer.Instance.UpdateConnectionData(ref data);
			ServiceLocator.Get<ListenersCombiner>().Server = new DebugServer(port, -1, -1, 20);
		}
	}
}