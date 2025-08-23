using UnityEngine;

namespace Mechanics
{
	[CreateAssetMenu(fileName = "Projectile Profile", menuName = "YaEm/ProjectileProfile")]
	public sealed class ProjectileProfile : ScriptableObject
	{
		public enum HitInteraction
		{
			Destroy,
			Penetrate,
			Reflect
		}

		[SerializeField] private HitInteraction _defaultInteraction;
		[SerializeField] private int _maxPenetrationTimes;
		[SerializeField] private int _maxReflectTimes;
		[SerializeField] private ProjectilePreHitProcessorType[] _activePreProcessors;

		public ProjectilePreHitProcessorType[] ActivePreProcessors => _activePreProcessors;
		public HitInteraction Interaction => _defaultInteraction;
		public int MaxPenetrationTimes => _maxPenetrationTimes;
		public int MaxReflectTimes => _maxReflectTimes;
	}
}
