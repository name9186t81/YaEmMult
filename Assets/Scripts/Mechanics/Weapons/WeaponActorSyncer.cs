using Actors;

using UnityEngine;

namespace Mechanics
{
	public sealed class WeaponActorSyncer : MonoBehaviour
	{
		[SerializeField] private Unit _unit;
		[SerializeField] private RangedWeapon _weapon;

		private void Start()
		{
			(_unit as IActor).OnAction += ReadAction;
		}

		private void ReadAction(IController.ControllerAction obj)
		{
			if (obj == IController.ControllerAction.Fire)
				_weapon.TryAttack(AttackKey.Fire1);
		}
	}
}
