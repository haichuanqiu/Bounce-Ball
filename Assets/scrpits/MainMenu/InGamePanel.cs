using BallzGame.Balls;
using BallzGame.InventorySystem;
using BallzGame.InventorySystem.ShopItems;
using BallzGame.Managers;
using BallzGame.Managers.Shop;
using BallzGame.Managers.Shop.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameMeta
{
	public class InGamePanel : Panel
	{
		public GameObject Panel;
		public GameObject DieSubPanel;
		public GameObject ShopSubPanel;

		public Button DieSubPanelConfirmButton;
		public Button ShopOnePull;
		public Button ShopMultiplePull;
		public Button ShopSubPanelExit;
		public Button GainBallConfirm;

		[Header("Free six-ball draws")]
		public Button FreeDrawButton;
		public TMP_Text FreeDrawCountText;
		public TMP_Text LaunchesUntilFreeDrawText;

		public GameObject GainBallPanel;

		public bool IsShowingGainBalls => GainBallPanel.activeSelf;
		public List<GainBallCard> GainBallPanelItems;
		public List<GainItemCard>  GainItemCards;
		public TextMeshProUGUI Coin;
		public	TMP_Text WaveDisplay;
		public BallsDisplay BallsDisplay;
		public Button RefreshShopItem;
		public TMP_Text CurrentScore;
		[SerializeField]private EventTrigger shopButton;
		[SerializeField]private GameObject ballzGameUI;

		public void SetBallzGameUI(bool active)
		{
			ballzGameUI.active = active;
		}
		private void Start()
		{
			ShopSubPanelExit.onClick.AddListener(() =>
				{
					ShopSubPanel.SetActive(false);
				}
			);
			GainBallConfirm.onClick.AddListener(() =>
				{
					GainBallPanel.SetActive(false);
					GameManager.Instance.shopController.UpdateFreeDrawUI();
				}
			);
			RefreshShopItem.onClick.AddListener(GameManager.Instance.shopController.PlayerRefreshItem);
			if (FreeDrawButton != null)
				FreeDrawButton.onClick.AddListener(GameManager.Instance.shopController.ClaimFreeDraw);
			GameManager.Instance.shopController.UpdateFreeDrawUI();
			AddShopButtonEvents();
		}
		private Coroutine longPressCoroutine;
		private bool longPressTriggered;
		private void AddShopButtonEvents()
		{
			// 按下
			EventTrigger.Entry pointerDown = new EventTrigger.Entry
			{
				eventID = EventTriggerType.PointerDown
			};

			pointerDown.callback.AddListener(_ =>
			{
				longPressTriggered = false;
				longPressCoroutine = StartCoroutine(CheckLongPress());
			});
			UnityEngine.Debug.Log(shopButton);
               UnityEngine.Debug.Log(pointerDown);
               shopButton.triggers.Add(pointerDown);


			// 松开
			EventTrigger.Entry pointerUp = new EventTrigger.Entry
			{
				eventID = EventTriggerType.PointerUp
			};

			pointerUp.callback.AddListener(_ =>
			{
				if (longPressCoroutine != null)
				{
					StopCoroutine(longPressCoroutine);
					longPressCoroutine = null;
				}

				// 没有触发长按，才算短按
				if (!longPressTriggered)
				{
					ShowCurrentBall();
					ShopSubPanel.SetActive(true);
				}
			});

			shopButton.triggers.Add(pointerUp);
		}

		private IEnumerator CheckLongPress()
		{
			yield return new WaitForSeconds(0.5f);

			longPressTriggered = true;
			GameManager.Instance.shopController.DoGacha6();
		}
		public void ShowGainBalls(List<BallData> datas)
		{
			GainBallPanel.SetActive(true);

			// 全部隐藏
			for (int i = 0; i < GainBallPanelItems.Count; i++)
			{
				GainBallPanelItems[i].gameObject.SetActive(false);
			}

			// 填充
			for (int i = 0; i < datas.Count && i < GainBallPanelItems.Count; i++)
			{
				GainBallPanelItems[i].gameObject.SetActive(true);
				GainBallPanelItems[i].SetData(datas[i]);
			}
		}

		public void ShowCurrentBall()
		{
			BallsDisplay.ShowBalls(GameManager.Instance.launcher.ballDatas);
		}
		public void RefreshItemShop(List<ShopItem> shopItems)
		{
			// 全部隐藏
			for (int i = 0; i < GainItemCards.Count; i++)
			{
				GainItemCards[i].gameObject.SetActive(false);
			}

			// 填充
			for (int i = 0; i < shopItems.Count && i < GainItemCards.Count; i++)
			{
				var card = GainItemCards[i];
				var data = shopItems[i];
				card.button.interactable = true;
				card.gameObject.SetActive(true);
				card.SetData(data);

				// 先清掉旧监听（很重要）
				card.button.onClick.RemoveAllListeners();

				card.button.onClick.AddListener(() =>
				{
					// 扣钱（你可以自己加价格判断）
					var shop = GameManager.Instance.shopController;

					if (shop.CurrentCoin < data.Price || !data.Spawnable())
						return;

					shop.CurrentCoin -= data.Price;

					// 加入背包
					GameManager.Instance.inventory.AddItemToInventory(data);
					card.button.interactable = false;
				});
			}
		}

		public override void Activate()
		{
			Panel.SetActive(true);
		}

		public override void Disable()
		{
			Panel.SetActive(false);
		}
	}
}
