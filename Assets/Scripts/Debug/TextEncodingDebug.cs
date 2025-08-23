using System.Text;

using UnityEngine;

namespace Global
{
	public sealed class TextEncodingDebug : MonoBehaviour
	{
		[SerializeField] private TMPro.TextMeshProUGUI _text;

		private void Awake()
		{
			Debug.Log("Original message - " + _text.text + " length - " + _text.text.Length);
			Debug.Log("Encoded length - " + Encoding.UTF8.GetByteCount(_text.text));
			Debug.Log("Decoded string - " + Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(_text.text)));
			Debug.Log("Decoded string length - " + Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(_text.text)).Length);
		}
	}
}