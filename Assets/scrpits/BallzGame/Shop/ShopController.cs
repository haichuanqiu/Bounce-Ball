using System.Collections;
using System.Collections.Generic;
using BallzGame.Balls;
using BallzGame.InventorySystem;
using BallzGame.InventorySystem.ShopItems;
using GameMeta;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BallzGame.Managers.Shop
{
    public class ShopController : MonoBehaviour
    {
        public int CurrentCoin
        {
            get { return _currentCoin; }
            set
            {
                _currentCoin = value;
                UpdateGoldTexts();
                MainMenu.Instance.InGamePanel.RefreshShopPrices();
            }
        }

        [SerializeField] private int _currentCoin;
        [SerializeField, Min(0)] private int startingCoin = 0;

        public void ResetCoins()
        {
            CurrentCoin = Mathf.Max(0, startingCoin);
            UpdateUI();
        }

        // 抽卡球列表（带概率权重）
        public List<BallProbability> GachaBalls;

        public int Gacha6Price;
        public int Gacha1Price;

        // Keep the original reference so existing scenes retain their assignment.
        [HideInInspector] public TextMeshProUGUI GoldText;
        public List<TextMeshProUGUI> GoldTexts = new List<TextMeshProUGUI>();

        // 商店商品列表（带概率权重）
        public List<ShopItemProbability> Items;

        [SerializeField] EventTrigger shopButton;

        private int totalGachaProbability;
        private int totalItemProbability;
        public bool HasCrisisFreeChoice { get; private set; }
        public int FreeItemRefreshes { get; private set; }
        public int RemainingCrisisChoices { get; private set; }
        private readonly HashSet<ShopItem> currentOffers = new HashSet<ShopItem>();
        private readonly HashSet<ShopItem> purchasedOffers = new HashSet<ShopItem>();

        public int GetItemPrice(ShopItem item) => HasCrisisFreeChoice ? 0 : item.Price;

        public void OpenCrisisReward(int rewardCount = 1)
        {
            if (rewardCount <= 0)
                return;
            RemainingCrisisChoices += rewardCount;
            HasCrisisFreeChoice = true;
            RefreshShopItems();
            if (currentOffers.Count == 0)
            {
                HasCrisisFreeChoice = false;
                RemainingCrisisChoices = 0;
            }
            MainMenu.Instance.InGamePanel.ShopSubPanel.SetActive(true);
            UpdateCrisisRewardUI();
        }

        public bool TryBuyItem(ShopItem item)
        {
            if (!item || !currentOffers.Contains(item) || purchasedOffers.Contains(item) || !item.Spawnable())
                return false;
            int price = GetItemPrice(item);
            if (CurrentCoin < price)
                return false;

            bool freeChoice = HasCrisisFreeChoice;
            purchasedOffers.Add(item);
            HasCrisisFreeChoice = false;
            if (freeChoice)
            {
                RemainingCrisisChoices--;
                FreeItemRefreshes++;
            }
            CurrentCoin -= price;
            GameManager.Instance.inventory.AddItemToInventory(item);
            UpdateCrisisRewardUI();
            return true;
        }

        private void UpdateCrisisRewardUI()
        {
            MainMenu.Instance.InGamePanel.UpdateCrisisRewardShop(HasCrisisFreeChoice);
        }

        public void ResetCrisisRewards()
        {
            HasCrisisFreeChoice = false;
            RemainingCrisisChoices = 0;
            FreeItemRefreshes = 0;
            currentOffers.Clear();
            purchasedOffers.Clear();
            UpdateCrisisRewardUI();
        }

        [SerializeField, Min(6)] private int launchesPerFreeDraw = 6;
        public int LaunchesPerFreeDraw => Mathf.Max(6, launchesPerFreeDraw);
        private int launchesSinceFreeDraw;
        public int FreeDrawCount { get; private set; }
        public int LaunchesUntilFreeDraw => LaunchesPerFreeDraw - launchesSinceFreeDraw;

        public void ResetFreeDraws()
        {
            ResetCrisisRewards();
            FreeDrawCount = 0;
            launchesSinceFreeDraw = 0;
            UpdateFreeDrawUI();
        }

        public void OnLaunchCompleted()
        {
            launchesSinceFreeDraw++;
            if (launchesSinceFreeDraw >= LaunchesPerFreeDraw)
            {
                launchesSinceFreeDraw = 0;
                FreeDrawCount++;
                GameManager.DoFreeDrawFloatText();
            }
            UpdateFreeDrawUI();
        }

        public void ClaimFreeDraw()
        {
            if (FreeDrawCount <= 0 || !GameManager.Instance.CanClaimFreeDraw)
                return;

            var resultBalls = RollBalls(6);
            if (resultBalls == null)
                return;

            FreeDrawCount--;
            GrantBalls(resultBalls);
        }

        public void UpdateFreeDrawUI()
        {
            var panel = MainMenu.Instance.InGamePanel;
            if (panel.FreeDrawCountText != null)
                panel.FreeDrawCountText.text = "X" + FreeDrawCount;
            if (panel.LaunchesUntilFreeDrawText != null)
                panel.LaunchesUntilFreeDrawText.text =  LaunchesUntilFreeDraw.ToString();
            if (panel.FreeDrawButton != null)
                panel.FreeDrawButton.interactable = FreeDrawCount > 0 && GameManager.Instance.CanClaimFreeDraw;
        }

        private void Start()
        {

            // 初始化时归一化概率
            NormalizedProbability();

            UpdateUI();

        }


        // ==================== 概率归一化 ====================

        [ContextMenu("NormalizedProbability")]
        void NormalizedProbability()
        {
            totalGachaProbability = 0;

            foreach (var item in GachaBalls)
            {
                totalGachaProbability += item.Probability;
            }

            // 如果不是100，自动归一化
            if (totalGachaProbability != 100)
            {
                foreach (var item in GachaBalls)
                {
                    item.Probability =
                        Mathf.RoundToInt(
                            item.Probability * 100f / totalGachaProbability
                        );
                }
            }

            totalGachaProbability = 0;

            foreach (var item in GachaBalls)
            {
                totalGachaProbability += item.Probability;
            }
            totalItemProbability = 0;

            foreach (var item in Items)
            {
                totalItemProbability += item.Probability;
            }

            // 如果不是100，自动归一化
            if (totalItemProbability != 100)
            {
                foreach (var item in Items)
                {
                    item.Probability =
                        Mathf.RoundToInt(
                            item.Probability * 100f / totalItemProbability
                        );
                }
            }

            totalItemProbability = 0;

            foreach (var item in Items)
            {
                totalItemProbability += item.Probability;
            }
        }


        // ==================== 随机抽取 ====================

        /// <summary>
        /// 按概率随机获取一个Ball（抽卡用）
        /// </summary>
        Ball GetRandomBallData()
        {
            // Read the current weights so rewards also work before Start runs.
            totalGachaProbability = 0;
            foreach (var item in GachaBalls)
                totalGachaProbability += item.Probability;

            int random = Random.Range(0, totalGachaProbability);

            int current = 0;

            foreach (var item in GachaBalls)
            {
                current += item.Probability;

                if (random < current)
                {
                    return item.Ball;
                }
            }

            // 防止没有return
            return GachaBalls[0].Ball;
        }

        /// <summary>
        /// 按概率随机获取不重复的商店商品
        /// </summary>
        public void RefreshShopItems()
        {
            currentOffers.Clear();
            purchasedOffers.Clear();
            List<ShopItem> result = new List<ShopItem>();

            // 只保留当前可以 Spawn 的物品
            List<ShopItemProbability> availableItems = new List<ShopItemProbability>();

            foreach (var item in Items)
            {
                if (item.Item != null && item.Item.Spawnable())
                {
                    availableItems.Add(item);
                }
            }

            // 最多展示 3 个
            int count = Mathf.Min(3, availableItems.Count);

            // 如果没有可以生成的物品
            if (count <= 0)
            {
                MainMenu.Instance.InGamePanel.RefreshItemShop(result);
                return;
            }

            // 临时列表，用于无重复抽取
            List<ShopItemProbability> tempList =
                new List<ShopItemProbability>(availableItems);

            // 计算当前可生成物品的总概率
            int remainingProbability = 0;

            foreach (var item in tempList)
            {
                remainingProbability += item.Probability;
            }

            for (int i = 0; i < count && tempList.Count > 0; i++)
            {
                // 防止剩余概率为 0
                if (remainingProbability <= 0)
                    break;

                int random = Random.Range(0, remainingProbability);

                int current = 0;
                int selectedIndex = 0;

                for (int j = 0; j < tempList.Count; j++)
                {
                    current += tempList[j].Probability;

                    if (random < current)
                    {
                        selectedIndex = j;
                        break;
                    }
                }

                // 添加抽中的 Item
                result.Add(tempList[selectedIndex].Item);
                currentOffers.Add(tempList[selectedIndex].Item);

                // 移除已经抽中的物品，避免重复
                int removedProb = tempList[selectedIndex].Probability;

                tempList.RemoveAt(selectedIndex);

                // 更新剩余概率
                remainingProbability -= removedProb;
            }

            // 不需要修改 Items 本身的 Probability
            MainMenu.Instance.InGamePanel.RefreshItemShop(result);
        }

        // ==================== UI & 交互 ====================


        public void GainCoin(int amount)
        {
            CurrentCoin += amount;
            UpdateUI();
        }


        [SerializeField]public int refreshPrice;
        public void PlayerRefreshItem()
        {
            if (HasCrisisFreeChoice)
                return;
            if (FreeItemRefreshes > 0)
            {
                FreeItemRefreshes--;
                HasCrisisFreeChoice = RemainingCrisisChoices > 0;
                RefreshShopItems();
                if (currentOffers.Count == 0)
                {
                    HasCrisisFreeChoice = false;
                    RemainingCrisisChoices = 0;
                }
                UpdateCrisisRewardUI();
                return;
            }
            if (CurrentCoin < refreshPrice)
            {
                return;
            }

            CurrentCoin -= refreshPrice;
            RefreshShopItems();
        }
        public void DoGacha6()
        {
            if (FreeDrawCount > 0)
            {
                ClaimFreeDraw();
                return;
            }
            DoGacha(6, Gacha6Price);
        }

        public void DoGacha1()
        {
            DoGacha(1, Gacha1Price);
        }

        private void DoGacha(int count, int price)
        {
            if (CurrentCoin < price)
            {
                Debug.Log("金币不足！");
                return;
            }

            var resultBalls = RollBalls(count);
            if (resultBalls == null)
                return;

            CurrentCoin -= price;
            GrantBalls(resultBalls);
        }

        public bool GrantRandomBalls(int count)
        {
            var resultBalls = RollBalls(count);
            if (resultBalls == null)
                return false;

            GrantBalls(resultBalls);
            return true;
        }

        private List<Ball> RollBalls(int count)
        {
            if (count <= 0 || GachaBalls == null || GachaBalls.Count == 0)
            {
                Debug.LogError("Cannot draw balls: the gacha pool is empty or the count is invalid.");
                return null;
            }

            int weight = 0;
            foreach (var item in GachaBalls)
            {
                if (item == null || item.Ball == null || item.Ball.Data == null || item.Probability < 0)
                {
                    Debug.LogError("Cannot draw balls: the gacha pool contains an invalid entry.");
                    return null;
                }
                weight += item.Probability;
            }
            if (weight <= 0)
            {
                Debug.LogError("Cannot draw balls: the total gacha weight must be positive.");
                return null;
            }

            List<Ball> resultBalls = new List<Ball>();

            for (int i = 0; i < count; i++)
            {
                Ball ball = GetRandomBallData();

                resultBalls.Add(ball);
            }
            return resultBalls;
        }

        private void GrantBalls(List<Ball> resultBalls)
        {
            List<BallData> resultDatas = new List<BallData>();
            foreach (var ball in resultBalls)
                resultDatas.Add(ball.Data);

            GameManager.Instance.launcher.AddBalls(resultBalls);

            // 显示抽卡结果
            MainMenu.Instance.InGamePanel.ShowGainBalls(resultDatas);

            UpdateUI();
        }

        private void UpdateUI()
        {
            UpdateFreeDrawUI();
            UpdateGoldTexts();

        }

        private void UpdateGoldTexts()
        {
            string amount = CurrentCoin.ToString();
            if (GoldText != null)
                GoldText.text = amount;

            if (GoldTexts == null)
                return;

            foreach (var goldText in GoldTexts)
            {
                if (goldText != null)
                    goldText.text = amount;
            }
        }
    }

    // ==================== 概率配置类 ====================

    /// <summary>
    /// 抽卡球概率配置（对应 BrickProbability）
    /// </summary>
    [System.Serializable]
    public class BallProbability
    {
        public Ball Ball;
        public int Probability;
    }

    /// <summary>
    /// 商店商品概率配置（对应 BrickProbability）
    /// </summary>
    [System.Serializable]
    public class ShopItemProbability
    {
        public ShopItem Item;
        public int Probability;
    }
}
