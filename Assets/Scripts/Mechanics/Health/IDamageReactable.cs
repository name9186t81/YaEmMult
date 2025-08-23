using System;

namespace Mechanics
{
	public interface IDamageReactable
	{
		void TakeDamage(DamageArgs args);
		event Action<DamageArgs> OnDamage;
	}
}
