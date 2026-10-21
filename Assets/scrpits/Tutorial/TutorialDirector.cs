using System;
using System.Collections;
using System.Collections.Generic;
using BallzGame.Bricks;
using BallzGame.Bricks.SpecialBricks;
using BallzGame.Control;
using BallzGame.Managers;
using GameMeta;
using UnityEngine;

namespace BallzGame.Tutorial
{
    public enum TutorialSegmentKind
    {
        OpeningDraw,
        FreeExperience,
        HealBrick,
        ShieldBrick,
        BlackHoleBrick,
        NormalProgression,
        FreeBuffAndFinish = 7,
        Message = 8
    }

    [Serializable]
    public sealed class TutorialSegmentConfig
    {
        public bool Enabled = true;
        public string StableId;
        public TutorialSegmentKind Kind;
    }

    public sealed class TutorialDirector : MonoBehaviour
    {
        public static TutorialDirector Instance { get; private set; }

        [SerializeField] private List<TutorialSegmentConfig> segments = new List<TutorialSegmentConfig>();
        [SerializeField, Min(0.1f)] private float demonstrationInterval = 0.5f;
        [SerializeField] private int demonstrationBrickHp = 3;
        [SerializeField, Min(1)] private int timedSpecialBrickHp = 3;

        private TutorialOverlay overlay;
        private Coroutine routine;
        private bool running;

        public bool IsRunning => running;
        public IReadOnlyList<TutorialSegmentConfig> Segments => segments;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (segments == null || segments.Count == 0)
                ResetDefaultSegments();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        [ContextMenu("Reset Default Tutorial Segments")]
        public void ResetDefaultSegments()
        {
            segments = new List<TutorialSegmentConfig>
            {
                Segment(true, "opening-draw", TutorialSegmentKind.OpeningDraw),
                Segment(true, "free-experience", TutorialSegmentKind.FreeExperience),
                Segment(true, "heal-brick", TutorialSegmentKind.HealBrick),
                Segment(true, "shield-brick", TutorialSegmentKind.ShieldBrick),
                Segment(true, "black-hole-brick", TutorialSegmentKind.BlackHoleBrick),
                Segment(true, "crisis", TutorialSegmentKind.NormalProgression),
                Segment(true, "post-crisis-practice", TutorialSegmentKind.FreeBuffAndFinish)
            };
        }

        private static TutorialSegmentConfig Segment(bool enabled, string id, TutorialSegmentKind kind)
        {
            return new TutorialSegmentConfig { Enabled = enabled, StableId = id, Kind = kind };
        }

        public void StartTutorial()
        {
            if (running || !GameManager.Instance)
                return;
            routine = StartCoroutine(RunTutorial());
        }

        private IEnumerator RunTutorial()
        {
            running = true;
            overlay = FindFirstObjectByType<TutorialOverlay>(FindObjectsInactive.Include);
            if (!overlay)
            {
                Debug.LogError("Tutorial: no TutorialOverlay exists under Canvas/NavigationPages/HowToPlay.");
                running = false;
                routine = null;
                yield break;
            }
            GameManager manager = GameManager.Instance;
            manager.BeginTutorialSession();
            overlay.gameObject.SetActive(true);

            try
            {
                foreach (TutorialSegmentConfig segment in segments)
                {
                    if (segment == null || !segment.Enabled)
                        continue;
                    yield return RunSegment(segment, manager);
                    if (!manager.IsGameRunning)
                        yield break;
                }
            }
            finally
            {
                manager.launcher.SetForcedLaunchTarget(null);
                if (overlay)
                {
                    overlay.Hide();
                    overlay.gameObject.SetActive(false);
                }
                overlay = null;
                running = false;
                routine = null;
            }
        }

        private IEnumerator RunSegment(TutorialSegmentConfig segment, GameManager manager)
        {
            switch (segment.Kind)
            {
                case TutorialSegmentKind.OpeningDraw:
                    yield return OpeningDraw(segment, manager);
                    break;
                case TutorialSegmentKind.FreeExperience:
                    yield return FreeExperience(segment, manager);
                    break;
                case TutorialSegmentKind.HealBrick:
                    yield return HealBrickDemo(segment, manager);
                    break;
                case TutorialSegmentKind.ShieldBrick:
                    yield return TimedBrickDemo<TreeBrick>(segment, manager);
                    break;
                case TutorialSegmentKind.BlackHoleBrick:
                    yield return TimedBrickDemo<BlackHoleBrick>(segment, manager);
                    break;
                case TutorialSegmentKind.NormalProgression:
                    yield return NormalProgression(segment, manager);
                    break;
                case TutorialSegmentKind.FreeBuffAndFinish:
                    yield return FreeBuffAndFinish(segment, manager);
                    break;
                default:
                    yield return overlay.Show(segment.StableId);
                    break;
            }
        }

