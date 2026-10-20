using System;
using System.Collections;
using System.Collections.Generic;
using BallzGame.Control;
using UnityEngine;
using Utils;

namespace BallzGame.Bricks.SpecialBricks
{

          public class TreeBrick : SpecialBrick, IBrickEffectControl
          {
               public SpriteRenderer shieldIcon;
               public GameObject physicsTrigger;
               public TriggerCollector triggers;

               public List<Brick> SurroundingBricks = new List<Brick>();

               public float DefenseInterval = 3f;
               public float WaitInterval = 5f;

               [Header("闪烁设置")]
               public int FlashCount = 3;
               public float FlashInterval = 0.12f;

               private Coroutine AddDefenseRoutine;
               private Coroutine effectRoutine;
               private bool automaticSchedulingEnabled = true;

               public bool AutomaticSchedulingEnabled => automaticSchedulingEnabled;
               public bool IsEffectActive => SurroundingBricks.Count > 0;

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



               private void Start()
               {
                    if (physicsTrigger != null)
                         physicsTrigger.SetActive(false);

                    if (shieldIcon != null)
                         shieldIcon.gameObject.SetActive(false);

                    if (automaticSchedulingEnabled)
                         StartEffect();
               }

               public void StartEffect()
               {
                    if (automaticSchedulingEnabled && effectRoutine == null && isActiveAndEnabled)
                         effectRoutine = StartCoroutine(BrickEffect());
               }

               public void SetAutomaticScheduling(bool enabled)
               {
                    automaticSchedulingEnabled = enabled;
                    if (enabled)
                         StartEffect();
                    else
                         Stop();
               }

               public void ActivateEffect()
               {
                    if (shieldIcon != null)
                         shieldIcon.gameObject.SetActive(true);
                    AddDefense();
               }

               public void DeactivateEffect()
               {
                    RemoveDefense();
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
                         physicsTrigger.SetActive(false);

                    RemoveDefense();
               }

               public IEnumerator AddingDefense()
               {
                    // 开启前闪烁
                    yield return StartCoroutine(FlashShield());


                    if (shieldIcon != null)
                         shieldIcon.gameObject.SetActive(true);


                    AddDefense();

                    // 持续防御
                    yield return new WaitForSeconds(DefenseInterval);

                    // 关闭前闪烁
                    yield return StartCoroutine(FlashShield());

                    // 关闭触发器
                    if (physicsTrigger != null)
                         physicsTrigger.SetActive(false);

                    RemoveDefense();
               }

               private IEnumerator FlashShield()
               {
                    if (shieldIcon == null)
                         yield break;

                    for (int i = 0; i < FlashCount; i++)
                    {
                         shieldIcon.gameObject.SetActive(false);
                         yield return new WaitForSeconds(FlashInterval);

                         shieldIcon.gameObject.SetActive(true);
                         yield return new WaitForSeconds(FlashInterval);
                    }
               }

               void AddDefense()
               {
                    foreach (var brick in GetSurroundingBricks())
                    {

                         if (brick && !SurroundingBricks.Contains(brick))
                         {
                              brick.SetDefence(brick.DefencePoint + 3);
                              SurroundingBricks.Add(brick);
                         }
                    }
               }

               void RemoveDefense()
               {
                    if (shieldIcon != null)
                         shieldIcon.gameObject.SetActive(false);

                    foreach (var brick in SurroundingBricks)
                    {
                         if (brick != null)
                         {
                              brick.SetDefence(
                                  brick.DefencePoint -
                                  Mathf.Clamp(3, 0, brick.DefencePoint)
                              );
                         }
                    }

                    SurroundingBricks.Clear();
               }

               private void OnDestroy()
               {
                    Stop();
               }

               private void OnDisable()
               {
                    Stop();
               }
          }
     }
