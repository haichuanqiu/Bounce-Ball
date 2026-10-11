using System;
using System.Collections.Generic;
using BallzGame.Bricks.SpecialBricks;
using BallzGame.Effects;
using BallzGame.Managers;
using UnityEngine;
using UnityEngine.Events;
using Object = System.Object;

namespace BallzGame.Bricks
{
    public class Brick : MonoBehaviour
    {
        public int Hp = 3;

        public Action OnDestroy;
        public int DefencePoint;
        public int OriginalHp;
        public BrickVisualEffects VisualEffect;
        public BrickPoint PointPrefab;
        public SpecialBrick SpecialBrick;
        public List<BrickStatus> Status;
        public UnityEvent<bool,object> OnHit;
        void Start()
        {
            VisualEffect.UpdateHPText(Hp, OriginalHp);
            SetDefence(DefencePoint);
        }

        public void OnRowMoved()
        {
            SpecialBrick?.OnRowMoved();
        }

        public void OnMiniGameStart()
        {
            SpecialBrick?.OnMiniGameStart();
        }

        public void OnMiniGameEnd()
        {
            SpecialBrick?.OnMiniGameEnd();
        }

        public void SetDefence(int defence)
        {
            DefencePoint = defence;
            VisualEffect.SetShieldEffect(defence > 0);
        }

        public void AddHP(int add)
        {
            Hp += add;
            VisualEffect.UpdateHPText(Hp, OriginalHp);
        }

        // 被 Ball 调用
        public void TakeDamage(int damage,Object sender ,Vector2 force =new Vector2())
        {
            int originalDefense = DefencePoint;
            int remainingDamage = damage;

            // 先扣护盾
            if (DefencePoint > 0)
            {
                if (DefencePoint >= remainingDamage)
                {
                    SetDefence(DefencePoint - remainingDamage);
                    remainingDamage = 0;
                }
                else
                {
                    remainingDamage -= DefencePoint;
                    SetDefence(0);
                }

                int shieldLoss = originalDefense - DefencePoint;
                if (shieldLoss > 0)
                {
                    VisualEffect.ShowFloatText(transform.position, shieldLoss);
                }
            }

            // 再扣血
           var alive = true;
            if (remainingDamage > 0)
            {
                Hp -= remainingDamage;
                VisualEffect.UpdateHPText(Hp, OriginalHp);

                // 只有真正掉血才触发受击效果
                VisualEffect.DoHit(force);

                if (Hp <= 0)
                {
                    alive=false;
                    Die(force);
                }
            }
            OnHit.Invoke(alive,sender);
            SpecialBrick?.OnHit(alive,sender);
        }

        public void Die(Vector2 force)
        {
            VisualEffect.MakeBreakEffect();

            // 得分预制体由 Brick 自己实例化
            if (PointPrefab != null)
            {
                BrickPoint point = Instantiate(PointPrefab, transform.position, Quaternion.identity,
                    GameManager.Instance.VisualEffectsParent);
                point.Init(transform.position, force);
            }

            GameManager.Instance.feverController.AddFeverPoints(1);
            GameManager.Instance.AddCurrentResultPoints(OriginalHp);
            GameManager.Instance.CurrentResult.BricksCount += 1;
            OnDestroy?.Invoke();

            Destroy(gameObject);
        }

        public void Init(int i)
        {
            Hp = i;
            OriginalHp = Hp;
            VisualEffect.UpdateHPText(Hp, OriginalHp);
        }
        public enum BrickStatus
        {
            DisableEffect
        }

        public void AddStatus(BrickStatus status)
        {
            Status.Add(status);
        }

        public bool removeStatus(BrickStatus status)
        {
           return Status.Remove(status);
        }
    }
}

