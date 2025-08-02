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
				_serverIP.text = "���� ����� ���� �����";
				return;
			}

			if (!short.TryParse(_serverPort.text, out var port))
			{
				_serverPort.text = "����� ����� ����";
				return;
			}
	
			var data = new ConnectionData(adress, port);
			NetworkingInfoContainer.Instance.UpdateConnectionData(ref data);
			Listener.Create(port, true);
			ConnectAsync(adress, port);
		}

		private async void ConnectAsync(IPAddress adress, int port)
		{
			await ServiceLocator.Get<ListenersCombiner>().Client.TryConnect(adress, port);
		}
		private void Confirm()
		{
			if (!(IPAddress.TryParse(_serverIP.text, out var adress) || NetworkUtils.TryParseSpecialIP(_serverIP.text, out adress)))
			{
				_serverIP.text = "���� ����� ���� �����";
				return;
			}

			if (!short.TryParse(_serverPort.text, out var port))
			{
				_serverPort.text = "����� ����� ����";
				return;
			}

			var data = new ConnectionData(adress, port);
			NetworkingInfoContainer.Instance.UpdateConnectionData(ref data);
			Listener.Create(port, false);
		}
	}
}