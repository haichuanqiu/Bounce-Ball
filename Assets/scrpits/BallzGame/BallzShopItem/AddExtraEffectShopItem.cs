using BallzGame.Balls;
using BallzGame.Managers;
using UnityEngine;

namespace BallzGame.InventorySystem.ShopItems
{
	public class AddExtraEffectShopItem: ShopItem
	{
		private BallExtraDamage ballExtraDamageController;
		[SerializeField] private BallExtraDamage.EffectType target;
		[SerializeField] private int increment;

		public override int GetVersion()
		{
			return (int)target;
		}

		public override bool Spawnable()
		{
			return InventoryHasBelow(maxAmount);
			;
		}

		public override void OnAdded()
		{
			if (Count == 0)
			{
				Init();
			}

			Count += 1;
			ballExtraDamageController.AddEffectValue(target, increment);
		}

		public override void OnRemoved()
		{
			Count -= 1;
			ballExtraDamageController.AddEffectValue(
				target,
				-increment
			);
		}

		public void Init()
		{
			ballExtraDamageController = GameManager.Instance.BallExtraDamageController;
		}
	}
}