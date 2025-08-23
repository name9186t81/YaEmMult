namespace Actors
{
	public interface IActorComponent
	{
		void Init(IActor actor);
		int Order { get; }
	}
}
