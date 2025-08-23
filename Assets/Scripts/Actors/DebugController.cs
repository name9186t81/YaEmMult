using System;

using UnityEngine;

namespace Actors
{
	public class DebugController : MonoBehaviour, IController
	{
		public IController.Type SelfType => IController.Type.Player;

		public Vector2 WalkDirection => _walkDirection;

		public Vector2 LookDirection => _lookDirection;

		public event Action<IController.ControllerAction> OnAction;

		private Vector2 _walkDirection;
		private Vector2 _lookDirection;

		private void Awake()
		{
			if (TryGetComponent<IActor>(out var actor))
			{
				if (!actor.TryChangeController(this))
				{
					Debug.LogError("Cannot change controller");
				}
			}
			else
			{
				Debug.LogError("Cannof find actor");
			}
		}

		private void Update()
		{
			_walkDirection = Vector2.zero;

			if (Input.GetKey(KeyCode.W))
			{
				_walkDirection += Vector2.up;
			}
			if (Input.GetKey(KeyCode.S))
			{
				_walkDirection += Vector2.down;
			}
			if (Input.GetKey(KeyCode.A))
			{
				_walkDirection += Vector2.left;
			}
			if (Input.GetKey(KeyCode.D))
			{
				_walkDirection += Vector2.right;
			}

			if (Input.GetMouseButton(0))
			{
				OnAction?.Invoke(IController.ControllerAction.Fire);
			}
			if (Input.GetMouseButton(1))
			{
				OnAction?.Invoke(IController.ControllerAction.SpecialFire1);
			}
			if (Input.GetMouseButton(2) || Input.GetKey(KeyCode.Z))
			{
				OnAction?.Invoke(IController.ControllerAction.SpecialFire2);
			}
			if (Input.GetKey(KeyCode.Q))
			{
				OnAction?.Invoke(IController.ControllerAction.SpecialFire3);
			}

			_walkDirection.Normalize();
			Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			_lookDirection = (mousePos - transform.position).normalized;
			Debug.DrawLine(transform.position, _lookDirection + (Vector2)transform.position);
		}
	}
}
