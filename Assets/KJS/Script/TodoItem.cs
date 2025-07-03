using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup), typeof(RectTransform), typeof(LayoutElement))]
public class TodoItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Swipe Settings")]
    public float thresholdPixels = 50f;         // 오른쪽 스와이프 임계치
    public float moveDistance = 150f;           // 애니메이션 이동 거리
    public float animDuration = 0.2f;           // 애니메이션 시간
    public float revealDeleteThreshold = 50f;   // 왼쪽 스와이프 임계치
    [Header("Delete Button")]
    public Button deleteButton;

    [Header("Apply Button")]
    public Button applyButton;

    // Reorder 관련
    bool isReordering = false;
    Transform originalParent;
    int originalSiblingIndex;
    GameObject placeholder;
    Vector2 dragOffset;     // 클릭 위치 ↔ 피벗 간 Offset

    // Swipe용
    RectTransform rt;
    CanvasGroup cg;
    ScrollRect parentScroll;
    Vector2 originalPos;    // swipe 시작 시 anchoredPosition
    float swipeStartLocalX; // swipe 시작 시 포인터의 로컬 X

    // 이동 코루틴 핸들
    Coroutine moveCoroutine;

    private enum SwipeState { Normal, DeleteShown, ApplyShown, Moving }
    private SwipeState swipeState = SwipeState.Normal;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        parentScroll = GetComponentInParent<ScrollRect>();

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteButton);
            HideDeleteButton();
        }
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(OnApplyButton);
            HideApplyButton();
        }
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (swipeState == SwipeState.Moving) return;

        // Normal 상태에서 스와이프를 시작할 때만 originalPos를 기록합니다.
        if (swipeState == SwipeState.Normal)
        {
            originalPos = rt.anchoredPosition;
        }

        RectTransform canvasRect = rt.parent as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, e.position, e.pressEventCamera, out localPoint);
        swipeStartLocalX = localPoint.x;

        // 2) Scroll 막기, delete 버튼 숨기기
        if (parentScroll != null) parentScroll.vertical = false;
        
        isReordering = false;
    }

    public void OnDrag(PointerEventData e)
    {
        // 수직 이동이 더 크면 Reorder 모드로
        if (!isReordering && Mathf.Abs(e.delta.y) > Mathf.Abs(e.delta.x))
        {
            StartReorder();
            // 클릭 ↔ 피벗 Offset
            RectTransform canvasRect = rt.parent as RectTransform;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, e.position, e.pressEventCamera, out localPoint);
            dragOffset = rt.anchoredPosition - localPoint;
            isReordering = true;
        }

        if (isReordering)
            HandleReorderDrag(e);
        else
            HandleSwipeDrag(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (parentScroll != null) parentScroll.vertical = true;

        if (isReordering)
            EndReorder();
        else
            HandleSwipeEnd(e);
    }

    #region —— Swipe —— 

    void HandleSwipeDrag(PointerEventData e)
    {
        // Canvas 로컬 좌표로 현재 포인터 위치 구하기
        RectTransform canvasRect = rt.parent as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, e.position, e.pressEventCamera, out localPoint);

        // swipeStartLocalX 대비 얼마나 움직였는지 계산
        float deltaX = localPoint.x - swipeStartLocalX;
        Vector2 targetPos = originalPos + new Vector2(deltaX, 0f);

        // (선택) 감속 효과: Lerp 사용 시 Uncomment
        // rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, targetPos, Time.deltaTime * 10f);
        // 바로 따라오려면 아래 사용:
        rt.anchoredPosition = targetPos;
    }

    void HandleSwipeEnd(PointerEventData e)
    {
        if (swipeState == SwipeState.Moving) return; // 애니메이션 중엔 무시

        float finalDelta = rt.anchoredPosition.x - originalPos.x;

        if (swipeState == SwipeState.DeleteShown && finalDelta >= thresholdPixels)
        {
            ReturnToOriginal();
            return;
        }
        if (swipeState == SwipeState.ApplyShown && finalDelta <= -revealDeleteThreshold)
        {
            ReturnToOriginal();
            return;
        }

        if (finalDelta <= -revealDeleteThreshold)
        {
            ShowDeleteButton();
            HideApplyButton();
            Move(-moveDistance, () => { swipeState = SwipeState.DeleteShown; });
            swipeState = SwipeState.Moving;
        }
        else if (finalDelta >= thresholdPixels)
        {
            ShowApplyButton();
            HideDeleteButton();
            Move(+moveDistance, () => { swipeState = SwipeState.ApplyShown; });
            swipeState = SwipeState.Moving;
        }
        else
        {
            HideDeleteButton();
            HideApplyButton();
            Move(0f, () => { swipeState = SwipeState.Normal; });
            swipeState = SwipeState.Moving;
        }
    }

    void ShowDeleteButton()
    {
        if (deleteButton != null)
        {
            deleteButton.gameObject.SetActive(true);
            deleteButton.interactable = true;
            swipeState = SwipeState.DeleteShown;
        }
    }

    void HideDeleteButton()
    {
        if (deleteButton != null)
        {
            deleteButton.interactable = false;
            deleteButton.gameObject.SetActive(false);
            if (swipeState == SwipeState.DeleteShown) swipeState = SwipeState.Normal;
        }
    }

    void ShowApplyButton()
    {
        if (applyButton != null)
        {
            applyButton.gameObject.SetActive(true);
            applyButton.interactable = true;
            swipeState = SwipeState.ApplyShown;
        }
    }

    void HideApplyButton()
    {
        if (applyButton != null)
        {
            applyButton.interactable = false;
            applyButton.gameObject.SetActive(false);
            if (swipeState == SwipeState.ApplyShown) swipeState = SwipeState.Normal;
        }
    }

    void OnDeleteButton()
    {
        deleteButton.interactable = false;
        cg.blocksRaycasts = false;
        Move(+moveDistance, () =>
            StartCoroutine(AnimateDelete(rt.anchoredPosition, rt.anchoredPosition + Vector2.right * moveDistance))
        );
    }

    void OnApplyButton()
    {
        // Apply 동작 처리 (예: 데이터 적용, UI 피드백 등)
        HideApplyButton();
        cg.blocksRaycasts = false;
        Move(+moveDistance, () => {
            // 완료 후 추가 로직이 필요하면 여기에
        });
    }

    void ReturnToOriginal()
    {
        HideDeleteButton();
        HideApplyButton();
        Move(0f, () => { swipeState = SwipeState.Normal; });
        swipeState = SwipeState.Moving;
    }

    void Move(float distance, Action onComplete = null)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);
        Vector2 target = originalPos + new Vector2(distance, 0f);
        moveCoroutine = StartCoroutine(AnimateMove(rt.anchoredPosition, target, () => {
            onComplete?.Invoke();
            // 애니메이션 끝나면 상태 복구 (onComplete에서 처리)
        }));
    }

    IEnumerator AnimateMove(Vector2 from, Vector2 to, Action onComplete = null)
    {
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float ease = 1f - (1f - t) * (1f - t);
            rt.anchoredPosition = Vector2.Lerp(from, to, ease);
            yield return null;
        }
        rt.anchoredPosition = to;
        onComplete?.Invoke();
    }

    IEnumerator AnimateDelete(Vector2 from, Vector2 to)
    {
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float ease = 1f - (1f - t) * (1f - t);
            rt.anchoredPosition = Vector2.Lerp(from, to, ease);
            cg.alpha = Mathf.Lerp(1f, 0f, ease);
            yield return null;
        }
        Destroy(gameObject);
    }

    #endregion

    #region —— Reorder —— 

    void StartReorder()
    {
        originalParent = rt.parent;
        originalSiblingIndex = rt.GetSiblingIndex();

        placeholder = new GameObject("Placeholder");
        var le = placeholder.AddComponent<LayoutElement>();
        var selfLE = GetComponent<LayoutElement>();
        le.preferredHeight = selfLE.preferredHeight;
        le.preferredWidth = selfLE.preferredWidth;
        le.flexibleHeight = 0;
        le.flexibleWidth = 0;

        placeholder.transform.SetParent(originalParent);
        placeholder.transform.SetSiblingIndex(originalSiblingIndex);

        cg.blocksRaycasts = false;
        var canvas = GetComponentInParent<Canvas>();
        rt.SetParent(canvas.transform, worldPositionStays: true);

        // 2) **모든 아이템의 삭제 버튼을 일괄 숨기기**
        foreach (var item in originalParent.GetComponentsInChildren<TodoItem>())
            item.HideDeleteButton();
    }

    void HandleReorderDrag(PointerEventData e)
    {
        RectTransform canvasRect = rt.parent as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, e.position, e.pressEventCamera, out localPoint);
        rt.anchoredPosition = localPoint + dragOffset;

        int newIndex = originalParent.childCount;
        for (int i = 0; i < originalParent.childCount; i++)
        {
            var child = originalParent.GetChild(i);
            if (child == placeholder) continue;
            if (rt.position.y > child.position.y)
            {
                newIndex = i;
                if (placeholder.transform.GetSiblingIndex() < newIndex)
                    newIndex--;
                break;
            }
        }
        placeholder.transform.SetSiblingIndex(newIndex);
    }

    void EndReorder()
    {
        rt.SetParent(originalParent);
        rt.SetSiblingIndex(placeholder.transform.GetSiblingIndex());
        Destroy(placeholder);
        // 2) **삭제 버튼 숨기기** 추가!
        HideDeleteButton();
        HideApplyButton();

        // 3) Raycast 복원
        cg.blocksRaycasts = true;
    }

    #endregion
}