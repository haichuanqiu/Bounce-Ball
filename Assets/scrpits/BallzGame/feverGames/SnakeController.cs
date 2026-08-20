using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BallzGame.Minigame.SnakeGame
{
    public class SnakeController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveInterval = 0.15f;

        private Vector2Int direction = Vector2Int.right;
        private Vector2Int nextDirection;

        private FeverGameContext context;

        private Vector2Int currentPosition;

        private bool active;

        private bool hasMoved;

        private Coroutine moveCoroutine;

        public Action<Vector2Int> OnMoveToGrid;


        // =========================================================
        // Snake Visual
        // =========================================================

        public GameObject SnakeHeadVisual;


        // =========================================================
        // UI Buttons
        // =========================================================

        [Header("Direction Buttons")]
        public Button Up;
        public Button Down;
        public Button Left;
        public Button Right;


        // =========================================================
        // Tail System
        // =========================================================

        public GameObject SnakeTailPrefab;


        // 只保存尾巴，不包含头
        private List<Vector2Int> tailPositions =
            new List<Vector2Int>();


        // 保存生成出来的尾巴物体
        private List<GameObject> tailVisuals =
            new List<GameObject>();


        // 外部查看撞尾
        // 不包含头
        public IReadOnlyList<Vector2Int> TailPositions
        {
            get
            {
                return tailPositions;
            }
        }


        // =========================================================
        // Unity
        // =========================================================

        private void Awake()
        {
            SetupButtons();
        }


        private void Update()
        {
            if (!active)
                return;

            HandleKeyboardInput();
        }


        // =========================================================
        // Button Setup
        // =========================================================

        private void SetupButtons()
        {
            if (Up != null)
            {
                Up.onClick.AddListener(
                    OnUpButton
                );
            }


            if (Down != null)
            {
                Down.onClick.AddListener(
                    OnDownButton
                );
            }


            if (Left != null)
            {
                Left.onClick.AddListener(
                    OnLeftButton
                );
            }


            if (Right != null)
            {
                Right.onClick.AddListener(
                    OnRightButton
                );
            }
        }


        private void OnDestroy()
        {
            if (Up != null)
            {
                Up.onClick.RemoveListener(
                    OnUpButton
                );
            }


            if (Down != null)
            {
                Down.onClick.RemoveListener(
                    OnDownButton
                );
            }


            if (Left != null)
            {
                Left.onClick.RemoveListener(
                    OnLeftButton
                );
            }


            if (Right != null)
            {
                Right.onClick.RemoveListener(
                    OnRightButton
                );
            }
        }


        // =========================================================
        // Button Input
        // =========================================================

        private void OnUpButton()
        {
            SetDirection(
                Vector2Int.down
            );
        }


        private void OnDownButton()
        {
            SetDirection(
                Vector2Int.up
            );
        }


        private void OnLeftButton()
        {
            SetDirection(
                Vector2Int.left
            );
        }


        private void OnRightButton()
        {
            SetDirection(
                Vector2Int.right
            );
        }


        // =========================================================
        // 设置方向
        // =========================================================

        private void SetDirection(
            Vector2Int newDirection)
        {
            if (!active)
                return;


            // -----------------------------------------------------
            // 防止直接 180° 掉头
            //
            // 例如：
            // 当前向右
            // 不能立即向左
            // -----------------------------------------------------

            if (
                hasMoved &&
                direction == -newDirection
            )
            {
                return;
            }


            nextDirection =
                newDirection;
        }


        // =========================================================
        // Init
        // =========================================================

        public void Init(
            FeverGameContext ctx,
            Vector2Int startPosition)
        {
            gameObject.SetActive(true);


            context = ctx;


            currentPosition =
                startPosition;


            direction =
                Vector2Int.right;


            nextDirection =
                direction;


            hasMoved = false;


            active = true;


            // -----------------------------------------------------
            // 清理旧尾巴
            // -----------------------------------------------------

            ClearTail();


            // -----------------------------------------------------
            // 设置蛇头位置
            // -----------------------------------------------------

            transform.position =
                GridToWorld(
                    currentPosition
                );


            UpdateHeadRotation();


            // -----------------------------------------------------
            // 开始移动
            // -----------------------------------------------------

            if (moveCoroutine != null)
            {
                StopCoroutine(
                    moveCoroutine
                );
            }


            moveCoroutine =
                StartCoroutine(
                    MoveRoutine()
                );
        }


        public void StartMove()
        {
            if (!active)
                return;


            if (moveCoroutine != null)
            {
                StopCoroutine(
                    moveCoroutine
                );
            }


            moveCoroutine =
                StartCoroutine(
                    MoveRoutine()
                );
        }


        // =========================================================
        // Move Routine
        // =========================================================

        private IEnumerator MoveRoutine()
        {
            while (active)
            {
                yield return new WaitForSeconds(
                    moveInterval
                );


                Move();
            }
        }


        // =========================================================
        // Keyboard Input
        // =========================================================

        private void HandleKeyboardInput()
        {
            Keyboard keyboard =
                Keyboard.current;

            if (keyboard == null)
                return;


            // Up / W
            if (
                keyboard.upArrowKey.wasPressedThisFrame ||
                keyboard.wKey.wasPressedThisFrame
            )
            {
                SetDirection(
                    Vector2Int.down
                );
            }


            // Down / S
            if (
                keyboard.downArrowKey.wasPressedThisFrame ||
                keyboard.sKey.wasPressedThisFrame
            )
            {
                SetDirection(
                    Vector2Int.up
                );
            }


            // Left / A
            if (
                keyboard.leftArrowKey.wasPressedThisFrame ||
                keyboard.aKey.wasPressedThisFrame
            )
            {
                SetDirection(
                    Vector2Int.left
                );
            }


            // Right / D
            if (
                keyboard.rightArrowKey.wasPressedThisFrame ||
                keyboard.dKey.wasPressedThisFrame
            )
            {
                SetDirection(
                    Vector2Int.right
                );
            }
        }


        // =========================================================
        // Move
        // =========================================================

        private void Move()
        {
            if (!active)
                return;


            direction =
                nextDirection;


            hasMoved = true;


            Vector2Int oldHead =
                currentPosition;


            currentPosition +=
                direction;


            // =====================================================
            // 移动尾巴
            // =====================================================

            MoveTail(
                oldHead
            );


            // =====================================================
            // 移动蛇头
            // =====================================================

            transform.position =
                GridToWorld(
                    currentPosition
                );


            UpdateHeadRotation();


            // =====================================================
            // 通知外部
            // =====================================================

            OnMoveToGrid?.Invoke(
                currentPosition
            );
        }


        // =========================================================
        // Move Tail
        // =========================================================

        private void MoveTail(
            Vector2Int previousHead)
        {
            if (tailPositions.Count == 0)
                return;


            // -----------------------------------------------------
            // 从最后往前移动
            // -----------------------------------------------------

            for (
                int i = tailPositions.Count - 1;
                i > 0;
                i--
            )
            {
                tailPositions[i] =
                    tailPositions[i - 1];
            }


            // -----------------------------------------------------
            // 第一节尾巴去旧头位置
            // -----------------------------------------------------

            tailPositions[0] =
                previousHead;


            // -----------------------------------------------------
            // 更新视觉
            // -----------------------------------------------------

            for (
                int i = 0;
                i < tailVisuals.Count;
                i++
            )
            {
                if (tailVisuals[i] == null)
                    continue;


                tailVisuals[i].transform.position =
                    GridToWorld(
                        tailPositions[i]
                    );
            }
        }


        // =========================================================
        // Add Body
        // =========================================================

        public void AddBody()
        {
            Vector2Int pos;


            if (tailPositions.Count == 0)
            {
                pos =
                    currentPosition -
                    direction;
            }
            else
            {
                pos =
                    tailPositions[
                        tailPositions.Count - 1
                    ];
            }


            // -----------------------------------------------------
            // 添加逻辑位置
            // -----------------------------------------------------

            tailPositions.Add(
                pos
            );


            // -----------------------------------------------------
            // 创建尾巴
            // -----------------------------------------------------

            GameObject tail = null;


            if (SnakeTailPrefab != null)
            {
                tail =
                    Instantiate(
                        SnakeTailPrefab,
                        GridToWorld(pos),
                        Quaternion.identity
                    );


                tail.SetActive(true);


                tailVisuals.Add(
                    tail
                );
            }
        }


        // =========================================================
        // Check Tail
        // =========================================================

        public bool IsHitTail(
            Vector2Int pos)
        {
            return tailPositions.Contains(
                pos
            );
        }


        // =========================================================
        // Head Rotation
        // =========================================================

        private void UpdateHeadRotation()
        {
            if (SnakeHeadVisual == null)
                return;


            float angle = 0f;


            if (
                direction ==
                Vector2Int.right
            )
            {
                angle = 0f;
            }
            else if (
                direction ==
                Vector2Int.up
            )
            {
                angle = -90f;
            }
            else if (
                direction ==
                Vector2Int.left
            )
            {
                angle = 180f;
            }
            else if (
                direction ==
                Vector2Int.down
            )
            {
                angle = 90f;
            }


            SnakeHeadVisual.transform.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );
        }


        // =========================================================
        // Grid -> World
        // =========================================================

        private Vector3 GridToWorld(
            Vector2Int pos)
        {
            return new Vector3(
                pos.x,
                -pos.y,
                0f
            );
        }


        // =========================================================
        // Clear Tail
        // =========================================================

        public void ClearTail()
        {
            for (
                int i = 0;
                i < tailVisuals.Count;
                i++
            )
            {
                if (tailVisuals[i] != null)
                {
                    Destroy(
                        tailVisuals[i]
                    );
                }
            }


            tailVisuals.Clear();

            tailPositions.Clear();
        }


        // =========================================================
        // Die
        // =========================================================

        public void Die()
        {
            Debug.Log(
                "SnakeDie"
            );


            if (!active)
                return;


            active = false;


            if (moveCoroutine != null)
            {
                StopCoroutine(
                    moveCoroutine
                );

                moveCoroutine = null;
            }


            ClearTail();


            gameObject.SetActive(false);
        }


        // =========================================================
        // Stop
        // =========================================================

        public void Stop()
        {
            active = false;


            if (moveCoroutine != null)
            {
                StopCoroutine(
                    moveCoroutine
                );

                moveCoroutine = null;
            }


            ClearTail();


            gameObject.SetActive(false);
        }
    }
}