using System;

namespace Mechanics
{
	public interface ITeamProvider
	{
		event Action<int> OnTeamChange;
		int Team { get; }
		bool TryChangeTeam(int newTeam);
	}
}
