using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class TodoItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Swipe Settings")]
    [Tooltip("전체 너비 대비 드래그 임계치(0~1)")]
    public float deleteThreshold = 0.3f;
    [Tooltip("드래그 후 이동시킬 거리 (픽셀)")]
    public float moveDistance = 150f;
    [Tooltip("애니메이션 지속 시간 (초)")]
    public float animDuration = 0.2f;

    RectTransform rt;
    CanvasGroup cg;
    ScrollRect parentScroll;
    Vector2 originalPos;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        parentScroll = GetComponentInParent<ScrollRect>();
        cg.blocksRaycasts = true;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        originalPos = rt.anchoredPosition;
        parentScroll.vertical = false;
    }

    public void OnDrag(PointerEventData e)
    {
        rt.anchoredPosition += new Vector2(e.delta.x, 0);
    }

    public void OnEndDrag(PointerEventData e)
    {
        parentScroll.vertical = true;
        float width = ((RectTransform)rt.parent).rect.width;

        if (Mathf.Abs(rt.anchoredPosition.x) > width * deleteThreshold)
        {
            float dir = Mathf.Sign(rt.anchoredPosition.x);
            MoveSelf(dir * moveDistance);
        }
        else
        {
            ReturnToPlace();
        }
    }

    public void ReturnToPlace()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateMove(rt.anchoredPosition, originalPos, animDuration));
    }

    public void MoveSelf(float distance)
    {
        StopAllCoroutines();
        Vector2 target = originalPos + new Vector2(distance, 0);
        StartCoroutine(AnimateMove(rt.anchoredPosition, target, animDuration));
    }

    IEnumerator AnimateMove(Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - (1f - t) * (1f - t);  // Ease-Out-Quad
            rt.anchoredPosition = Vector2.Lerp(from, to, ease);
            yield return null;
        }
        rt.anchoredPosition = to;
    }
}



