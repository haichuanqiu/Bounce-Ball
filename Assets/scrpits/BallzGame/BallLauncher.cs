using System.Collections;
using System.Collections.Generic;
using BallzGame.Balls;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BallzGame.Managers
{
    public class BallLauncher : MonoBehaviour
    {
        [SerializeField]
        private List<Ball> ballPrefabs;

        public Ball InitialBall;

        public float ElapsedTimeSinceLaunch;

        public EventTrigger clickableArea;

        private int activeBalls;

        private Coroutine timer;

        public bool HasInput;

        private Vector2 inputTarget;

        public int BallCount;

        public Dictionary<BallData, int> ballDatas = new();

        public Button SkipButton;


        // =========================================================
        // Skip
        // =========================================================

        private bool SkipPressed;


        // 已经发射出去、目前还在场上的 Ball
        private List<Ball> launchedBalls =
            new List<Ball>();


        // 当前正在被 Skip 收回的 Ball 数量
        private int returningBalls;


        // Skip 收球协程
        private Coroutine interruptCoroutine;


        // =========================================================
        // Unity
        // =========================================================

        private void Awake()
        {
            SetupEventTrigger();

            if (SkipButton != null)
            {
                SkipButton.onClick.AddListener(
                    InterruptLaunch
                );
            }
        }


        // =========================================================
        // EventTrigger
        // =========================================================

        private void SetupEventTrigger()
        {
            if (clickableArea == null)
                return;


            var entry =
                new EventTrigger.Entry();


            entry.eventID =
                EventTriggerType.PointerUp;


            entry.callback.AddListener(
                (data) =>
                {
                    // PointerEventData 同时支持：
                    // PC Mouse
                    // Android Touch
                    // iOS Touch

                    PointerEventData pointerData =
                        data as PointerEventData;

                    if (pointerData != null)
                    {
                        OnRelease(pointerData);
                    }
                }
            );


            clickableArea.triggers.Add(entry);
        }


        // =========================================================
        // Input
        // =========================================================

        // 鼠标和触屏统一走这里
        private void OnRelease(
            PointerEventData eventData)
        {
            if (eventData == null)
                return;


            // -----------------------------------------------------
            // PointerEventData.position
            //
            // PC:
            // 鼠标位置
            //
            // Android / iOS:
            // 手指位置
            // -----------------------------------------------------

            Vector2 screenPosition =
                eventData.position;


            // -----------------------------------------------------
            // Screen -> World
            // -----------------------------------------------------

            Vector3 worldPosition =
                GameManager.Instance.MainCamera
                    .ScreenToWorldPoint(
                        new Vector3(
                            screenPosition.x,
                            screenPosition.y,
                            0f
                        )
                    );


            // -----------------------------------------------------
            // 限制必须向上发射
            // -----------------------------------------------------

            if (
                worldPosition.y >=
                transform.position.y + 0.02f
            )
            {
                inputTarget =
                    worldPosition;

                HasInput = true;
            }
        }


        public void WaitForInput()
        {
            HasInput = false;
        }


        public void StopListenToInput()
        {
            HasInput = false;
        }


        // =========================================================
        // Timer
        // =========================================================

        public IEnumerator BounceTimer()
        {
            ElapsedTimeSinceLaunch = 0;


            while (true)
            {
                ElapsedTimeSinceLaunch +=
                    Time.fixedDeltaTime;


                yield return
                    new WaitForFixedUpdate();
            }
        }


        // =========================================================
        // Skip
        // =========================================================

        private void InterruptLaunch()
        {
            // 防止重复点击
            if (SkipPressed)
                return;


            SkipPressed = true;


            // 停止输入
            HasInput = false;


            // 如果之前还有收球协程
            if (interruptCoroutine != null)
            {
                StopCoroutine(
                    interruptCoroutine
                );
            }


            // 开始收回已经发射出去的球
            interruptCoroutine =
                StartCoroutine(
                    InterruptBalls()
                );
        }


        // =========================================================
        // Interrupt Balls
        // =========================================================

        private IEnumerator InterruptBalls()
        {
            // -----------------------------------------------------
            // 停止计时器
            // -----------------------------------------------------

            if (timer != null)
            {
                StopCoroutine(timer);

                timer = null;
            }


            // -----------------------------------------------------
            // 目标位置
            // -----------------------------------------------------

            Vector3 targetPosition =
                transform.position;


            // -----------------------------------------------------
            // 复制当前已经发射出去的球
            // -----------------------------------------------------

            List<Ball> ballsToReturn =
                new List<Ball>(
                    launchedBalls
                );


            returningBalls = 0;


            // -----------------------------------------------------
            // 开始收回所有 Ball
            // -----------------------------------------------------

            foreach (Ball ball in ballsToReturn)
            {
                if (ball == null)
                    continue;


                returningBalls++;


                StartCoroutine(
                    ReturnBallAndDestroy(
                        ball,
                        targetPosition
                    )
                );
            }


            // -----------------------------------------------------
            // 这些球不再等待正常返回
            // -----------------------------------------------------

            activeBalls = 0;


            launchedBalls.Clear();


            // -----------------------------------------------------
            // ★ 等待所有 Ball 真正 Destroy
            // -----------------------------------------------------

            while (returningBalls > 0)
            {
                yield return null;
            }


            interruptCoroutine = null;
        }


        // =========================================================
        // Return Ball And Destroy
        // =========================================================

        private IEnumerator ReturnBallAndDestroy(
            Ball ball,
            Vector3 targetPosition)
        {
            if (ball == null)
            {
                returningBalls =
                    Mathf.Max(
                        0,
                        returningBalls - 1
                    );

                yield break;
            }


            Transform ballTransform =
                ball.transform;


            // -----------------------------------------------------
            // 关闭所有 Collider2D
            // -----------------------------------------------------

            Collider2D[] colliders =
                ball.GetComponentsInChildren<Collider2D>(
                    true
                );


            foreach (
                Collider2D collider
                in colliders
            )
            {
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }


            // -----------------------------------------------------
            // 停止所有 Rigidbody2D
            // -----------------------------------------------------

            Rigidbody2D[] rigidbodies =
                ball.GetComponentsInChildren<Rigidbody2D>(
                    true
                );


            foreach (
                Rigidbody2D rb
                in rigidbodies
            )
            {
                if (rb != null)
                {
                    rb.linearVelocity =
                        Vector2.zero;

                    rb.angularVelocity = 0f;

                    rb.simulated = false;
                }
            }


            // -----------------------------------------------------
            // 开始位置
            // -----------------------------------------------------

            Vector3 startPosition =
                ballTransform.position;


            float elapsed = 0f;

            const float duration = 0.5f;


            // -----------------------------------------------------
            // 0.5 秒回到 Launcher
            // -----------------------------------------------------

            while (elapsed < duration)
            {
                if (ball == null)
                {
                    returningBalls =
                        Mathf.Max(
                            0,
                            returningBalls - 1
                        );

                    yield break;
                }


                elapsed += Time.deltaTime;


                float t =
                    Mathf.Clamp01(
                        elapsed / duration
                    );


                // SmoothStep
                t = Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


                ballTransform.position =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        t
                    );


                yield return null;
            }


            // -----------------------------------------------------
            // 最终位置
            // -----------------------------------------------------

            if (ball != null)
            {
                ballTransform.position =
                    targetPosition;


                Destroy(
                    ball.gameObject
                );
            }


            // -----------------------------------------------------
            // Ball 收回完成
            // -----------------------------------------------------

            returningBalls =
                Mathf.Max(
                    0,
                    returningBalls - 1
                );
        }


        // =========================================================
        // Launch
        // =========================================================

        public IEnumerator Launch()
        {
            SkipPressed = false;


            launchedBalls.Clear();

            returningBalls = 0;


            // -----------------------------------------------------
            // 发射方向
            // -----------------------------------------------------

            var target =
                inputTarget;


            // -----------------------------------------------------
            // Timer
            // -----------------------------------------------------

            timer =
                StartCoroutine(
                    BounceTimer()
                );


            Transform parent =
                GameManager.Instance
                    .BallsParent;


            Vector2 dir =
                (
                    target -
                    (Vector2)transform.position
                ).normalized;


            activeBalls = 0;


            // =====================================================
            // 发射 Ball
            // =====================================================

            for (
                int i = 0;
                i < ballPrefabs.Count &&
                !SkipPressed;
                i++
            )
            {
                var prefab =
                    ballPrefabs[i];


                var obj =
                    Instantiate(
                        prefab,
                        transform.position,
                        Quaternion.identity,
                        parent
                    );


                obj.Init(
                    this,
                    dir,
                    GameManager.Instance
                        .BallConfig
                );


                activeBalls++;


                launchedBalls.Add(obj);


                // 发射间隔
                yield return
                    new WaitForSeconds(
                        GameManager.Instance
                            .BallConfig
                            .ShootInterval
                    );
            }


            // =====================================================
            // Skip
            // =====================================================

            if (SkipPressed)
            {
                // ★ 等所有球收回并 Destroy
                while (
                    interruptCoroutine != null
                )
                {
                    yield return null;
                }


                if (timer != null)
                {
                    StopCoroutine(timer);

                    timer = null;
                }


                yield break;
            }


            // =====================================================
            // 正常情况
            // 等所有 Ball 返回
            // =====================================================

            while (activeBalls > 0)
            {
                yield return null;
            }


            // =====================================================
            // 停止 Timer
            // =====================================================

            if (timer != null)
            {
                StopCoroutine(timer);

                timer = null;
            }
        }


        // =========================================================
        // Ball Returned
        // =========================================================

        public void OnBallReturned(Ball ball)
        {
            if (ball != null)
            {
                launchedBalls.Remove(ball);
            }


            activeBalls =
                Mathf.Max(
                    0,
                    activeBalls - 1
                );
        }


        // =========================================================
        // Add Balls
        // =========================================================

        public void AddBalls(
            List<Ball> balls)
        {
            if (balls == null)
                return;


            if (ballPrefabs == null)
            {
                ballPrefabs =
                    new List<Ball>();
            }


            foreach (
                Ball ball
                in balls
            )
            {
                if (ball == null)
                    continue;


                ballPrefabs.Add(ball);


                if (
                    !ballDatas.TryAdd(
                        ball.Data,
                        1
                    )
                )
                {
                    ballDatas[
                        ball.Data
                    ]++;
                }
            }


            BallCount +=
                balls.Count;
        }


        // =========================================================
        // Reset
        // =========================================================

        public void Reset()
        {
            // -----------------------------------------------------
            // 停止 Skip 收球
            // -----------------------------------------------------

            if (
                interruptCoroutine != null
            )
            {
                StopCoroutine(
                    interruptCoroutine
                );

                interruptCoroutine = null;
            }


            // -----------------------------------------------------
            // 清理状态
            // -----------------------------------------------------

            launchedBalls.Clear();

            returningBalls = 0;


            // -----------------------------------------------------
            // Ball Prefabs
            // -----------------------------------------------------

            if (ballPrefabs == null)
            {
                ballPrefabs =
                    new List<Ball>();
            }
            else
            {
                ballPrefabs.Clear();
            }


            BallCount = 0;

            ballDatas.Clear();


            // -----------------------------------------------------
            // Initial Ball
            // -----------------------------------------------------

            if (InitialBall != null)
            {
                ballPrefabs.Add(
                    InitialBall
                );


                if (
                    !ballDatas.TryAdd(
                        InitialBall.Data,
                        1
                    )
                )
                {
                    ballDatas[
                        InitialBall.Data
                    ]++;
                }
            }


            // -----------------------------------------------------
            // Timer
            // -----------------------------------------------------

            if (timer != null)
            {
                StopCoroutine(timer);

                timer = null;
            }


            StopAllCoroutines();


            // -----------------------------------------------------
            // Reset
            // -----------------------------------------------------

            activeBalls = 0;

            ElapsedTimeSinceLaunch = 0f;

            HasInput = false;

            SkipPressed = false;

            interruptCoroutine = null;

            returningBalls = 0;
        }
    }
}