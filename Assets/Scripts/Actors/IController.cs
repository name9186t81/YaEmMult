using System;

using UnityEngine;

namespace Actors
{
	public interface IController
	{
		public enum ControllerAction : byte
		{
			Fire = 0,
			SpecialFire1 = 1,
			SpecialFire2 = 2,
			SpecialFire3 = 3,
			SpecialFire4 = 4,
			SpecialFire5 = 5
		}

		public enum Type
		{
			Player,
			AI,
			Net
		}

		Type SelfType { get; }
		Vector2 WalkDirection { get; }
		Vector2 LookDirection { get; }
		event Action<ControllerAction> OnAction;
	}
}
