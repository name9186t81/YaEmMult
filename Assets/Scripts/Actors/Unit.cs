using Mechanics;

using System;

using UnityEngine;

namespace Actors
{
	[RequireComponent(typeof(Rigidbody2D))]
	public sealed class Unit : MonoBehaviour, IActor, IVelocityProvider, IDirectionProvider
	{
		[SerializeField] private IActor.ControllerSwitchFlags _controllerSwitchFlags;
		[SerializeField] private float _maxSpeed;
		[SerializeField] private float _maxRotationSpeed;

		private IController _controller;
		private Motor _motor;
		private float _angularVelocity;
		private Rigidbody2D _body;

		public IActor.ControllerSwitchFlags ControllerSwitchCondition => _controllerSwitchFlags;

		public Vector2 Position => transform.position;

		public IController Controller => _controller;

		public float Size => 1f;
			
		public Vector2 Velocity { get => _body.velocity; set => _body.velocity = value; }
		public float AngularVelocity { get => _angularVelocity; set => _angularVelocity = value; }

		public Vector2 DesiredDirection => Controller.WalkDirection;

		public float DesiredRotation => Mathf.Atan2(Controller.LookDirection.y, Controller.LookDirection.x);

		public float Angle => transform.eulerAngles.z * Mathf.Deg2Rad;

		public event Action<IController, IController> OnControllerChanged;
		public event Action<IController.ControllerAction> OnAction;

		private void Awake()
		{
			_body = GetComponent<Rigidbody2D>();
			_motor = new Motor(this, this, _maxSpeed, 0.6f, _maxRotationSpeed, 0.9f, 0.9f);
		}

		private void FixedUpdate()
		{
			_motor.Update(Time.deltaTime);
			transform.rotation = Quaternion.Euler(0, 0, transform.eulerAngles.z + _angularVelocity);
		}

		public bool TryChangeController(IController controller)
		{
			bool canSwitch = CanSwitchTo(controller);
			if(_controller != null)
			{
				if ((_controllerSwitchFlags & IActor.ControllerSwitchFlags.SwitchAnyTime) != 0 && canSwitch)
				{
					if (_controller != null)
					{
						_controller.OnAction -= ReadAction;
					}
					var oldC = _controller;
					_controller = controller;
					if(_controller != null)
					{
						_controller.OnAction += ReadAction;
					}
					OnControllerChanged?.Invoke(oldC, _controller);
					return true;
				}

				return false;
			}

			if (canSwitch)
			{
				if (_controller != null)
				{
					_controller.OnAction -= ReadAction;
				}
				var oldC = _controller;
				_controller = controller;
				if (_controller != null)
				{
					_controller.OnAction += ReadAction;
				}
				OnControllerChanged?.Invoke(oldC, _controller);
				return true;
			}

			return false;
		}

		private void ReadAction(IController.ControllerAction obj)
		{
			OnAction?.Invoke(obj);
		}

		private bool CanSwitchTo(IController controller)
		{
			switch (controller.SelfType)
			{
				case IController.Type.Net: return (_controllerSwitchFlags & IActor.ControllerSwitchFlags.SwitchToNet) != 0;
				case IController.Type.AI: return (_controllerSwitchFlags & IActor.ControllerSwitchFlags.SwitchToAI) != 0;
				case IController.Type.Player: return (_controllerSwitchFlags & IActor.ControllerSwitchFlags.SwitchToPlayer) != 0;
			}
			return false;
		}

		public bool TryGetActorComponent<T>(out T comp) where T : IActorComponent
		{
			throw new NotImplementedException();
		}
	}
}
