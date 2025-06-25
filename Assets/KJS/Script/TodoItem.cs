using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class TodoItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    const float DELETE_THRESHOLD = 0.3f;
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
        // (수평/수직 구분 로직 생략)
        rt.anchoredPosition += new Vector2(e.delta.x, 0);
    }

    public void OnEndDrag(PointerEventData e)
    {
        parentScroll.vertical = true;
        float width = ((RectTransform)rt.parent).rect.width;
        if (Mathf.Abs(rt.anchoredPosition.x) > width * DELETE_THRESHOLD
            && rt.anchoredPosition.x > 0)
            DeleteSelf();
        else
            ReturnToPlace();
    }

    // ───────────────────────────────────────────────
    // 1) 원위치로 부드럽게 복귀
    public void ReturnToPlace()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateMove(rt.anchoredPosition, originalPos, 0.2f));
    }

    // 2) 오른쪽으로 밀어내면서 동시에 페이드아웃 → 삭제
    public void DeleteSelf()
    {
        StopAllCoroutines();
        Vector2 targetPos = new Vector2(rt.rect.width, originalPos.y);
        StartCoroutine(AnimateDelete(rt.anchoredPosition, targetPos, 0.2f));
    }

    // ───────────────────────────────────────────────
    // 수평 이동용 코루틴 (Ease-Out-Quad)
    IEnumerator AnimateMove(Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease-Out-Quad: f(t) = 1 − (1−t)²
            float ease = 1f - (1f - t) * (1f - t);
            rt.anchoredPosition = Vector2.Lerp(from, to, ease);
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    // 수평 이동 + 페이드아웃용 코루틴
    IEnumerator AnimateDelete(Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - (1f - t) * (1f - t);
            rt.anchoredPosition = Vector2.Lerp(from, to, ease);
            cg.alpha = Mathf.Lerp(1f, 0f, ease);
            yield return null;
        }
        cg.alpha = 0f;
        Destroy(gameObject);
    }
}


