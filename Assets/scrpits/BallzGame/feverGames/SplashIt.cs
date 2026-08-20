using System;
using System.Collections;
using System.Collections.Generic;
using BallzGame.Bricks;
using BallzGame.Managers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BallzGame.Minigame
{
    public class SplashIt : IFeverGame
    {
        [Header("References")]
        [SerializeField] private LineRenderer lineRenderer;

        [Header("Trajectory")]
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private int maxBounces = 5;
        [SerializeField] private float skin = 0.01f;

        [Header("Damage")]
        [SerializeField] private float damageInterval = 1f;
        [SerializeField] private int damage = 1;

        [Header("Fever")]
        [SerializeField] private float duration = 10f;

        private EventTrigger area;

        private Coroutine dragCoroutine;
        private Coroutine damageCoroutine;
        private Coroutine gameCoroutine;

        // 当前水流碰到的 Brick
        private readonly HashSet<Brick> affectedBricks = new();

        // 当前鼠标 / 手指对应的世界坐标
        private Vector2 inputTarget;

        // 当前水流方向
        // 游戏开始默认向上
        private Vector2 currentDirection = Vector2.up;

        private bool gameActive;

        private Action onEnd;


        // =========================================================
        // 只保存 SplashIt 自己添加的 EventTrigger
        // 不影响 clickableArea 上其他系统的监听
        // =========================================================

        private EventTrigger.Entry pointerDownEntry;
        private EventTrigger.Entry dragEntry;
        private EventTrigger.Entry pointerUpEntry;


        // =========================================================
        // Start Game
        // =========================================================

        public override void StartGame(
            FeverGameContext context,
            Action onEnd)
        {
            this.onEnd = onEnd;

            gameActive = true;

            // 初始方向：正上方
            currentDirection = Vector2.up;


            // -----------------------------------------------------
            // 初始 inputTarget
            // -----------------------------------------------------

            Vector2 launcherPosition =
                GameManager.Instance.launcher.transform.position;

            inputTarget =
                launcherPosition + currentDirection;


            lineRenderer.positionCount = 0;


            // -----------------------------------------------------
            // 使用已有的 clickableArea
            // -----------------------------------------------------

            area =
                GameManager.Instance
                    .launcher
                    .clickableArea;


            SetupEventTrigger();


            // -----------------------------------------------------
            // 游戏一开始就显示水流
            // -----------------------------------------------------

            UpdateLine();


            // -----------------------------------------------------
            // 持续伤害
            // -----------------------------------------------------

            damageCoroutine =
                StartCoroutine(
                    DamageRoutine()
                );


            // -----------------------------------------------------
            // Fever 时间
            // -----------------------------------------------------

            gameCoroutine =
                StartCoroutine(
                    GameRoutine()
                );
        }


        // =========================================================
        // EventTrigger
        // =========================================================

        private void SetupEventTrigger()
        {
            if (area == null)
                return;


            // 不允许 Clear！
            //
            // clickableArea 上可能还有：
            // BallLauncher
            // BallTrajectoryPreview
            // 其他系统
            //
            // 所以这里只添加自己的监听。

            pointerDownEntry =
                AddEventTrigger(
                    EventTriggerType.PointerDown,
                    OnPointerDown
                );


            dragEntry =
                AddEventTrigger(
                    EventTriggerType.Drag,
                    OnDrag
                );


            pointerUpEntry =
                AddEventTrigger(
                    EventTriggerType.PointerUp,
                    OnPointerUp
                );
        }


        private EventTrigger.Entry AddEventTrigger(
            EventTriggerType type,
            UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            EventTrigger.Entry entry =
                new EventTrigger.Entry
                {
                    eventID = type
                };


            entry.callback.AddListener(
                callback
            );


            area.triggers.Add(entry);


            return entry;
        }


        private void RemoveEventTrigger()
        {
            if (area == null)
                return;


            // -----------------------------------------------------
            // 只删除自己的 Entry
            // -----------------------------------------------------

            if (pointerDownEntry != null)
            {
                area.triggers.Remove(
                    pointerDownEntry
                );

                pointerDownEntry = null;
            }


            if (dragEntry != null)
            {
                area.triggers.Remove(
                    dragEntry
                );

                dragEntry = null;
            }


            if (pointerUpEntry != null)
            {
                area.triggers.Remove(
                    pointerUpEntry
                );

                pointerUpEntry = null;
            }
        }


        // =========================================================
        // Input
        // =========================================================

        private void OnPointerDown(
            BaseEventData data)
        {
            if (!gameActive)
                return;


            // -----------------------------------------------------
            // 获取 PointerEventData
            //
            // PC:
            // 鼠标
            //
            // Android / iOS:
            // 手指
            // -----------------------------------------------------

            PointerEventData pointerData =
                data as PointerEventData;


            if (pointerData == null)
                return;


            // -----------------------------------------------------
            // 更新输入位置
            // -----------------------------------------------------

            UpdateInputTarget(
                pointerData
            );


            // -----------------------------------------------------
            // 更新水流方向
            // -----------------------------------------------------

            UpdateDirection();


            // -----------------------------------------------------
            // 防止重复创建 Drag Coroutine
            // -----------------------------------------------------

            if (dragCoroutine != null)
                return;


            dragCoroutine =
                StartCoroutine(
                    DragRoutine()
                );
        }


        private void OnDrag(
            BaseEventData data)
        {
            if (!gameActive)
                return;


            PointerEventData pointerData =
                data as PointerEventData;


            if (pointerData == null)
                return;


            // -----------------------------------------------------
            // 更新鼠标 / 手指位置
            // -----------------------------------------------------

            UpdateInputTarget(
                pointerData
            );


            // -----------------------------------------------------
            // 更新方向
            // -----------------------------------------------------

            UpdateDirection();
        }


        private void OnPointerUp(
            BaseEventData data)
        {
            if (dragCoroutine != null)
            {
                StopCoroutine(
                    dragCoroutine
                );

                dragCoroutine = null;
            }


            // 注意：
            // 松手以后不清除 Line
            //
            // currentDirection 保持最后一次方向
        }


        // =========================================================
        // Input Position
        // =========================================================

        private void UpdateInputTarget(
            PointerEventData pointerData)
        {
            if (pointerData == null)
                return;


            // -----------------------------------------------------
            // PointerEventData.position
            //
            // PC:
            // 鼠标当前位置
            //
            // Android:
            // 手指当前位置
            //
            // iOS:
            // 手指当前位置
            // -----------------------------------------------------

            Vector2 screenPosition =
                pointerData.position;


            // -----------------------------------------------------
            // Screen -> World
            // -----------------------------------------------------

            Vector3 worldPosition =
                GameManager.Instance
                    .MainCamera
                    .ScreenToWorldPoint(
                        new Vector3(
                            screenPosition.x,
                            screenPosition.y,
                            0f
                        )
                    );


            inputTarget =
                worldPosition;
        }


        // =========================================================
        // Update Direction
        // =========================================================

        private void UpdateDirection()
        {
            Vector2 launcherPosition =
                GameManager.Instance
                    .launcher
                    .transform
                    .position;


            Vector2 direction =
                inputTarget -
                launcherPosition;


            // 防止鼠标 / 手指刚好在 Launcher 中心
            if (direction.sqrMagnitude < 0.0001f)
                return;


            currentDirection =
                direction.normalized;
        }


        // =========================================================
        // Drag Coroutine
        // =========================================================

        private IEnumerator DragRoutine()
        {
            while (gameActive)
            {
                UpdateLine();

                yield return null;
            }
        }


        // =========================================================
        // Update Line
        // =========================================================

        private void UpdateLine()
        {
            // -----------------------------------------------------
            // 每次重新收集
            // -----------------------------------------------------

            affectedBricks.Clear();


            Vector2 currentPosition =
                GameManager.Instance
                    .launcher
                    .transform
                    .position;


            Vector2 direction =
                currentDirection;


            List<Vector3> points =
                new()
                {
                    currentPosition
                };


            for (
                int i = 0;
                i < maxBounces;
                i++
            )
            {
                RaycastHit2D hit =
                    GetSplashHit(
                        currentPosition,
                        direction,
                        maxDistance
                    );


                // =================================================
                // 什么都没撞到
                // =================================================

                if (hit.collider == null)
                {
                    points.Add(
                        currentPosition +
                        direction *
                        maxDistance
                    );

                    break;
                }


                // -------------------------------------------------
                // 画到碰撞点
                // -------------------------------------------------

                points.Add(
                    hit.point
                );


                // =================================================
                // Bottom
                // =================================================

                // Bottom 是特殊情况：
                // 即使 Bottom 是 Trigger，
                // 也必须结束水流

                if (
                    hit.collider.CompareTag(
                        "Bottom"
                    )
                )
                {
                    break;
                }


                // =================================================
                // Brick
                // =================================================

                Brick brick =
                    hit.collider.GetComponent<Brick>();


                if (brick != null)
                {
                    affectedBricks.Add(
                        brick
                    );
                }


                // =================================================
                // Bounce
                // =================================================

                direction =
                    Vector2.Reflect(
                        direction,
                        hit.normal
                    ).normalized;


                currentPosition =
                    hit.point +
                    direction * skin;
            }


            // -----------------------------------------------------
            // 更新 LineRenderer
            // -----------------------------------------------------

            lineRenderer.positionCount =
                points.Count;


            lineRenderer.SetPositions(
                points.ToArray()
            );
        }


        // =========================================================
        // 获取真正影响水流的 Collider
        // =========================================================
        //
        // Bottom
        //      ↓
        // 无论 Trigger / 非 Trigger
        // 都返回
        //
        // 普通 Trigger
        //      ↓
        // 忽略
        //
        // 非 Trigger
        //      ↓
        // 返回，产生反弹
        //
        // =========================================================

        private RaycastHit2D GetSplashHit(
            Vector2 origin,
            Vector2 direction,
            float distance)
        {
            RaycastHit2D[] hits =
                Physics2D.RaycastAll(
                    origin,
                    direction,
                    distance
                );


            foreach (
                RaycastHit2D hit
                in hits
            )
            {
                if (hit.collider == null)
                    continue;


                // -------------------------------------------------
                // Bottom 是特殊 Trigger
                // -------------------------------------------------

                if (
                    hit.collider.CompareTag(
                        "Bottom"
                    )
                )
                {
                    return hit;
                }


                // -------------------------------------------------
                // 其他 Trigger 全部忽略
                // -------------------------------------------------

                if (hit.collider.isTrigger)
                {
                    continue;
                }


                // -------------------------------------------------
                // 第一个非 Trigger Collider
                // -------------------------------------------------

                return hit;
            }


            return default;
        }


        // =========================================================
        // Damage
        // =========================================================

        private IEnumerator DamageRoutine()
        {
            while (gameActive)
            {
                yield return new WaitForSeconds(
                    damageInterval
                );


                if (!gameActive)
                    yield break;


                DamageBricks();
            }
        }


        private void DamageBricks()
        {
            foreach (
                Brick brick
                in affectedBricks
            )
            {
                if (brick == null)
                    continue;


                brick.TakeDamage(
                    damage,
                    this
                );
            }
        }


        // =========================================================
        // Fever Timer
        // =========================================================

        private IEnumerator GameRoutine()
        {
            yield return new WaitForSeconds(
                duration
            );


            EndFeverGame();
        }


        // =========================================================
        // End Fever Game
        // =========================================================

        private void EndFeverGame()
        {
            if (!gameActive)
                return;


            gameActive = false;


            // -----------------------------------------------------
            // 停止拖动 Coroutine
            // -----------------------------------------------------

            if (dragCoroutine != null)
            {
                StopCoroutine(
                    dragCoroutine
                );

                dragCoroutine = null;
            }


            // -----------------------------------------------------
            // 停止伤害 Coroutine
            // -----------------------------------------------------

            if (damageCoroutine != null)
            {
                StopCoroutine(
                    damageCoroutine
                );

                damageCoroutine = null;
            }


            // -----------------------------------------------------
            // 停止游戏 Timer
            // -----------------------------------------------------

            if (gameCoroutine != null)
            {
                StopCoroutine(
                    gameCoroutine
                );

                gameCoroutine = null;
            }


            // -----------------------------------------------------
            // 移除 SplashIt 自己的监听
            // 不影响 BallLauncher
            // -----------------------------------------------------

            RemoveEventTrigger();


            // -----------------------------------------------------
            // 清理水流
            // -----------------------------------------------------

            ClearLine();


            // -----------------------------------------------------
            // 通知 Fever Game
            // -----------------------------------------------------

            onEnd?.Invoke();

            onEnd = null;
        }


        // =========================================================
        // Cleanup
        // =========================================================

        private void ClearLine()
        {
            lineRenderer.positionCount = 0;

            affectedBricks.Clear();
        }
    }
}