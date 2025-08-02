using System;

using UnityEngine;

namespace Networking
{
	public sealed class ServerUpdater : MonoBehaviour
	{
		private float _rate = 1f;
		private float _elapsed;

		public static ServerUpdater Instance { get; private set; }
		public event Action OnTick;

		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
		}

		public static ServerUpdater Create()
		{
			if(Instance != null) return Instance;

			var obj = new GameObject();
			obj.name = "SERVER UPDATE DO NOT DESTROY";
			Instance = obj.AddComponent<ServerUpdater>();
			return Instance;
		}

		public void SetTickRate(int rate)
		{
			_rate = 1 / Mathf.Max(0.01f, rate);
		}

		private void Update()
		{
			_elapsed += Time.deltaTime;

			while(_elapsed > _rate)
			{
				OnTick?.Invoke();
				_elapsed -= _rate;
			}
		}
	}
}
