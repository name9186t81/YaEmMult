using UnityEngine;

namespace Mechanics
{
	[StaticPreProcessor]
	public sealed class IgnoreSameTeamPreProcessor : IProjectilePreHitProcessor
	{
		public ProjectilePreHitProcessorType Type => ProjectilePreHitProcessorType.IgnoreSameTeam;

		public void Process(Projectile projectile, in RaycastHit2D hit)
		{
			if(hit.transform.TryGetComponent<ITeamProvider>(out var teamprovider)
				&& teamprovider.Team == projectile.Team)
			{
				projectile.IgnoreCollider(hit.collider);
				projectile.IgnoreCurrentHit();
			}
		}
	}
}
