using System.Collections;
using System.Collections.Generic;
using BallzGame.Bricks;
using BallzGame.Bricks.SpecialBricks;
using UnityEngine;
using Utils;

public class BlackHoleBrick : SpecialBrick
    {
        [SerializeField]private SpriteRenderer BlackholeSprite;
        [SerializeField]private TriggerCollector triggers;
        public List<Brick> SurroundingBricks = new List<Brick>();
        [SerializeField]private float DefenseInterval;
        [SerializeField]private float WaitInterval;
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
            BlackholeSprite.gameObject.SetActive(true);
            yield return new WaitForFixedUpdate();
            AddDefense();
            yield return new WaitForSeconds(DefenseInterval);
            RemoveDefense();
        }

        void AddDefense()
        {

            foreach (var VARIABLE in triggers.Colliders)
            {
                if(!VARIABLE)
                    continue;
                var script = VARIABLE.GetComponent<Brick>();
                if (script && !SurroundingBricks.Contains(script))
                {
                    script.AddStatus(Brick.BrickStatus.DisableEffect);
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
            if (BlackholeSprite != null)
                BlackholeSprite.gameObject.SetActive(false);
            foreach (var brick in SurroundingBricks)
            {
                if (brick != null)
                {

                    brick.removeStatus(Brick.BrickStatus.DisableEffect);
                }
            }

            SurroundingBricks.Clear();
        }
    }
