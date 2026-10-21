using System;
using System.Collections.Generic;
using BallzGame.Bricks;
using GameMeta;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BallzGame.Managers
{


    public class CrisisManager : MonoBehaviour
    {
        public List<CrisisBehaviour> Crisis;

        public void RefreshWaveProgress(int currentWave, int firstCrisisWave, int interval)
        {
            currentWave = Mathf.Max(1, currentWave);
            firstCrisisWave = Mathf.Max(1, firstCrisisWave);
            interval = Mathf.Max(1, interval);

            int previousWave = 1;
            int nextWave = firstCrisisWave;
            if (currentWave >= firstCrisisWave)
            {
                previousWave = firstCrisisWave +
                    ((currentWave - firstCrisisWave) / interval) * interval;
                nextWave = previousWave + interval;
            }

            float progress = Mathf.InverseLerp(previousWave, nextWave, currentWave);
            var menu = MainMenu.Instance;
            if (menu && menu.InGamePanel)
                menu.InGamePanel.SetCrisisProgress(currentWave, nextWave, progress);
        }

        [Header("Crisis row marks")]
        [Tooltip("World-space mark prefab. One instance is placed on each side of every crisis row.")]
        [SerializeField] private GameObject crisisMarkPrefab;
        [SerializeField] private Transform marksParent;
        [SerializeField, Min(0f)] private float markSideOffset = 1f;
        [SerializeField] private float markYOffset;
        [SerializeField] private float markZOffset;

        private sealed class MarkedRow
        {
            public readonly List<Brick> Bricks = new List<Brick>();
            public GameObject Left;
            public GameObject Right;
        }

        private readonly List<MarkedRow> markedRows = new List<MarkedRow>();
        private sealed class Encounter
        {
            public int RowsRemaining;
            public readonly List<Brick> Bricks = new List<Brick>();
            public bool HadBricks;
        }
        private readonly List<Encounter> encounters = new List<Encounter>();
        private Encounter spawningEncounter;

        public bool TryConsumeCompletedReward()
        {
            for (int i = 0; i < encounters.Count; i++)
            {
                var encounter = encounters[i];
                encounter.Bricks.RemoveAll(brick => !brick || brick.Hp <= 0);
                if (encounter.RowsRemaining > 0 || encounter.Bricks.Count > 0)
                    continue;
                encounters.RemoveAt(i--);
                if (encounter.HadBricks)
                    return true;
            }
            return false;
        }

        public void RegisterRow(IEnumerable<Brick> bricks)
        {
            var row = new MarkedRow();
            foreach (var brick in bricks)
                if (brick && brick.Hp > 0)
                    row.Bricks.Add(brick);

            if (spawningEncounter != null)
            {
                spawningEncounter.Bricks.AddRange(row.Bricks);
                spawningEncounter.HadBricks |= row.Bricks.Count > 0;
                if (--spawningEncounter.RowsRemaining <= 0)
                    spawningEncounter = null;
            }

            if (row.Bricks.Count == 0)
                return;

            markedRows.Add(row);
            UpdateMarks();
        }

        private void LateUpdate()
        {
            UpdateMarks();
            var manager = GameManager.Instance;
            if (manager)
                RefreshWaveProgress(manager.level, manager.FirstCrisisWave, manager.CrisisInterval);
        }

        private void UpdateMarks()
        {
            var manager = GameManager.Instance;
            if (!manager)
                return;

            for (int i = markedRows.Count - 1; i >= 0; i--)
            {
                var row = markedRows[i];
                row.Bricks.RemoveAll(brick => !brick || brick.Hp <= 0);
                if (row.Bricks.Count == 0)
                {
                    DestroyMarks(row);
                    markedRows.RemoveAt(i);
                    continue;
                }

                if (!crisisMarkPrefab)
                    continue;

                Transform parent = marksParent ? marksParent : transform;
                if (!row.Left)
                    row.Left = Instantiate(crisisMarkPrefab, parent);
                if (!row.Right)
                    row.Right = Instantiate(crisisMarkPrefab, parent);

                Vector3 position = row.Bricks[0].transform.position;
                float y = position.y + markYOffset;
                float z = position.z + markZOffset;
                row.Left.transform.position = new Vector3(-markSideOffset, y, z);
                row.Right.transform.position = new Vector3(manager.width - 1 + markSideOffset, y, z);
            }
        }

        public void ClearMarks()
        {
            encounters.Clear();
            spawningEncounter = null;
            if (Crisis != null)
                foreach (var behaviour in Crisis)
                    if (behaviour)
                        behaviour.CancelCrisis();
            foreach (var row in markedRows)
                DestroyMarks(row);
            markedRows.Clear();
        }

        private void DestroyMarks(MarkedRow row)
        {
            if (row.Left)
            {
                row.Left.SetActive(false);
                Destroy(row.Left);
            }
            if (row.Right)
            {
                row.Right.SetActive(false);
                Destroy(row.Right);
            }
        }

        private void OnDestroy()
        {
            ClearMarks();
        }

        private void Start()
        {
            GameManager.Instance.BeforeRowSpawn.AddListener(BeforeRowSpawn);
        }

        void BeforeRowSpawn()
        {

        }
        public int DoCrisis()
        {
            var index=Random.Range(0, Crisis.Count);
            spawningEncounter = new Encounter { RowsRemaining = Crisis[index].RowCount };
            encounters.Add(spawningEncounter);
            Crisis[index].DoCrisis();
            return Mathf.Max(1, Crisis[index].RowCount);
        }

        public int DoTutorialBigBrickCrisis()
        {
            SpecialBrickRush bigBrickCrisis = null;
            foreach (var crisisBehaviour in Crisis)
            {
                if (crisisBehaviour is SpecialBrickRush specialBrickRush)
                {
                    bigBrickCrisis = specialBrickRush;
                    break;
                }
            }

            if (!bigBrickCrisis)
            {
                Debug.LogWarning("Tutorial: no SpecialBrickRush is configured; falling back to a random Crisis.");
                return DoCrisis();
            }

            int rowCount = Mathf.Max(1, bigBrickCrisis.RowCount);
            spawningEncounter = new Encounter { RowsRemaining = rowCount };
            encounters.Add(spawningEncounter);
            bigBrickCrisis.DoBigBrickCrisis(3);
            return rowCount;
        }
    }

    public abstract class CrisisBehaviour : MonoBehaviour
    {
        public virtual int RowCount => 1;
        public virtual void CancelCrisis() { }
        public abstract void DoCrisis();

    }
}
