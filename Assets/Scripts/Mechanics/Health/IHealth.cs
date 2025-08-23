using System;

namespace Mechanics
{
	public interface IHealth : IDamageReactable
	{
		float MaxHealth { get; }
		float CurrentHealth { get; }

		event Action<DamageArgs> OnDeath;
	}
}
