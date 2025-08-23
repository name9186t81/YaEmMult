using UnityEngine;

namespace Mechanics
{
	public sealed class ReflectPreProcessor : ILocalPreProcessor
	{
		private int _currentReflects;

		public ProjectilePreHitProcessorType Type => ProjectilePreHitProcessorType.Penetrate;

		public int Order => 1;

		public void Process(Projectile projectile, in RaycastHit2D hit)
		{
			if (projectile.TempInvincible || projectile.IsIgnored(hit.collider)) return;

			_currentReflects++;

			if (_currentReflects > projectile.Profile.MaxReflectTimes)
			{
				return;
			}

			projectile.IgnoreCurrentHit();
			projectile.Direction = Vector2.Reflect(projectile.Direction, -hit.normal);
			projectile.Position = hit.point + projectile.Direction * Time.deltaTime;
		}

		public void Init(Projectile projectile)
		{
			projectile.OnDestroy += Reset;
		}

		private void Reset()
		{
			_currentReflects = 0;
		}
	}
}
