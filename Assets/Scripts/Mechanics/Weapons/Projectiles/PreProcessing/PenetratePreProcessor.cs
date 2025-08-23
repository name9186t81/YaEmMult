using UnityEngine;

namespace Mechanics
{
	public sealed class PenetratePreProcessor : ILocalPreProcessor
	{
		private int _currentPenetrations;

		public ProjectilePreHitProcessorType Type => ProjectilePreHitProcessorType.Penetrate;

		public int Order => 1;

		public void Process(Projectile projectile, in RaycastHit2D hit)
		{
			_currentPenetrations++;

			if(_currentPenetrations > projectile.Profile.MaxPenetrationTimes)
			{
				return;
			}

			projectile.IgnoreCurrentHit();
			projectile.IgnoreCollider(hit.collider);
		}

		public void Init(Projectile projectile)
		{
			projectile.OnDestroy += Reset;
		}

		private void Reset()
		{
			_currentPenetrations = 0;
		}
	}
}
