using UnityEngine;

namespace Mechanics
{
	public interface IDirectionProvider
	{
		Vector2 DesiredDirection { get; }
		float DesiredRotation { get; }
	}
}
