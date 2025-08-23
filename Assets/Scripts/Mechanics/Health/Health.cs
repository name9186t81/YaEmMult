using System;

using UnityEngine;

namespace Mechanics
{
	public sealed class Health : IHealth
	{
		private float _maxHealth;
		private float _currentHealth;

		public Health(float maxHealth)
		{
			_maxHealth = _currentHealth = maxHealth;
		}

		public float MaxHealth => _maxHealth;

		public float CurrentHealth => _currentHealth;

		public event Action<DamageArgs> OnDeath;
		public event Action<DamageArgs> OnDamage;

		public void TakeDamage(DamageArgs args)
		{
			if(args.SelfType == DamageArgs.DamageType.Heal)
			{
				_currentHealth += args.Damage;
				_currentHealth = Mathf.Min(_maxHealth, _currentHealth);

				OnDamage?.Invoke(args);
				return;
			}

			_currentHealth -= args.Damage;
			if(_currentHealth < 0)
			{
				OnDeath?.Invoke(args);
				return;
			}
			OnDamage?.Invoke(args);
		}
	}
}
