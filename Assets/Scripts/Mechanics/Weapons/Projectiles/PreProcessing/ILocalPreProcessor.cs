namespace Mechanics
{
	public interface ILocalPreProcessor : IProjectilePreHitProcessor
	{
		int Order { get; }
		void Init(Projectile projectile);
	}
}
