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
		public GameObject PauseSubPanel;
		public Button PauseButton;
		public Button ResumeButton;
		public Button ReturnToMainMenuButton;

		public Button DieSubPanelConfirmButton;
		public Button ShopSubPanelExit;
          public Button ShopSubPanelExit2;
          public Button GainBallConfirm;

		[Header("Free six-ball draws")]
		public Button FreeDrawButton;
		public TMP_Text FreeDrawCountText;
		public TMP_Text LaunchesUntilFreeDrawText;

		[Header("Next crisis progress")]
		[Tooltip("Use an Image with Type = Filled for the progress bar.")]
		[SerializeField] private Image crisisProgressImage;
		[SerializeField] private TMP_Text crisisWaveText;

		public void SetCrisisProgress(int currentWave, int nextWave, float progress)
		{
			if (crisisProgressImage)
				crisisProgressImage.fillAmount = Mathf.Clamp01(progress);
			if (crisisWaveText)
				crisisWaveText.SetText("{0}/{1}", currentWave, nextWave);
		}

		public void SetCrisisWaveActive(bool active)
		{
			if (WaveDisplay)
				WaveDisplay.color = active ? Color.red : Color.white;
		}

		public GameObject GainBallPanel;

		public bool IsShowingGainBalls => GainBallPanel.activeSelf;
		public List<GainBallCard> GainBallPanelItems;
		public List<GainItemCard>  GainItemCards;
		public TextMeshProUGUI Coin;
		public	TMP_Text WaveDisplay;
		public BallsDisplay BallsDisplay;
		public Button RefreshShopItem;
		[SerializeField] private TMP_Text refreshPriceNumberText;
		private TMP_Text cachedRefreshPriceText;
		private Color normalRefreshPriceColor;

		public void UpdateCrisisRewardShop(bool choosingFreeItem)
		{
			var shop = GameManager.Instance.shopController;
			if (ShopSubPanelExit)
				ShopSubPanelExit.interactable = shop.RemainingCrisisChoices == 0;
			if (ShopSubPanelExit2)
				ShopSubPanelExit2.interactable = shop.RemainingCrisisChoices == 0;
			if (RefreshShopItem)
				RefreshShopItem.interactable = !choosingFreeItem;
			RefreshShopPrices();
		}

		public void RefreshShopPrices()
		{
			var shop = GameManager.Instance.shopController;
			int refreshCost = shop.HasCrisisFreeChoice || shop.FreeItemRefreshes > 0 ? 0 : shop.refreshPrice;
			if (refreshPriceNumberText)
			{
				if (cachedRefreshPriceText != refreshPriceNumberText)
				{
					cachedRefreshPriceText = refreshPriceNumberText;
					normalRefreshPriceColor = refreshPriceNumberText.color;
				}
				refreshPriceNumberText.text = refreshCost.ToString();
				refreshPriceNumberText.color = shop.CurrentCoin < refreshCost ? Color.red : normalRefreshPriceColor;
			}
			foreach (var card in GainItemCards)
				if (card && card.gameObject.activeSelf)
					card.RefreshPrice();
		}
		public TMP_Text CurrentScore;
		[SerializeField]private EventTrigger shopButton;
		[SerializeField]private GameObject ballzGameUI;

		public void SetBallzGameUI(bool active)
		{
			ballzGameUI.active = active;
		}
		private void Start()
		{
			if (PauseButton) PauseButton.onClick.AddListener(GameManager.Instance.PauseGame);
			if (ResumeButton) ResumeButton.onClick.AddListener(GameManager.Instance.ResumeGame);
			if (ReturnToMainMenuButton) ReturnToMainMenuButton.onClick.AddListener(GameManager.Instance.ReturnToMainMenu);
			ShopSubPanelExit.onClick.AddListener(() =>
				{
					ShopSubPanel.SetActive(false);
				}
			);
               ShopSubPanelExit2.onClick.AddListener(() =>
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
					var shop = GameManager.Instance.shopController;

					if (!shop.TryBuyItem(data))
						return;
					card.button.interactable = false;
					card.Refresh();
				});
			}
		}

		public override void Activate()
		{
			Panel.SetActive(true);
		}

		public override void Disable()
		{
			if (GameManager.Instance) GameManager.Instance.ResumeGame();
			Panel.SetActive(false);
		}

		public void CloseSubPanels()
		{
			if (longPressCoroutine != null) StopCoroutine(longPressCoroutine);
			longPressCoroutine = null;
			longPressTriggered = false;
			DieSubPanel.SetActive(false);
			ShopSubPanel.SetActive(false);
			GainBallPanel.SetActive(false);
			if (PauseSubPanel) PauseSubPanel.SetActive(false);
		}
	}
}
