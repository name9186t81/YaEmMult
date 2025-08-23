using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace Mechanics
{
	[DisallowMultipleComponent]
	public sealed class Projectile : MonoBehaviour
	{
		public enum State
		{
			InActive,
			Active,
			BeingDestroyed
		}

		[SerializeField] private float _timeToDestroy;
		[SerializeField] private LayerMask _hitMask;
		[SerializeField] private float _baseSpeed;
		[SerializeField] private float _baseLifeTime;
		[SerializeField] private ProjectileProfile _profile;

		public event Action OnInit;
		public event Action OnDestroy;
		public event Action<RaycastHit2D> OnHit;
		public event Action OnBeingDestroy;

		private ILocalPreProcessor[] _locals;
		private HashSet<Collider2D> _ignored = new HashSet<Collider2D>();
		private bool _tempInvincible;
		private int _team;
		private float _forcedLifeTime;
		private float _elapsedDestroy;
		private float _elapsedLifeTime;
		private float _speedModifier;
		private State _selfState;
		private Vector2 _direction;
		private Vector2? _forcedPosition;
		private Transform _cached;
		private Vector2 _prevPosition;

		private Pool<Projectile> _pool;
		private DamageArgs _args;

		private void Awake()
		{
			_cached = transform;
			_locals = GlobalProjectilePreProcessor.GetLocalPreHitProcessors(this);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ForceLifeTime(float time)
		{
			_forcedLifeTime = time;
		}

		public void Init(DamageArgs args, Pool<Projectile> pool, Vector2 position, Vector2 direction, int team)
		{
			_args = args;
			_args.SetSource(DamageArgs.SourceType.Projectile, this);

			if(_pool != null)
			{
				_pool.OnDestroy -= PoolDestroyed;
			}
			_pool = pool;
			if(_pool != null)
			{
				_pool.OnDestroy += PoolDestroyed;
			}
			_team = team;
			_ignored.Clear();
			gameObject.SetActive(true);

			_elapsedDestroy = 0;
			Position = position;
			_direction = direction;
			_selfState = State.Active;
			OnInit?.Invoke();
		}

		private void PoolDestroyed()
		{
			Debug.Log("Destroyed");
			_pool = null;
		}

		public void Destroy(bool instantly)
		{
			if (_selfState == State.InActive) return;

			if (instantly)
			{
				DestroyInternal();
			}
			else
			{
				OnBeingDestroy?.Invoke();
				_selfState = State.BeingDestroyed;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IgnoreCurrentHit()
		{
			_tempInvincible = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IgnoreCollider(Collider2D collider)
		{
			_ignored.Add(collider);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsIgnored(Collider2D collider)
		{
			return _ignored.Contains(collider);
		}

		private void Update()
		{
			_prevPosition = _cached.position;
		}

		private void LateUpdate()
		{
			if (_selfState == State.InActive) return;
			else if(_selfState == State.BeingDestroyed)
			{
				_elapsedDestroy += Time.deltaTime;

				if(_elapsedDestroy > _timeToDestroy)
				{
					DestroyInternal();
				}

				return;
			}

			_elapsedLifeTime += Time.deltaTime;
			if(_elapsedLifeTime > (_forcedLifeTime > Mathf.Epsilon ? _forcedLifeTime : _baseLifeTime))
			{
				DestroyInternal();
				return;
			}

			Vector2 position = _cached.position;

			float dU = TotalSpeed * Time.deltaTime;
			position += _direction * dU;

			var ray = Physics2D.Raycast(_prevPosition, _direction, dU, _hitMask);

			if (Trace(dU, out var hit))
			{
				var value = hit.Value;

				if (IsValidHit(value))
				{
					PreProcess(value);

					if (!_tempInvincible)
					{
						OnHit?.Invoke(value);
						position = value.point;
						_selfState = State.BeingDestroyed;
						OnBeingDestroy?.Invoke();

						if(value.transform.TryGetComponent<IDamageReactable>(out var reactable))
						{
							_args.UpdateHitPosition(value.point, value.normal);
							reactable.TakeDamage(_args);
						}
					}
					else
					{
						_tempInvincible = false;
					}
				}
			}

			if (_forcedPosition.HasValue)
			{
				_cached.position = _prevPosition = _forcedPosition.Value;
				_forcedPosition = null;
			}
			else
			{
				_cached.position = position;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void PreProcess(in RaycastHit2D hit)
		{
			GlobalProjectilePreProcessor.ProcessProjectile(this, hit);

			for (int i = 0; i < _locals.Length; i++)
			{
				_locals[i].Process(this, hit);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsValidHit(in RaycastHit2D hit)
		{
			return !_ignored.Contains(hit.collider);
		}

		private bool Trace(float dU, out RaycastHit2D? hit)
		{
			var ray = Physics2D.Raycast(_prevPosition, _direction, dU, _hitMask);

			if (ray)
			{
				hit = ray;
				return true;
			}

			hit = null;
			return false;
		}

		private void DestroyInternal()
		{
			OnDestroy?.Invoke();
			_selfState = State.InActive;
			_elapsedLifeTime = 0f;
			_forcedLifeTime = 0f;
			_elapsedDestroy = 0;

			if(_pool != null)
			{
				_pool.ReturnToPool(this);
				gameObject.SetActive(false);
			}
			else
			{
				MonoBehaviour.Destroy(gameObject);
			}
		}

		private void OnValidate()
		{
			_timeToDestroy = Mathf.Abs(_timeToDestroy);
			_baseSpeed = Mathf.Abs(_baseSpeed);
		}

		public bool TempInvincible => _tempInvincible;
		public ProjectileProfile Profile => _profile;
		public int Team => _team;
		public State SelfState => _selfState;
		public float BaseSpeed => _baseSpeed;
		public float TotalSpeed => _baseSpeed + _speedModifier;
		public float DestroyTime => _timeToDestroy;
		public float ElapsedDestroy => _elapsedDestroy;
		public float LifeTime => _forcedLifeTime > Mathf.Epsilon ? _forcedLifeTime : _baseLifeTime;
		public float ElapsedLifeTime => _elapsedLifeTime;

		public float SpeedModifier
		{
			get => _speedModifier;
			set => _speedModifier = value;
		}

		public Vector2 Position
		{
			get => _cached.position;
			set => _forcedPosition = value;
		}

		public Vector2 Direction
		{
			get => _direction;
			set => _direction = value.normalized;
		}
	}
}
