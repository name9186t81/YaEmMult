using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;

using YaEm;

namespace Mechanics
{
	[DisallowMultipleComponent]
	public class RangedWeapon : MonoBehaviour, IWeapon
	{
		[SerializeField] private float _inputBufferTime;
		[SerializeField] private RangedAttack[] _attacks;

		private Dictionary<RangedAttack, Pool<Projectile>> _mappedPools;
		private Dictionary<AttackKey, RangedAttack> _mappedAttacks;
		private WeaponState _state;

		public event Action OnStateChanged;
		public event Action<RangedAttack> OnAttackChange;
		public event Action<Projectile> OnProjectileShot;
		public event Action OnShot;

		private bool _wantToAttack;
		private float _attackRequestTime;
		private AttackKey _wantedKey;

		private bool _isAttacking;
		private RangedAttack _currentAttack;
		private float _totalElapsed;
		private float _currentStateDelta;

		private float _currentDelay;
		private int _currentProjectileIndex;
		private int _currentShotIndex;

		private void Awake()
		{
			MapAttacks();
			MapPools();
			_state = WeaponState.Idle;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void MapPools()
		{
			_mappedPools = new Dictionary<RangedAttack, Pool<Projectile>>();
			foreach (var attack in _attacks)
			{
				_mappedPools.Add(attack, new Pool<Projectile>(() =>
				{
					var obj = Instantiate(attack.Projectile);
					return obj;
				}));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void MapAttacks()
		{
			_mappedAttacks = new Dictionary<AttackKey, RangedAttack>();
			foreach (var attack in _attacks) 
			{
				if(!_mappedAttacks.TryAdd(attack.Key, attack))
				{
					Debug.LogWarning($"Attack mapping error: same attack key already being used: {attack.Key} weapon name: {gameObject.name} attack name: {attack.name}");
				}
			}
		}

		private void Update()
		{
#if UNITY_EDITOR
			if (Input.GetKey(KeyCode.LeftShift))
			{
				if (Input.GetKeyDown(KeyCode.S))
				{
					if (!TryAttack(_attacks[UnityEngine.Random.Range(0, _attacks.Length)].Key))
					{
						Debug.Log("Attack failed");
					}
				}
			}
#endif

			if (!(_isAttacking || _wantToAttack)) return;
			else if (_wantToAttack && !_isAttacking)
			{
				if((Time.time - _attackRequestTime) < _inputBufferTime)
				{
					TryAttack(_wantedKey);
				}
				_wantToAttack = false;
				return;
			}

			_totalElapsed += Time.deltaTime;
			switch (_state)
			{
				case WeaponState.Charging:
				{
					ChargeBehaviour();
					break;
				}
				case WeaponState.Attacking:
				{
					AttackBehaviour();
					break;
				}
				case WeaponState.Cooldown:
				{
					_currentStateDelta = Mathf.Clamp01((_totalElapsed - _currentAttack.TotalTime + _currentAttack.PostFireDelay) / _currentAttack.PostFireDelay);
					if (_totalElapsed > _currentAttack.TotalTime)
					{
						ProgressState();
					}
					break;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ChargeBehaviour()
		{
			_currentStateDelta = _totalElapsed / _currentAttack.PreFireDelay;

			if(_currentStateDelta > 1f)
			{
				ProgressState();
				_currentStateDelta = 0f;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AttackBehaviour()
		{
			float locElapsed = _totalElapsed - _currentAttack.PreFireDelay;
			_currentStateDelta = Mathf.Clamp01(locElapsed / (_currentAttack.PerProjectileDelay * _currentAttack.ProjectileCountPerShot + _currentAttack.PerShotDelay * _currentAttack.ShotsCount));

			if(_currentDelay > 0f)
			{
				_currentDelay -= Time.deltaTime;
				return;
			}

			if (_currentProjectileIndex == 0)
			{
				OnShot?.Invoke();
				_currentShotIndex++;
				if (_currentShotIndex > _currentAttack.ShotsCount)
				{
					ProgressState();
					_currentShotIndex = 0;
					_currentDelay = 0f;
					_currentProjectileIndex = 0;
					return;
				}
			}

			float timeLeft = Time.deltaTime;

			while (timeLeft > 0f)
			{
				_currentProjectileIndex++;
				if (_currentProjectileIndex > _currentAttack.ProjectileCountPerShot)
				{
					_currentDelay = _currentAttack.PerShotDelay;
					_currentProjectileIndex = 0;
					return;
				}

				float dIndex = Mathf.Clamp01((float)(_currentProjectileIndex - 1) / _currentAttack.ProjectileCountPerShot);
				float offset = (_currentAttack.AngleOffset * (dIndex - 0.5f) + _currentAttack.AngleOffsetPerShot * _currentShotIndex) * Mathf.Deg2Rad;
				float angle = MathUtils.AngleFromVector(transform.up);

				var proj = _mappedPools[_currentAttack].Get();
				proj.Init(new DamageArgs(), _mappedPools[_currentAttack], transform.position, MathUtils.Polar2Vector(angle + offset, 1), 0);
				OnProjectileShot?.Invoke(proj);
				timeLeft -= _currentAttack.PerProjectileDelay;
			}

			_currentDelay = _currentAttack.PerProjectileDelay + timeLeft - Time.deltaTime;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryAttack(AttackKey key)
		{
			if (_isAttacking)
			{
				_attackRequestTime = Time.time;
				_wantToAttack = true;
				_wantedKey = key;
				return false;
			}

			if(_mappedAttacks.TryGetValue(key, out var val))
			{
				ChangeAttack(val);
				return true;
			}

			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ChangeAttack(RangedAttack attack)
		{
			_currentAttack = attack;
			_totalElapsed = 0f;
			OnAttackChange?.Invoke(attack);
			_state = WeaponState.Idle;
			_isAttacking = true;
			ProgressState();
		}

		private void OnDestroy()
		{
			foreach(var pool in _mappedPools.Values)
			{
				pool.Destroy();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ProgressState()
		{
			switch (_state)
			{
				case WeaponState.Idle:
				{
					if (_currentAttack.PreFireDelay > 0f)
					{
						_state = WeaponState.Charging;
						OnStateChanged?.Invoke();
					}
					else
					{
						_state = WeaponState.Attacking;
						OnStateChanged?.Invoke();
					}
					break;
				}
				case WeaponState.Charging:
				{
					_state = WeaponState.Attacking;
					OnStateChanged?.Invoke();
					break;
				}
				case WeaponState.Attacking:
				{
					if (_currentAttack.PostFireDelay > 0f)
					{
						_state = WeaponState.Cooldown;
						OnStateChanged?.Invoke();
					}
					else
					{
						_state = WeaponState.Idle;
						OnStateChanged?.Invoke();
					}
					break;
				}
				case WeaponState.Cooldown:
				{
					_state = WeaponState.Idle;
					OnStateChanged?.Invoke();
					_totalElapsed = 0f;
					_isAttacking = false;
					break;
				}
			}
		}

		public WeaponState State => _state;
		public float TotalElapsedAttackTime => _totalElapsed;
		public float CurrentStateProgress => _currentStateDelta;
		public RangedAttack CurrentAttack => _currentAttack;
	}
}