using BallzGame.Balls;
using BallzGame.InventorySystem;
using BallzGame.InventorySystem.ShopItems;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net.Mime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallzGame.Managers.Shop
{
	public class GainItemCard : MonoBehaviour
	{
		public TMP_Text NameText;
		public TMP_Text DescriptionText;
		public TMP_Text Count;
		public TMP_Text PriceText;
		public Image IconImage;
		public Button button;
		public List<Image> SlotDots;
          public TMP_Text slotdotText;
		public Sprite slotdotFilled;
		public Sprite slotdotEmpty;
          ShopItem myData;
          private Color normalPriceColor;
          private TMP_Text cachedPriceText;
          public void RefreshPrice()
          {
               if (PriceText && myData)
               {
                    if (cachedPriceText != PriceText)
                    {
                         cachedPriceText = PriceText;
                         normalPriceColor = PriceText.color;
                    }
                    var shop = GameManager.Instance.shopController;
                    int price = shop.GetItemPrice(myData);
                    PriceText.text = price.ToString();
                    PriceText.color = shop.CurrentCoin < price ? Color.red : normalPriceColor;
               }
          }
          public void Refresh(){
               SetData(myData);
          }
          public void SetData(ShopItem data)
		{
               myData=data;

               if (NameText)
			{
				NameText.text = data.Name;
			}

			if (IconImage)
			{
				IconImage.sprite = data.Icon;
			}

			if (DescriptionText)
			{
				DescriptionText.text = data.Description;
			}
			if (PriceText)
			{
				RefreshPrice();
			}

			if (Count)
			{
				Count.text = data.Count.ToString();
			}
               if (data.maxAmount > 0)
               {
                    var item = GameManager.Instance.inventory.GetItem(
                        data.GetType(),
                        data.GetVersion()
                    );

                    int current = item != null ? item.Count : 0;
                    int max = data.maxAmount;

                    // 最多显示 8 个 Dot
                    int visibleCount = Mathf.Min(max, SlotDots.Count);

                    for (int i = 0; i < SlotDots.Count; i++)
                    {
                         Image dot = SlotDots[i];
                         if (dot == null) continue;

                         bool visible = i < visibleCount;
                         dot.gameObject.SetActive(visible);

                         if (visible)
                         {
                              dot.sprite = i < current
                                  ? slotdotFilled
                                  : slotdotEmpty;
                         }
                    }

                    if (slotdotText)
                    {
                         slotdotText.gameObject.SetActive(true);
                         slotdotText.text = $"{current}/{max}";
                    }
               }
               else
               {
                    foreach (var dot in SlotDots)
                    {
                         if (dot != null)
                              dot.gameObject.SetActive(false);
                    }

                    if (slotdotText)
                         slotdotText.gameObject.SetActive(false);
               }
          }

	}
}
