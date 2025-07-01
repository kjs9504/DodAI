using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 1) 수평 스와이프: 삭제 버튼 노출 & 삭제  
/// 2) 수직 드래그: 아이템 순서 변경 (Offset 보정 포함)  
/// </summary>
[RequireComponent(typeof(CanvasGroup), typeof(RectTransform), typeof(LayoutElement))]
public class TodoItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Swipe Settings")]
    public float thresholdPixels = 50f;
    public float moveDistance = 150f;
    public float animDuration = 0.2f;
    [Header("Delete Button")]
    public Button deleteButton;

    // Reorder 관련
    private bool isReordering = false;
    private Transform originalParent;
    private int originalSiblingIndex;
    private GameObject placeholder;
    private Vector2 dragOffset;     // 클릭 위치 ↔ 피벗 간 Offset

    // 공통
    RectTransform rt;
    CanvasGroup cg;
    ScrollRect parentScroll;
    Vector2 originalPos;         // 스와이프용

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        parentScroll = GetComponentInParent<ScrollRect>();
        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDeleteButton);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        originalPos = rt.anchoredPosition;
        if (parentScroll != null) parentScroll.vertical = false;
        isReordering = false;
    }

    public void OnDrag(PointerEventData e)
    {
        // 첫 이동에서 수직 이동이 더 크면 리오더 시작
        if (!isReordering && Mathf.Abs(e.delta.y) > Mathf.Abs(e.delta.x))
        {
            StartReorder();

            // 이 시점에서 클릭 지점과 피벗 위치 간 Offset 계산
            RectTransform canvasRect = rt.parent as RectTransform;
            Vector2 localPointer;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, e.position, e.pressEventCamera, out localPointer);
            dragOffset = rt.anchoredPosition - localPointer;

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

    #region —— Swipe (가로 스와이프) ——

    void HandleSwipeDrag(PointerEventData e)
    {
        rt.anchoredPosition += new Vector2(e.delta.x, 0f);
    }

    void HandleSwipeEnd(PointerEventData e)
    {
        float deltaX = rt.anchoredPosition.x - originalPos.x;
        if (Mathf.Abs(deltaX) >= thresholdPixels)
            MoveOnly(Mathf.Sign(deltaX) * moveDistance);
        else
            ReturnToOriginal();
    }

    public void MoveOnly(float distance)
    {
        StopAllCoroutines();
        Vector2 target = originalPos + new Vector2(distance, 0f);
        StartCoroutine(AnimateMove(rt.anchoredPosition, target));
    }

    public void ReturnToOriginal()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateMove(rt.anchoredPosition, originalPos));
    }

    IEnumerator AnimateMove(Vector2 from, Vector2 to)
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
    }

    void OnDeleteButton()
    {
        deleteButton.interactable = false;
        cg.blocksRaycasts = false;
        StartCoroutine(AnimateDelete(rt.anchoredPosition,
            rt.anchoredPosition + Vector2.right * moveDistance));
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
        rt.anchoredPosition = to;
        cg.alpha = 0f;
        Destroy(gameObject);
    }

    #endregion

    #region —— Reorder (수직 드래그) ——

    void StartReorder()
    {
        originalParent = rt.parent;
        originalSiblingIndex = rt.GetSiblingIndex();

        // 플레이스홀더 생성
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
        // worldPositionStays: true로 해야 리패런팅 시 월드 위치 유지
        Canvas canvas = GetComponentInParent<Canvas>();
        rt.SetParent(canvas.transform, worldPositionStays: true);
    }

    void HandleReorderDrag(PointerEventData e)
    {
        // 스크린→캔버스 로컬 좌표 변환 후 Offset 적용
        RectTransform canvasRect = rt.parent as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, e.position, e.pressEventCamera, out localPoint);
        rt.anchoredPosition = localPoint + dragOffset;

        // 플레이스홀더 위치 업데이트
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
        cg.blocksRaycasts = true;
    }

    #endregion
}
