using System.Collections.Generic;

using UnityEngine;

using YaEm;

namespace Mechanics
{
	public sealed class Motor
	{
		public enum Constrains
		{
			FreezeRotation = 1,
			FreezeMovement = 1 << 1
		}

		private readonly IVelocityProvider _provider;
		private readonly IDirectionProvider _directionProvider;

		private readonly float _maxSpeed;
		private readonly float _acceleration;

		private readonly float _maxRotationSpeed;

		private readonly float _rotationDrag;
		private readonly float _linearDrag;

		private List<IForce> _linearForces = new List<IForce>();
		private Vector2 _directionVelocity;
		private float _angularDirectionVelocity;
		private float _angularImpulse;
		private Vector2 _linearImpulse;
		private Constrains _constrains;
		private bool _isFrozen;

		public Motor(IVelocityProvider prov, IDirectionProvider directionProvider, float maxSpeed, float acceleration, float maxRotationSpeed, float rotationDrag, float linearDrag)
		{
			_provider = prov;
			_directionProvider = directionProvider;
			_maxSpeed = Mathf.Max(0, maxSpeed);
			_acceleration = Mathf.Clamp01(acceleration);
			_maxRotationSpeed = Mathf.Max(0, maxRotationSpeed);
			_rotationDrag = Mathf.Clamp01(rotationDrag);
			_linearDrag = Mathf.Clamp01(linearDrag);
		}

		public void Update(float dt)
		{
			if (_isFrozen) return;

			if (CanMove)
			{
				UpdateMovement(dt);
			}
			if (CanRotate)
			{
				UpdateRotation(dt);
			}
		}

		private void UpdateMovement(float dt)
		{
			Vector2 result = Vector2.zero;
			_linearImpulse *= _linearDrag;
			result += _linearImpulse;

			for (int i = 0; i < _linearForces.Count; i++)
			{
				var force =_linearForces[i];

				if(force.SelfState == IForce.State.Dead)
				{
					_linearForces.RemoveAt(i);
					i = Mathf.Max(i - 1, 0);
					continue;
				}

				result += _linearForces[i].Update(dt);
			}

			_directionVelocity = Vector2.Lerp(_directionVelocity, _directionProvider.DesiredDirection * _maxSpeed, _acceleration);

			_provider.Velocity = result + _directionVelocity;
		}

		private void UpdateRotation(float dt)
		{
			float result = 0;
			result += _angularImpulse;
			_angularImpulse *= _rotationDrag;

			float desiredAnlge = _directionProvider.DesiredRotation * Mathf.Rad2Deg - 90;
			float angle = _provider.Angle * Mathf.Rad2Deg;
			float delta = Mathf.DeltaAngle(angle, desiredAnlge);
			//Debug.Log(desiredAnlge + " " + angle + " " + Mathf.DeltaAngle(angle, desiredAnlge) + " " + Mathf.Sign(delta) * Mathf.Min(Mathf.Abs(delta), (_maxRotationSpeed)));
			result += Mathf.Sign(delta) * Mathf.Min(Mathf.Abs(delta), (_maxRotationSpeed));

			float normalAngle = MathUtils.NormalizeAngle(_provider.Angle);
			float normalDesired = MathUtils.NormalizeAngle(_directionProvider.DesiredRotation);
			float normalVel = MathUtils.NormalizeAngle(_angularDirectionVelocity);

			float difference = Mathf.Max(normalDesired - normalAngle, normalAngle - normalDesired);
			float sign = Mathf.Sign(normalDesired - normalAngle);

			_provider.AngularVelocity = result + _angularDirectionVelocity;
		}

		public void AddImpulse(float angularImpulse)
		{
			_angularImpulse += angularImpulse;
		}

		public void AddImpulse(Vector2 impulse)
		{
			_linearImpulse += impulse;
		}

		public void Freeze()
		{
			_isFrozen = true;
		}

		public void UnFreeze()
		{
			_isFrozen = false;
		}

		public Vector2 Velocity => _provider.Velocity;
		public float AngularVelocity => _provider.AngularVelocity;
		public bool CanMove => !(_isFrozen || (_constrains & Constrains.FreezeMovement) != 0);
		public bool CanRotate => !(_isFrozen || (_constrains & Constrains.FreezeRotation) != 0);

		public Constrains CurrentConstrains
		{
			get => _constrains;
			set => _constrains = value;
		}
	}
}