        private IEnumerator OpeningDraw(TutorialSegmentConfig segment, GameManager manager)
        {
            yield return overlay.Show("opening-draw");
            overlay.Hide();
            if (!manager.shopController.GrantBallsByTypes(
                    BallzGame.Balls.BallExtraDamage.BallType.NormalBall,
                    BallzGame.Balls.BallExtraDamage.BallType.NormalBall,
                    BallzGame.Balls.BallExtraDamage.BallType.HorizontalLazerBall,
                    BallzGame.Balls.BallExtraDamage.BallType.NormalBall,
                    BallzGame.Balls.BallExtraDamage.BallType.NormalBall,
                    BallzGame.Balls.BallExtraDamage.BallType.ExplosionBall))
                yield break;
            yield return new WaitUntil(() => !MainMenu.Instance.InGamePanel.IsShowingGainBalls);
        }

        private IEnumerator FreeExperience(TutorialSegmentConfig segment, GameManager manager)
        {
            manager.ClearScenarioBricks();
            Brick normal = FindBrickPrefab(manager, null);
            if (!normal)
                yield break;
            for (int x = 1; x < manager.width; x += 2)
                manager.TrySpawnScenarioBrick(normal, new Vector2Int(x, 3), 1, out _);
            yield return overlay.Show("free-experience");
            overlay.Hide();
            yield return manager.RunScenarioLaunch(null, false);
        }

        private IEnumerator HealBrickDemo(TutorialSegmentConfig segment, GameManager manager)
        {
            manager.ClearScenarioBricks();
            Brick healPrefab = FindBrickPrefab<HealBrick>(manager);
            Brick normal = FindBrickPrefab(manager, null);
            if (!healPrefab || !normal)
            {
                yield return overlay.Show("heal-brick-missing");
                yield break;
            }

            manager.TrySpawnScenarioBrick(healPrefab, new Vector2Int(manager.width / 2, 1), 1, out Brick heal);
            yield return overlay.Show("heal-brick-intro");
            manager.TryMoveScenarioBricksDown();

            int center = manager.width / 2;
            for (int x = center - 1; x <= center + 1; x++)
            {
                if (manager.TrySpawnScenarioBrick(normal, new Vector2Int(x, 1), demonstrationBrickHp * 2, out Brick wounded))
                    wounded.TakeDamage(demonstrationBrickHp, this);
            }
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(demonstrationInterval);
                manager.TryMoveScenarioBricksDown();
            }
            yield return new WaitForSeconds(0.5f);

            yield return overlay.Show("heal-brick-attack");
            overlay.Hide();
            if (heal)
                yield return manager.RunScenarioLaunch(heal.transform.position, false);
            if (heal)
                heal.TakeDamage(heal.Hp, this);
            yield return overlay.Show("heal-brick-complete");
        }

        private IEnumerator TimedBrickDemo<T>(TutorialSegmentConfig segment, GameManager manager)
            where T : Component
        {
            manager.ClearScenarioBricks();
            Brick specialPrefab = FindBrickPrefab<T>(manager);
            Brick normal = FindBrickPrefab(manager, null);
            if (!specialPrefab || !normal)
            {
                yield return overlay.Show(segment.StableId + "-missing");
                yield break;
            }

            int center = manager.width / 2;
            Brick special = null;
            for (int y = 2; y <= 4; y++)
            for (int x = center - 1; x <= center + 1; x++)
            {
                Brick prefab = x == center && y == 3 ? specialPrefab : normal;
                manager.TrySpawnScenarioBrick(prefab, new Vector2Int(x, y), x == center && y == 3 ? timedSpecialBrickHp : demonstrationBrickHp, out Brick spawned);
                if (x == center && y == 3)
                    special = spawned;
            }

            IBrickEffectControl control = special ? special.GetComponentInChildren<T>(true) as IBrickEffectControl : null;
            Component controlComponent = control as Component;
            control?.SetAutomaticScheduling(false);
            yield return overlay.Show(segment.StableId + "-intro");
            control?.ActivateEffect();
            yield return overlay.Show(segment.StableId + "-active");
            overlay.Hide();
            Brick protectedTarget = manager.GetBrickAt(center - 1, 3);
            if (protectedTarget)
                yield return manager.RunScenarioLaunch(protectedTarget.transform.position, false);

            if (controlComponent)
                control.DeactivateEffect();
            yield return overlay.Show(segment.StableId + "-inactive");
            overlay.Hide();
            if (special)
                yield return manager.RunScenarioLaunch(special.transform.position, false);
            if (special)
                special.TakeDamage(special.Hp, this);
            yield return overlay.Show(segment.StableId + "-complete");
        }

