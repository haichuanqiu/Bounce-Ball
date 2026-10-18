using System.Collections.Generic;
using BallzGame.Bricks;
using UnityEngine;
using UnityEngine.Serialization;

namespace BallzGame.Managers
{
    public class BrickSpawner : MonoBehaviour
    {
        public List<BrickProbability> BrickPrefabs;

        private int totalProbability;
        public float SpawnPossibility=0.7f;

        [Header("HP balance")]
        [Tooltip("Provisional q: average effective damage per ball per launch without purchased buffs. Replace with measured data.")]
        [SerializeField, Min(0.01f)] private float averageDamagePerBall = 3f;
        [Tooltip("Reference normal-wave density. Keep fixed when special waves change SpawnPossibility.")]
        [SerializeField, Range(0.01f, 1f)] private float normalSpawnPossibility = 0.7f;
        // Retain old serialized data; the continuous curve replaces stage-based pressure.
        [SerializeField, HideInInspector] private int wavesPerPressureStage = 9;
        [SerializeField, HideInInspector] private float[] pressureCoefficients = { 0.8f, 0.9f, 1f, 1.1f, 1.2f };
        [SerializeField, Min(0.01f)] private float startingPressure = 0.8f;
        [SerializeField, Min(0f)] private float pressureGrowthPerWave = 0.025f;
        [Tooltip("Second derivative of pressure per wave. Defaults target pressure 1.5 at wave 13 and 3 at wave 25.")]
        [SerializeField, Min(0f)] private float pressureAcceleration = 1f / 180f;

        void Start()
        {
            NormalizedBrickPossibility();
        }

        [ContextMenu("NormalizedBrickPossibility")]
        void NormalizedBrickPossibility()
        {
            totalProbability = 0;

            foreach (var item in BrickPrefabs)
            {
                totalProbability += item.Probability;
            }


            // 如果不是100，自动归一化
            if (totalProbability != 100)
            {
                foreach (var item in BrickPrefabs)
                {
                    item.Probability =
                        Mathf.RoundToInt(
                            item.Probability * 100f / totalProbability
                        );
                }
            }


            totalProbability = 0;

            foreach (var item in BrickPrefabs)
            {
                totalProbability += item.Probability;
            }
        }


        Brick GetRandomBrick()
        {
            int random = Random.Range(0, totalProbability);

            int current = 0;

            foreach (var item in BrickPrefabs)
            {
                current += item.Probability;

                if (random < current)
                {
                    return item.Brick;
                }
            }


            // 防止没有return
            return BrickPrefabs[0].Brick;
        }

        public List<Brick> NextBricks;
        public List<int> NextBricksHP;
        public List<Brick> SpawnRow(int level, int width)
        {
            var parent = GameManager.Instance.BricksParent;
            var bricks = new List<Brick>();

            // 是否有预设砖块
            bool useNextBricks = NextBricks.Count == width;

            for (int x = 0; x < width; x++)
            {
                if (useNextBricks)
                {
                    // 使用预设砖块
                    Brick prefab = NextBricks[x];

                    if (prefab)
                    {
                        var script = Instantiate(
                            prefab,
                            new Vector3(x, 0, 0),
                            Quaternion.identity,
                            parent
                        );

                        bricks.Add(script);
                        int hp;
                        if (NextBricksHP.Count==width)
                        {
                            hp = Mathf.Max(1, NextBricksHP[x]);
                        }
                        else
                        {
                            hp = GetRandomHPByLevel(level, width);
                        }
                        script.Init(hp);
                    }
                    else
                    {
                        bricks.Add(null);
                    }
                }
                else
                {
                    // 原来的随机逻辑
                    if (Random.value < SpawnPossibility)
                    {
                        Brick prefab = GetRandomBrick();

                        var script = Instantiate(
                            prefab,
                            new Vector3(x, 0, 0),
                            Quaternion.identity,
                            parent
                        );

                        bricks.Add(script);
                        script.Init(GetRandomHPByLevel(level, width));
                    }
                    else
                    {
                        bricks.Add(null);
                    }
                }
            }

            // 预设砖块只使用一次
            if (useNextBricks)
            {
                NextBricks.Clear();
            }
            NextBricksHP.Clear();

            return bricks;
        }
        public int GetRandomHPByLevel(int level)
        {
            int width = GameManager.Instance != null ? GameManager.Instance.width : 13;
            return GetRandomHPByLevel(level, width);
        }

        public float GetAverageHPByLevel(int level, int width)
        {
            int wave = Mathf.Max(1, level);
            float pressure = GetPressureByLevel(wave);

            // Scheduled ball growth, independent of actual claims, random results or purchased buffs.
            int rewardInterval = GameManager.Instance != null && GameManager.Instance.shopController != null
                ? GameManager.Instance.shopController.LaunchesPerFreeDraw : 6;
            float scheduledBallCount = 6f + 6f * ((wave - 1) / rewardInterval);
            float expectedBrickCount = Mathf.Max(1, width) * Mathf.Clamp(normalSpawnPossibility, 0.01f, 1f);
            return Mathf.Max(1f, Mathf.Max(0.01f, pressure) *
                Mathf.Max(0.01f, averageDamagePerBall) * scheduledBallCount / expectedBrickCount);
        }

        public float GetPressureByLevel(int level)
        {
            float elapsedWaves = Mathf.Max(0, level - 1);
            return Mathf.Max(0.01f, startingPressure) +
                Mathf.Max(0f, pressureGrowthPerWave) * elapsedWaves +
                0.5f * Mathf.Max(0f, pressureAcceleration) * elapsedWaves * elapsedWaves;
        }

        public int GetRandomHPByLevel(int level, int width)
        {
            float averageHP = GetAverageHPByLevel(level, width);
            int lowerHP = Mathf.FloorToInt(averageHP);
            return lowerHP + (Random.value < averageHP - lowerHP ? 1 : 0);
        }
    }

    [System.Serializable]
    public class BrickProbability
    {
        public Brick Brick;
        public int Probability;
    }
}
