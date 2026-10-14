using System.Collections.Generic;
using BallzGame.InventorySystem.ShopItems;
using BallzGame.Managers;
using UnityEngine;

namespace BallzGame.InventorySystem
{
	public class Inventory : MonoBehaviour
	{
		// Inventory 里当前拥有的 Item
		private List<ShopItem> items = new List<ShopItem>();

		public void Clear()
		{
			foreach (ShopItem item in items)
			{
				if (item == null)
					continue;

				while (item.Count > 0)
					item.OnRemoved();

				Destroy(item.gameObject);
			}
			items.Clear();
		}

		public void AddItemToInventory(ShopItem item)
		{
			if (item == null)
				return;

			// Type + Version 都相同才合并
			ShopItem existingItem = GetItem(
				item.GetType(),
				item.GetVersion()
			);

			if (existingItem != null)
			{
				existingItem.OnAdded();
				return;
			}

			// Type 或 Version 不同，创建一个新的
			ShopItem newItem = Instantiate(item, transform);

			newItem.Count = 0;
			newItem.OnAdded();

			items.Add(newItem);
		}

		public ShopItem GetItem(System.Type type, int version)
		{
			foreach (ShopItem item in items)
			{
				if (item.GetType() == type &&
				    item.GetVersion() == version)
				{
					return item;
				}
			}

			return null;
		}

		public T GetItem<T>(int version) where T : ShopItem
		{
			foreach (ShopItem item in items)
			{
				if (item is T &&
				    item.GetVersion() == version)
				{
					return (T)item;
				}
			}

			return null;
		}

		public bool HasItem<T>(int version) where T : ShopItem
		{
			return GetItem<T>(version) != null;
		}

		public void RemoveItem(ShopItem item)
		{
			if (item == null)
				return;

			ShopItem existingItem = GetItem(
				item.GetType(),
				item.GetVersion()
			);

			if (existingItem == null)
				return;

			existingItem.OnRemoved();

			// Count 到 0 后，从 Inventory 移除
			if (existingItem.Count <= 0)
			{
				items.Remove(existingItem);
				Destroy(existingItem.gameObject);
			}
		}
	}
}

namespace BallzGame.InventorySystem.ShopItems
{

	public abstract class ShopItem:MonoBehaviour
	{
		public abstract int GetVersion();
		public abstract bool Spawnable();
		public abstract void OnAdded();
		public abstract void OnRemoved();
		public string Name;
		public string Description;
		public Sprite Icon;
		public int Price;
		public int Count = 0;
		protected bool InventoryHasBelow(int amount)
		{
			var item = GameManager.Instance.inventory.GetItem(
				GetType(),
				GetVersion()
			);

			if (item != null && item.Count >= amount)
			{
				return false;
			}

			return true;
		}

	}
}
