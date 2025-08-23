using UnityEngine;

namespace Mechanics
{
	public interface IProjectilePreHitProcessor
	{
		ProjectilePreHitProcessorType Type { get; }
		void Process(Projectile projectile, in RaycastHit2D hit);
	}
}
