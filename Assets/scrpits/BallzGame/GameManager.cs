using System.Collections;
using BallzGame.Balls;
using BallzGame.Bricks;
using BallzGame.InventorySystem;
using BallzGame.Managers.Shop;
using GameMeta;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Utils;

namespace BallzGame.Managers
{


    public class GameManager : MonoBehaviour
    {

        [Header("controllers")]
        public BrickSpawner spawner;
        public BallLauncher launcher;
        public ShopController shopController;
        public Inventory inventory;
        public BallExtraDamage BallExtraDamageController;
        public CrisisManager CrisisManager;



        public static GameManager Instance { get; private set; }
        [Header("scene reference")]
        public Transform BallzCameraPosition;
        public Transform BricksParent;
        public Transform BallsParent;
        public Transform VisualEffectsParent;
        public Transform BrickPointTarget;
        public Camera MainCamera;
        [Header("game setting")]
        public int width = 7;
        [SerializeField]private int height = 15;
        [SerializeField]private int crisisInterval=15;
        [SerializeField]private int FirstCrisis=5;
        public int FirstCrisisWave => FirstCrisis;
        public int CrisisInterval => crisisInterval;

        [Header("current game status")]
        private Brick[,] grid;

        public Brick GetBrickAt(int x, int y)
        {
            if (grid == null || x < 0 || y < 0 ||
                x >= grid.GetLength(0) || y >= grid.GetLength(1))
                return null;

            Brick brick = grid[x, y];
            return brick && brick.Hp > 0 ? brick : null;
        }

        public int level = 1;
        private State state;
        public GameResultPanel.GameResult CurrentResult;
        [Header("Configs")]
        public BallSystemConfig BallConfig;
        [Header("FloatTextPrefab")]
        [SerializeField]private FloatText blockFloatText;
        [SerializeField]private FloatText brickHealFloatText;
        [SerializeField]private FloatText brickShieldFloatText;
        [SerializeField]private FloatText brickDamageFloatText;

        [SerializeField] private FloatText freeDrawFloatText;
        public Transform FreeDrawFloatTextPosition;

        public bool CanClaimFreeDraw => state == State.WaitForLaunchInput &&
            !launcher.HasInput && !MainMenu.Instance.InGamePanel.IsShowingGainBalls;