        private IEnumerator NormalProgression(TutorialSegmentConfig segment, GameManager manager)
        {
            manager.ClearScenarioBricks();
            if (!manager.CrisisManager)
            {
                yield return overlay.Show("crisis-missing");
                yield break;
            }

            yield return overlay.Show("crisis-intro");
            overlay.Hide();

            int rowCount = manager.CrisisManager.DoTutorialBigBrickCrisis();
            for (int row = 0; row < rowCount && manager.IsGameRunning; row++)
            {
                if (row > 0 && !manager.TryMoveScenarioBricksDown())
                    yield break;
                manager.SpawnScenarioTopRow();
                yield return manager.RunScenarioLaunch(null, false);
            }

            bool completed = manager.CrisisManager.TryConsumeCompletedReward();
            while (!completed && manager.IsGameRunning)
            {
                yield return manager.RunScenarioLaunch(null, false);
                completed = manager.CrisisManager.TryConsumeCompletedReward();
            }

            if (!completed)
                yield break;

            yield return overlay.Show("crisis-complete");
            overlay.Hide();
            manager.shopController.OpenCrisisReward(1);
            yield return new WaitUntil(() =>
                (!MainMenu.Instance.InGamePanel.ShopSubPanel.activeSelf && manager.shopController.RemainingCrisisChoices == 0) ||
                !manager.IsGameRunning);
        }

        private IEnumerator FreeBuffAndFinish(TutorialSegmentConfig segment, GameManager manager)
        {
            manager.ClearScenarioBricks();
            if (!manager.IsGameRunning)
                yield break;

            // NormalProgression has just waited for the reward shop to close.
            yield return overlay.Show("shop-reminder");
            overlay.Hide();
            if (!manager.IsGameRunning)
                yield break;

            yield return overlay.Show("practice-countdown");
            overlay.Hide();
            if (!manager.IsGameRunning)
                yield break;

            manager.ResumeNormalGameAfterScenario();

            // Previously practice lasted two rounds before the reminder plus six after it.
            // Finish one round earlier, while still letting the countdown reward arrive.
            int practiceTarget = manager.level + manager.shopController.LaunchesUntilFreeDraw + 1;
            int freeDrawCount = manager.shopController.FreeDrawCount;
            yield return new WaitUntil(() =>
                manager.shopController.FreeDrawCount > freeDrawCount ||
                manager.level >= practiceTarget ||
                !manager.IsGameRunning);
            if (!manager.IsGameRunning)
                yield break;

            if (manager.shopController.FreeDrawCount > freeDrawCount)
            {
                yield return new WaitUntil(() => manager.IsWaitingForLaunchInput || !manager.IsGameRunning);
                if (!manager.IsGameRunning)
                    yield break;

                yield return overlay.Show("free-draw-ready");
                overlay.Hide();
                manager.shopController.ClaimFreeDraw();
                yield return new WaitUntil(() => !MainMenu.Instance.InGamePanel.IsShowingGainBalls || !manager.IsGameRunning);
            }

            yield return new WaitUntil(() => manager.level >= practiceTarget || !manager.IsGameRunning);
            if (!manager.IsGameRunning)
                yield break;

            yield return new WaitUntil(() => manager.IsWaitingForLaunchInput || !manager.IsGameRunning);
            yield return overlay.Show("tutorial-complete");
            overlay.Hide();
            manager.ReturnToMainMenu();
        }

        private static Brick FindBrickPrefab<T>(GameManager manager) where T : Component
        {
            foreach (BrickProbability entry in manager.spawner.BrickPrefabs)
                if (entry != null && entry.Brick && entry.Brick.GetComponentInChildren<T>(true))
                    return entry.Brick;
            return null;
        }

        private static Brick FindBrickPrefab(GameManager manager, Type specialType)
        {
            foreach (BrickProbability entry in manager.spawner.BrickPrefabs)
            {
                if (entry == null || !entry.Brick)
                    continue;
                SpecialBrick special = entry.Brick.GetComponentInChildren<SpecialBrick>(true);
                if (specialType == null && !special)
                    return entry.Brick;
                if (specialType != null && special && specialType.IsInstanceOfType(special))
                    return entry.Brick;
            }
            return null;
        }
    }
}
