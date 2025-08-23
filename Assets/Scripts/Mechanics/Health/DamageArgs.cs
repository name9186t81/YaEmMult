using Actors;

using UnityEngine;

namespace Mechanics
{
	public struct DamageArgs
	{
		public enum DamageType
		{
			Kinetic,
			Explosive,
			Electric,
			Heal,
			Direct
		}

		public enum SourceType
		{
			Projectile,
			Melee,
			Unknown
		}

		public readonly IActor ActorSource;
		public readonly float Damage;
		public readonly DamageType SelfType;

		public Vector2 HitPosition;
		public Vector2 HitNormal;

		public object Source;
		public SourceType Type;

		public DamageArgs(IActor actorSource, float damage, DamageType selfType) : this()
		{
			ActorSource = actorSource;
			Damage = damage;
			SelfType = selfType;
		}

		public void UpdateHitPosition(in Vector2 hitPosition, in Vector2 hitNormal)
		{
			HitPosition = hitPosition;
			HitNormal = hitNormal;
		}

		public void SetSource(SourceType source, object sourceValue)
		{
			Source = sourceValue;
			Type = source;
		}
	}
}
