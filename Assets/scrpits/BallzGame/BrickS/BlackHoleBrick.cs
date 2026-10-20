using System.Collections;
using System.Collections.Generic;
using BallzGame.Bricks;
using BallzGame.Bricks.SpecialBricks;
using UnityEngine;
using Utils;

public class BlackHoleBrick : SpecialBrick
{
     [SerializeField] private SpriteRenderer BlackholeSprite;
     [SerializeField] private GameObject physicsTrigger;
     [SerializeField] private TriggerCollector triggers;

     public List<Brick> SurroundingBricks = new List<Brick>();

     [SerializeField] private float DefenseInterval = 3f;
     [SerializeField] private float WaitInterval = 5f;

     [Header("闪烁设置")]
     [SerializeField] private int FlashCount = 3;
     [SerializeField] private float FlashInterval = 0.12f;

     private Coroutine AddDefenseRoutine;
     private Coroutine effectRoutine;

     public IEnumerator BrickEffect()
     {
          while (true)
          {
               yield return new WaitForSeconds(WaitInterval);

               AddDefenseRoutine = StartCoroutine(AddingDefense());
               yield return AddDefenseRoutine;

               AddDefenseRoutine = null;
          }
     }

     public override void OnMiniGameStart()
     {
          base.OnMiniGameStart();
          Stop();
     }

     public override void OnMiniGameEnd()
     {
          base.OnMiniGameEnd();
          StartEffect();
     }

     private void Start()
     {
          if (physicsTrigger != null)
               physicsTrigger.gameObject.SetActive(false);

          if (BlackholeSprite != null)
               BlackholeSprite.gameObject.SetActive(false);

          StartEffect();
     }

     public void StartEffect()
     {
          if (effectRoutine == null && isActiveAndEnabled)
          {
               effectRoutine = StartCoroutine(BrickEffect());
          }
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

          if (physicsTrigger != null)
               physicsTrigger.gameObject.SetActive(false);

          RemoveDefense();
     }

     public IEnumerator AddingDefense()
     {
          // 开启前闪烁
          yield return StartCoroutine(FlashBlackhole());


          // 黑洞图像保持显示
          if (BlackholeSprite != null)
               BlackholeSprite.gameObject.SetActive(true);


          // 添加禁用效果
          AddDefense();

          // 持续时间
          yield return new WaitForSeconds(DefenseInterval);

          // 关闭前闪烁
          yield return StartCoroutine(FlashBlackhole());

          // 关闭物理触发器
          if (physicsTrigger != null)
               physicsTrigger.gameObject.SetActive(false);

          // 移除禁用效果
          RemoveDefense();
     }

     private IEnumerator FlashBlackhole()
     {
          if (BlackholeSprite == null)
               yield break;

          for (int i = 0; i < FlashCount; i++)
          {
               BlackholeSprite.gameObject.SetActive(false);
               yield return new WaitForSeconds(FlashInterval);

               BlackholeSprite.gameObject.SetActive(true);
               yield return new WaitForSeconds(FlashInterval);
          }
     }

     void AddDefense()
     {
          foreach (var brick in GetSurroundingBricks())
          {

               if (brick && !SurroundingBricks.Contains(brick))
               {
                    brick.AddStatus(Brick.BrickStatus.DisableEffect);
                    SurroundingBricks.Add(brick);
               }
          }
     }

     void RemoveDefense()
     {
          if (BlackholeSprite != null)
               BlackholeSprite.gameObject.SetActive(false);

          foreach (var brick in SurroundingBricks)
          {
               if (brick != null)
               {
                    brick.removeStatus(
                        Brick.BrickStatus.DisableEffect
                    );
               }
          }

          SurroundingBricks.Clear();
     }

     private void OnDisable()
     {
          Stop();
     }

     private void OnDestroy()
     {
          Stop();
     }
}

