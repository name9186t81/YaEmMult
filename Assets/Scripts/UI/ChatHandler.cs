using Core;

using Networking;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public sealed class ChatHandler : MonoBehaviour
	{
		[SerializeField] private TMPro.TMP_InputField _input;
		[SerializeField] private Button _sendButton;
		[SerializeField] private TMPro.TMP_Text _text;

		private ConcurrentQueue<string> _pendingMessages;
		private const int MAX_MESSAGES = 10;
		private string[] _messages;

		private void Awake()
		{
			_messages = new string[MAX_MESSAGES];
			_input.onSubmit.AddListener((string text) =>
			{
				if(ServiceLocator.Get<ListenersCombiner>().Client != null)
				{
					ServiceLocator.Get<ListenersCombiner>().Client.SendPackageToServerAsync(
						new ChatMessagePackage(text), ListenerBase.PackageSendOrder.NextTick);
				}
				_input.text = "";
			});
			_sendButton.onClick.AddListener(() =>
			{
				if (ServiceLocator.Get<ListenersCombiner>().Client != null)
				{
					ServiceLocator.Get<ListenersCombiner>().Client.SendPackageToServerAsync(
						new ChatMessagePackage(_input.text), ListenerBase.PackageSendOrder.NextTick);
				}
				_input.text = "";
			});
			_pendingMessages = new ConcurrentQueue<string>();

			ServiceLocator.Get<ChatMessageListener>().OnMessage += AddMesssage;
		}

		private void Update()
		{
			while(_pendingMessages.TryDequeue(out string message))
			{
				for (int i = MAX_MESSAGES - 1; i > 0; i--)
				{
					_messages[i] = _messages[i - 1];
				}
				_messages[0] = message;
				UpdateText();
			}
		}

		private void UpdateText()
		{
			var messages = _messages.
				Where(t => !string.IsNullOrEmpty(t))
				.Reverse()
				.ToArray();

			_text.text = string.Join(Environment.NewLine, messages);
		}

		private void AddMesssage(byte arg1, string arg2)
		{
			_pendingMessages.Enqueue(FormatString(arg1, arg2));
		}

		private string FormatString(byte client, string message)
		{
			if(ServiceLocator.Get<ListenersCombiner>().Client != null)
				return ((client == ServiceLocator.Get<ListenersCombiner>().Client.ID) ? "рш" : client.ToString()) + ":" + message;
			else
				return $"{client}:" + message;
		}
	}
}