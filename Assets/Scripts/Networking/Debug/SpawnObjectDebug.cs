using UnityEngine;
using UnityEngine.UI;

namespace Networking
{
	public sealed class SpawnObjectDebug : MonoBehaviour
	{
		[SerializeField] private Button _button;
		[SerializeField] private NetworkMonoBehaviour _prefab;

		private void Awake()
		{
			_button.onClick.AddListener(() =>
			{
				Vector2 randomPos = new Vector2(Random.Range(-10f, 10f), Random.Range(-10f, 10f));
				float randomRotation = Random.Range(0, 360);
				NetworkManager.Instance.TryToSpawn(_prefab, randomPos, randomRotation, out var obj);

				if(obj.TryGetComponent<SpriteRenderer>(out var renderer))
				{
					renderer.color = Color.HSVToRGB(Random.Range(0, 1f), 1, 1);
				}
			});
		}
	}
}