          [SerializeField] private GameObject healEffect;
          [Header("events")]
        public UnityEvent BeforeRowSpawn;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = FindAnyObjectByType<GameManager>();
            }
            var gamePanel = MainMenu.Instance.InGamePanel;
            gamePanel.DieSubPanelConfirmButton.onClick.AddListener(BackToMainMenuButtonClicked);
        }
        public void NewGame()
        {
            if (gameLoop!=null)
            {
                StopCoroutine(gameLoop);
            }
            ResetAllController();
            level = 1;
            state = State.TrySpawnRow;
            shopController.ResetCoins();
            MainMenu.Instance.InGamePanel.WaveDisplay.text = "Wave:   " + level;
            MainMenu.Instance.InGamePanel.WaveDisplay.color = Color.white;
            MainMenu.Instance.Goto(MainMenu.CurrentPanel.InGame);
            CurrentResult=new();
            grid = new Brick[width, height];
            MainMenu.Instance.InGamePanel.CurrentScore.text = "Score:  "+ CurrentResult.Points.ToString();
            gameLoop=StartCoroutine(GameLoop());
        }

        private Coroutine gameLoop;
        IEnumerator GameLoop()
        {
            // Confirm the opening reward before spawning bricks or accepting aim input.
            if (!shopController.GrantRandomBalls(6))
                yield break;

            yield return new WaitUntil(() => !MainMenu.Instance.InGamePanel.IsShowingGainBalls);
            yield return null;

            state = State.TrySpawnRow;
            MainMenu.Instance.InGamePanel.WaveDisplay.text = "Wave:   "+level.ToString();
            while (true)
            {
                switch (state)
                {
                    case State.WaitForLaunchInput:
                        shopController.RefreshShopItems();
                        int crisisRewards = 0;
                        while (CrisisManager && CrisisManager.TryConsumeCompletedReward())
                            crisisRewards++;
                        if (crisisRewards > 0)
                        {
                            shopController.OpenCrisisReward(crisisRewards);
                            yield return new WaitUntil(() => !MainMenu.Instance.InGamePanel.ShopSubPanel.activeSelf);
                        }
                        launcher.WaitForInput();
                        shopController.UpdateFreeDrawUI();
                        yield return new WaitUntil(() => launcher.HasInput);
                        state = State.ShootBall;
                        launcher.StopListenToInput();
                        shopController.UpdateFreeDrawUI();
                        break;
                    case State.ShootBall:
                        yield return new WaitForEndOfFrame();
                        yield return StartCoroutine( launcher.Launch() );
                        shopController.OnLaunchCompleted();
                        state = State.TrySpawnRow;
                        level++;
                        MainMenu.Instance.InGamePanel.WaveDisplay.text = "Wave:   "+level.ToString();
                        break;
                    case State.TrySpawnRow:

                        bool gameOver = !TryMoveBricksDown();

                        if (gameOver)
                        {
                            state = State.GameOver;
                        }
                        else
                        {
                            if ( (level+(crisisInterval-FirstCrisis))  % crisisInterval == 0)
                            {
                                CrisisManager.DoCrisis();
                            }

                            BeforeRowSpawn.Invoke();
                            SpawnTopRow();
                            state = State.WaitForLaunchInput;
                        }
                        break;
                    case State.GameOver:
                        shopController.UpdateFreeDrawUI();
                        MainMenu.Instance.InGamePanel.DieSubPanel.SetActive(true);
                        yield break;

                }

                yield return null;
            }
        }



        void BackToMainMenuButtonClicked()
        {
            if (state == State.GameOver)
            {
                CurrentResult.BallsCount  = launcher.BallCount;
                level = 1;
                ClearAllBricks();
                MainMenu.Instance.InGamePanel.DieSubPanel.SetActive(false);
                MainMenu.Instance.GameResultPanel.SetResult(CurrentResult);
                MainMenu.Instance.Goto(MainMenu.CurrentPanel.GameResult);
                ResetAllController();
            }
        }


        private void ResetAllController()
        {
            if (CrisisManager)
                CrisisManager.ClearMarks();
            spawner.NextRowIsCrisis = false;
            inventory.Clear();
            launcher.Reset();
            BallExtraDamageController.Reset();
            shopController.ResetFreeDraws();
        }

        public static void DoFreeDrawFloatText()
        {
               Debug.Log("called");
            if (Instance == null || Instance.freeDrawFloatText == null ||
                Instance.FreeDrawFloatTextPosition == null)
                return;

            var position = Instance.FreeDrawFloatTextPosition.position;
            var floatText = Instantiate(Instance.freeDrawFloatText, position, Quaternion.identity);
            floatText.DoFloatText("+1", position, Color.yellow, Vector2.up, 0.8f, Instance.FreeDrawFloatTextPosition);
        }

        void ClearAllBricks()
        {
            if (grid == null)
                return;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Brick brick = grid[x, y];

                    if (brick != null)
                    {
                        Destroy(brick.gameObject);
                        grid[x, y] = null;
                    }
                }
            }
        }


        void SpawnTopRow()
        {
            bool isCrisisRow = spawner.NextRowIsCrisis;
            var bricks = spawner.SpawnRow(level, width);
            spawner.NextRowIsCrisis = false;
            MainMenu.Instance.InGamePanel.SetCrisisWaveActive(isCrisisRow);

            for (int x = 0; x < width; x++)
            {
                Brick brick = bricks[x];

                grid[x, 0] = brick;

                if (brick != null)
                {
                    brick.GridPosition = new Vector2Int(x, 0);
                    brick.transform.position = new Vector3(x, 0, 0);
                }
            }
            if (isCrisisRow && CrisisManager)
                CrisisManager.RegisterRow(bricks);
        }




        bool TryMoveBricksDown()
        {
            bool StillHaveSpace;
            for (int x = 0; x < width; x++)
            {
                if (grid[x, height - 1])
                {
                    StillHaveSpace = false;
                    return StillHaveSpace;
                }
            }

            // 再移动
            for (int y = height - 2; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    Brick brick = grid[x, y];

                    if (brick == null)
                        continue;

                    grid[x, y + 1] = brick;
                    grid[x, y] = null;

                    brick.GridPosition = new Vector2Int(x, y + 1);
                    brick.transform.position =
                        new Vector3(x, -(y + 1), 0);
                }
            }

            // Notify only after every cell and position has reached its final state.
            for (int y = height - 1; y >= 1; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    Brick brick = GetBrickAt(x, y);
                    if (brick)
                        brick.OnRowMoved();
                }
            }

            StillHaveSpace = true;
            return StillHaveSpace;
        }

        [ContextMenu("Print Grid")]
        private void PrintGrid()
        {
            int w = grid.GetLength(0);
            int h = grid.GetLength(1);

            string result = "";

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (grid[x, y] == null)
                    {
                        result += "0\t";
                    }
                    else
                    {
                        result += grid[x, y].Hp + "\t";
                    }
                }

                result += "\n";
            }

            Debug.Log(result);
        }
        public enum State
        {
            TrySpawnRow,
            WaitForLaunchInput,
            GameOver,
            ShootBall,
        }

        public static void DoBlockFloatText(Vector3 pos)
        {

            var floatTextPrefab = Instance.blockFloatText;
            var   content = "BLOCK!";
            if (floatTextPrefab != null)
            {
                Vector3 spawnPos = pos;

                // 👉 稍微抬高一点避免重叠
                spawnPos += Vector3.up * 0.2f;

                // 👉 随机一点方向（更自然）
                Vector2 dir = (Vector2.up + Random.insideUnitCircle * 0.5f).normalized;

                // 👉 实例化
                FloatText ft = Instantiate(
                    floatTextPrefab,
                    spawnPos,
                    Quaternion.identity
                );

                // 👉 颜色可以自己调（这里红色伤害）
                ft.DoFloatText(
                    content,
                    spawnPos,
                    Color.red,
                    dir,
                    0.8f
                );
            }
        }
          public static void DoHealText(Vector3 pos, int amount)
          {

               var floatTextPrefab = Instance.brickHealFloatText;
               var vfx = Instance.healEffect;
               if (floatTextPrefab != null)
               {
                    Vector3 spawnPos = pos;
                    GameObject gm = Instantiate(
                                      vfx,
                                      spawnPos,
                                      Quaternion.identity
                                  );
                    // 👉 稍微抬高一点避免重叠
                    spawnPos += Vector3.up * 0.2f;

                    // 👉 随机一点方向（更自然）
                    Vector2 dir = (Vector2.up + Random.insideUnitCircle * 0.5f).normalized;

                    // 👉 实例化
                    FloatText ft = Instantiate(
                        floatTextPrefab,
                        spawnPos,
                        Quaternion.identity
                    );

                    // 👉 颜色可以自己调（这里红色伤害）
                    ft.DoFloatText(
                        "+" + amount,
                        spawnPos,
                        Color.green,
                        dir,
                        0.8f
                    );
               }
          }
          public static void DoShieldText(Vector3 pos,int amount)
        {

            var floatTextPrefab = Instance.brickShieldFloatText;
            if (floatTextPrefab != null)
            {
                Vector3 spawnPos = pos;

                // 👉 稍微抬高一点避免重叠
                spawnPos += Vector3.up * 0.2f;

                // 👉 随机一点方向（更自然）
                Vector2 dir = (Vector2.up + Random.insideUnitCircle * 0.5f).normalized;

                // 👉 实例化
                FloatText ft = Instantiate(
                    floatTextPrefab,
                    spawnPos,
                    Quaternion.identity
                );

                // 👉 颜色可以自己调（这里红色伤害）
                ft.DoFloatText(
                    "-"+amount,
                    spawnPos,
                    Color.white,
                    dir,
                    0.8f
                );
            }
        }
        public static void DoDamageText(Vector3 pos,int amount)
        {

            var floatTextPrefab = Instance.brickDamageFloatText;
            if (floatTextPrefab != null)
            {
                Vector3 spawnPos = pos;

                // 👉 稍微抬高一点避免重叠
                spawnPos += Vector3.up * 0.2f;

                // 👉 随机一点方向（更自然）
                Vector2 dir = (Vector2.up + Random.insideUnitCircle * 0.5f).normalized;

                // 👉 实例化
                FloatText ft = Instantiate(
                    floatTextPrefab,
                    spawnPos,
                    Quaternion.identity
                );

                // 👉 颜色可以自己调（这里红色伤害）
                ft.DoFloatText(
                    "-"+amount,
                    spawnPos,
                    Color.white,
                    dir,
                    0.8f
                );
            }
        }
        public static void DoCritText(Vector3 pos,int amount)
        {

            var floatTextPrefab = Instance.brickDamageFloatText;
            if (floatTextPrefab != null)
            {
                Vector3 spawnPos = pos;

                // 👉 稍微抬高一点避免重叠
                spawnPos += Vector3.up * 0.2f;

                // 👉 随机一点方向（更自然）
                Vector2 dir = (Vector2.up + Random.insideUnitCircle * 0.5f).normalized;

                // 👉 实例化
                FloatText ft = Instantiate(
                    floatTextPrefab,
                    spawnPos,
                    Quaternion.identity
                );

                // 👉 颜色可以自己调（这里红色伤害）
                ft.DoFloatText(
                    "-"+amount,
                    spawnPos,
                    Color.yellow,
                    dir,
                    0.8f
                );
            }
        }

        public void AddCurrentResultPoints(int increment)
        {
            CurrentResult.Points += increment;
            MainMenu.Instance.InGamePanel.CurrentScore.text = "Score:  "+CurrentResult.Points.ToString();
        }
    }


}
