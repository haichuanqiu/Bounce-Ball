using System.Collections;
using BallzGame.Balls;
using BallzGame.Bricks;
using BallzGame.InventorySystem;
using BallzGame.Managers.Shop;
using BallzGame.Minigame;
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
        public FeverController feverController;
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

        [Header("current game status")]
        private Brick[,] grid;
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
            ResetAllController();
            if (gameLoop!=null)
            {
                StopCoroutine(gameLoop);
            }
            MainMenu.Instance.Goto(MainMenu.CurrentPanel.InGame);
            CurrentResult=new();
            grid = new Brick[width, height];
            MainMenu.Instance.InGamePanel.CurrentScore.text = "Score:  "+ CurrentResult.Points.ToString();
            gameLoop=StartCoroutine(GameLoop());
        }

        private Coroutine gameLoop;
        IEnumerator GameLoop()
        {
            state = State.TrySpawnRow;
            MainMenu.Instance.InGamePanel.WaveDisplay.text = "Wave:   "+level.ToString();
            while (true)
            {
                switch (state)
                {
                    case State.WaitForFeverOrLaunchInput:
                        shopController.RefreshShopItems();
                        launcher.WaitForInput();
                        feverController.WaitForInput();
                        yield return new WaitUntil(() => launcher.HasInput || feverController.FeverClicked);
                        if (launcher.HasInput)
                        {
                            state = State.ShootBall;
                        }
                        else
                        {
                            state = State.Fever;
                        }
                        feverController.StopListenToInput();
                        launcher.StopListenToInput();
                        break;
                    case State.ShootBall:
                        yield return new WaitForEndOfFrame();
                        yield return StartCoroutine( launcher.Launch() );
                        state = State.TrySpawnRow;
                        level++;
                        MainMenu.Instance.InGamePanel.WaveDisplay.text = "Wave:   "+level.ToString();
                        MainMenu.Instance.InGamePanel.WaveDisplay.color = (level+(crisisInterval-FirstCrisis))  % crisisInterval == 0 ? Color.red : Color.white;
                        break;
                    case State.Fever:
                        NotifyBricksMiniGameStart();
                        yield return StartCoroutine(feverController.StartFeverGame(GetContext()));
                        NotifyBricksMiniGameEnd();
                        state = State.WaitForFeverOrLaunchInput;
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
                            state = State.WaitForFeverOrLaunchInput;
                        }
                        break;
                    case State.GameOver:
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
            feverController.Reset();
            launcher.Reset();
            BallExtraDamageController.Reset();
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
            var bricks = spawner.SpawnRow(level, width);

            for (int x = 0; x < width; x++)
            {
                Brick brick = bricks[x];

                grid[x, 0] = brick;

                if (brick != null)
                {
                    brick.transform.position = new Vector3(x, 0, 0);
                }
            }
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

                    brick.transform.position =
                        new Vector3(x, -(y + 1), 0);
                    brick.OnRowMoved();
                }
            }

            StillHaveSpace = true;
            return StillHaveSpace;
        }

        void NotifyBricksMiniGameStart()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y])
                    {
                        grid[x, y].OnMiniGameStart();
                    }
                }
            }
        }


        void NotifyBricksMiniGameEnd()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] != null)
                    {
                        grid[x, y].OnMiniGameEnd();
                    }
                }
            }
        }
        private FeverGameContext GetContext()
        {
            var context = new FeverGameContext();
            context.CurrentLevel = level;
            context.Grid = grid;
            return context;
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
            WaitForFeverOrLaunchInput,
            Fever,
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
        public static void DoHealText(Vector3 pos,int amount)
        {

            var floatTextPrefab = Instance.brickHealFloatText;
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
                    "+"+amount,
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
