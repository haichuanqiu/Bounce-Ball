using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace BallzGame.Bricks.SpecialBricks
{


    public class TreeBrick : SpecialBrick
    {
        public SpriteRenderer TreeIcon;
        public TriggerCollector triggers;
        public List<Brick> SurroundingBricks = new List<Brick>();
        public float DefenseInterval;
        public float WaitInterval;
        Coroutine AddDefenseRoutine;
        private Coroutine effectRoutine;

        public IEnumerator BrickEffect()
        {
            while (true)
            {
                yield return new WaitForSeconds(WaitInterval);
                AddDefenseRoutine = StartCoroutine(AddingDefense());

                // 等待这个协程执行完
                yield return AddDefenseRoutine;
                AddDefenseRoutine = null;

            }
        }

        public override void OnMiniGameStart()
        {
            base.OnMiniGameStart();
            Stop();
        }

        override public void OnMiniGameEnd()
        {
            base.OnMiniGameEnd();
            StartEffect();
        }

        private void Start()
        {
            StartEffect();
        }

        public void StartEffect()
        {
            if (effectRoutine == null && isActiveAndEnabled)
                effectRoutine = StartCoroutine(BrickEffect());
        }

        public void Stop()
        {
            if (effectRoutine != null)
            {
                StopCoroutine(effectRoutine);
                effectRoutine = null;
            }
            if (AddDefenseRoutine != null)
            {
                StopCoroutine(AddDefenseRoutine);
                AddDefenseRoutine = null;
            }
            RemoveDefense();
        }

        public IEnumerator AddingDefense()
        {
            TreeIcon.gameObject.SetActive(true);
            yield return new WaitForFixedUpdate();
            AddDefense();
            yield return new WaitForSeconds(DefenseInterval);
            RemoveDefense();
        }

        void AddDefense()
        {

            foreach (var VARIABLE in triggers.Colliders)
            {
                if (!VARIABLE)
                    continue;
                var script = VARIABLE.GetComponent<Brick>();
                if (script && !SurroundingBricks.Contains(script))
                {
                    script.SetDefence(script.DefencePoint + 3);
                    SurroundingBricks.Add(script);
                }
            }
        }

        private void OnDestroy()
        {
            Stop();
        }

        void RemoveDefense()
        {
            if (TreeIcon != null)
                TreeIcon.gameObject.SetActive(false);
            foreach (var brick in SurroundingBricks)
            {
                if (brick != null)
                {

                    brick.SetDefence(brick.DefencePoint - Mathf.Clamp(3, 0, brick.DefencePoint));
                }
            }

            SurroundingBricks.Clear();
        }
    }
}
