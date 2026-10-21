using System.Collections;
using System.Collections.Generic;
using BallzGame.Managers;
using UnityEngine;
using UnityEngine.EventSystems;

public class BallTrajectoryPreview : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BallLauncher launcher;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Trajectory")]
    [SerializeField] private float maxDistance = 100f;
    [Tooltip("World-space ball radius. A ball with diameter 0.5 has radius 0.25.")]
    [SerializeField, Min(0.001f)] private float ballRadius = 0.25f;
    [SerializeField] private float skin = 0.01f;
    [SerializeField] private int bounceTime;

    [Header("Input")]
    [SerializeField] private float showDelay = 0.05f;

    private EventTrigger area;

    private EventTrigger.Entry pointerDownEntry;
    private EventTrigger.Entry dragEntry;
    private EventTrigger.Entry pointerUpEntry;

    private bool dragging;
    private bool trajectoryVisible;

    private Vector2 inputTarget;

    private Coroutine showCoroutine;

    public void CancelAim()
    {
        dragging = false;
        if (showCoroutine != null) StopCoroutine(showCoroutine);
        showCoroutine = null;
        HideTrajectory();
    }


    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        if (launcher == null)
            launcher = GetComponent<BallLauncher>();

        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
    }


    private void Start()
    {
        if (launcher == null)
            return;

        area = launcher.clickableArea;

        SetupEventTrigger();
    }


    private void OnDestroy()
    {
        RemoveEventTrigger();

        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }
    }


    // =========================================================
    // EventTrigger
    // =========================================================

    private void SetupEventTrigger()
    {
        if (area == null)
            return;


        pointerDownEntry = AddEventTrigger(
            EventTriggerType.PointerDown,
            OnPointerDown
        );


        dragEntry = AddEventTrigger(
            EventTriggerType.Drag,
            OnDrag
        );


        pointerUpEntry = AddEventTrigger(
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


        entry.callback.AddListener(callback);

        area.triggers.Add(entry);

        return entry;
    }


    private void RemoveEventTrigger()
    {
        if (area == null)
            return;


        if (pointerDownEntry != null)
        {
            area.triggers.Remove(pointerDownEntry);

            pointerDownEntry = null;
        }


        if (dragEntry != null)
        {
            area.triggers.Remove(dragEntry);

            dragEntry = null;
        }


        if (pointerUpEntry != null)
        {
            area.triggers.Remove(pointerUpEntry);

            pointerUpEntry = null;
        }
    }


    // =========================================================
    // Input
    // =========================================================

    private void OnPointerDown(BaseEventData data)
    {
        PointerEventData pointerData =
            data as PointerEventData;

        if (pointerData == null)
            return;


        dragging = true;

        trajectoryVisible = false;


        // 使用 PointerEventData 的位置
        // PC = Mouse
        // Android/iOS = Touch
        UpdateInputTarget(pointerData);


        // 防止之前的 Coroutine 还在
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
        }


        showCoroutine =
            StartCoroutine(
                ShowTrajectoryAfterDelay()
            );
    }


    private IEnumerator ShowTrajectoryAfterDelay()
    {
        yield return new WaitForSeconds(
            showDelay
        );


        // 这 0.05 秒内已经松手
        if (!dragging)
        {
            showCoroutine = null;

            yield break;
        }


        trajectoryVisible = true;


        // 这里无法直接拿 PointerEventData
        // 所以 UpdateInputTarget 会在 Drag 时
        // 持续更新 inputTarget
        UpdateTrajectory();


        showCoroutine = null;
    }


    private void OnDrag(BaseEventData data)
    {
        if (!dragging)
            return;


        PointerEventData pointerData =
            data as PointerEventData;

        if (pointerData == null)
            return;


        // 更新触摸 / 鼠标位置
        UpdateInputTarget(pointerData);


        // 还没达到 0.05 秒
        if (!trajectoryVisible)
            return;


        UpdateTrajectory();
    }


    private void OnPointerUp(BaseEventData data)
    {
        dragging = false;


        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);

            showCoroutine = null;
        }


        HideTrajectory();
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
            GameManager.Instance.MainCamera
                .ScreenToWorldPoint(
                    new Vector3(
                        screenPosition.x,
                        screenPosition.y,
                        0f
                    )
                );


        inputTarget = launcher != null && launcher.ForcedLaunchTarget.HasValue
            ? launcher.ForcedLaunchTarget.Value
            : (Vector2)worldPosition;
    }


    // =========================================================
    // Trajectory
    // =========================================================

    private void UpdateTrajectory()
    {
        Vector2 startPosition =
            launcher.transform.position;


        Vector2 direction =
            inputTarget - startPosition;


        if (direction.sqrMagnitude < 0.0001f)
        {
            HideTrajectory();

            return;
        }


        direction.Normalize();


        List<Vector3> points = new()
        {
            startPosition
        };


        Vector2 currentPosition =
            startPosition;


        int bounceCount = 0;


        while (
            bounceCount <
            bounceTime + 1
        )
        {
            RaycastHit2D hit =
                GetSolidHit(
                    currentPosition,
                    direction,
                    maxDistance
                );


            // -------------------------------------------------
            // 没有碰到墙
            // -------------------------------------------------

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
            // Draw the ball center at impact, rather than the surface contact point.
            // -------------------------------------------------

            points.Add(
                hit.centroid
            );


            // -------------------------------------------------
            // 发生一次反弹
            // -------------------------------------------------

            bounceCount++;


            // -------------------------------------------------
            // 达到最大次数
            // -------------------------------------------------

            if (
                bounceCount >=
                bounceTime + 1
            )
            {
                break;
            }


            // -------------------------------------------------
            // 反弹
            // -------------------------------------------------

            direction =
                Vector2.Reflect(
                    direction,
                    hit.normal
                ).normalized;


            // -------------------------------------------------
            // 从碰撞点稍微偏移
            // Offset outward so the next circle cast starts clear of the surface.
            // -------------------------------------------------

            currentPosition =
                hit.centroid +
                hit.normal * Mathf.Max(skin, 0.001f);
        }


        // -----------------------------------------------------
        // 更新 LineRenderer
        // -----------------------------------------------------

        lineRenderer.positionCount =
            points.Count;


        lineRenderer.SetPositions(
            points.ToArray()
        );


        lineRenderer.enabled = true;
    }


    // =========================================================
    // 获取 Ball 真正会撞到的 Collider
    // =========================================================

    private RaycastHit2D GetSolidHit(
        Vector2 origin,
        Vector2 direction,
        float distance)
    {
        RaycastHit2D[] hits =
            Physics2D.CircleCastAll(
                origin,
                ballRadius,
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


            // Trigger 不参与 Ball 反弹
            if (hit.collider.isTrigger)
                continue;


            return hit;
        }


        return default;
    }


    // =========================================================
    // Hide
    // =========================================================

    private void HideTrajectory()
    {
        trajectoryVisible = false;


        lineRenderer.positionCount = 0;

        lineRenderer.enabled = false;
    }
}
