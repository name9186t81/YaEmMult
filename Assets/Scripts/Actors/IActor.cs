using System;

using UnityEngine;

namespace Actors
{
	public interface IActor
	{
		[Flags]
		public enum ControllerSwitchFlags
		{
			None = 0,
			SwitchAnyTime = 1,
			SwitchToPlayer = 2,
			SwitchToAI = 4,
			SwitchToNet = 8,

			Everything = SwitchAnyTime | SwitchToPlayer | SwitchToAI | SwitchToNet
		}

		Vector2 WalkDirection
		{
			get
			{
				return Controller.WalkDirection;
			}
		}
		Vector2 LookDirection { get => Controller.LookDirection; }
		event Action<IController.ControllerAction> OnAction
		{
			add => Controller.OnAction += value;
			remove => Controller.OnAction -= value;
		}

		/// <summary>
		/// First controller is old one, and the second is new controller.
		/// </summary>
		event Action<IController, IController> OnControllerChanged;
		public bool TryChangeController(IController controller);
		public bool TryGetActorComponent<T>(out T comp) where T : IActorComponent;
		public ControllerSwitchFlags ControllerSwitchCondition { get; }
		Vector2 Position { get; }
		public IController Controller { get; }
		public float Size { get; }
	}
}