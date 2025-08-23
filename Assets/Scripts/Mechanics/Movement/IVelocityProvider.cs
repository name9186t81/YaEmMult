using UnityEngine;

namespace Mechanics
{
	public interface IVelocityProvider
	{
		Vector2 Position { get; }
		float Angle { get; }
		Vector2 Velocity { get; set; }
		float AngularVelocity { get; set; }
	}
}
