using UnityEngine;

namespace Mechanics
{
	[CreateAssetMenu(fileName = "Ranged Attack", menuName = "YaEm/RangedAttack")]
	public sealed class RangedAttack : ScriptableObject
	{
		[Header("Main")]
		[SerializeField] private AttackKey _key;
		[SerializeField] private Projectile _projectile;

		[Space]
		[Header("Timings")]
		[SerializeField] private float _preFireDelay;
		[SerializeField] private float _perProjectileDelay;
		[SerializeField] private float _perShotDelay;
		[SerializeField] private float _postFireDelay;

		[Space]
		[Header("Attack Settings")]
		[SerializeField] private int _projectileCountPerShot;
		[SerializeField] private float _angleOffsetPerShot;
		[SerializeField] private int _shotsCount;
		[SerializeField] private float _angleOffset;

#if UNITY_EDITOR
		private void OnValidate()
		{
			_preFireDelay = Mathf.Max(0f, _preFireDelay);
			_perProjectileDelay = Mathf.Max(0f, _perProjectileDelay);
			_perShotDelay = Mathf.Max(0f, _perShotDelay);
			_postFireDelay = Mathf.Max(0f, _postFireDelay);

			_projectileCountPerShot = Mathf.Max(0, _projectileCountPerShot);
			_angleOffsetPerShot = Mathf.Max(0, _angleOffsetPerShot);
			_shotsCount = Mathf.Max(1, _shotsCount);
			_angleOffset = Mathf.Max(0, _angleOffset);
		}
#endif

		public AttackKey Key => _key;
		public Projectile Projectile => _projectile;

		public float PreFireDelay => _preFireDelay;
		public float PerProjectileDelay => _perProjectileDelay;
		public float PerShotDelay => _perShotDelay;
		public float PostFireDelay => _postFireDelay;

		public int ProjectileCountPerShot => _projectileCountPerShot;
		public float AngleOffsetPerShot => _angleOffsetPerShot;
		public int ShotsCount => _shotsCount;
		public float AngleOffset => _angleOffset;

		public float TotalTime => _preFireDelay + _perProjectileDelay * Mathf.Max((_projectileCountPerShot - 1), 0) + _perShotDelay * (_shotsCount - 1) + _postFireDelay;
	}
}