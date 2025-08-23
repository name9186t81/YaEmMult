using UnityEngine;

namespace Mechanics
{
	public interface IForce
	{
		public enum State
		{
			Active,
			Dead
		}

		State SelfState { get; }
		Vector2 Update(float dt);
		void Kill();
	}
}