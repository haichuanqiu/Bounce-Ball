using System.Collections;
using System.Collections.Generic;
using BallzGame.Managers;
using UnityEngine;
using Utils;

namespace BallzGame.Bricks.SpecialBricks
{
    public class HealBrick : SpecialBrick

    {
        public TriggerCollector triggers;
          [SerializeField] SpriteRenderer rd;
        public int HealAmount = 3;
        private bool activated;
          [SerializeField] private Sprite NotActivate;
          [SerializeField] private Sprite Activated;

          [SerializeField]
        private int moveCount;

        public override void OnRowMoved()
        {
            base.OnRowMoved();
            moveCount += 1;
            if (moveCount == 1)
            {
                    rd.sprite = Activated;
                //HealVisual.SetActive(true);
            }
            else if (moveCount > 1)
            {

                StartCoroutine(Heal());
            }

        }

        public override void OnMiniGameStart()
        {
            base.OnMiniGameStart();

        }

        

        public IEnumerator Heal()
        {
            AddHP();
            yield break;
        }



        void AddHP()
        {
            foreach (var script in GetSurroundingBricks())
            {
                if (script)
                {
                    GameManager.DoHealText(script.transform.position, HealAmount);
                    script.AddHP(HealAmount);
                }
            }
        }
    }

}
