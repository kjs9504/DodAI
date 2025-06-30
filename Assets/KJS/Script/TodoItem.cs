using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
public class TodoItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Swipe Settings")]
    public float thresholdPixels = 50f;
    public float moveDistance = 150f;
    public float animDuration = 0.2f;

    [Header("Delete Button")]
    [Tooltip("이 버튼을 누르면 삭제 애니메이션 후 아이템이 파괴됩니다.")]
    public Button deleteButton;

    RectTransform rt;
    CanvasGroup cg;
    ScrollRect parentScroll;
    Vector2 originalPos;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        parentScroll = GetComponentInParent<ScrollRect>();

        // 삭제 버튼에 리스너 연결
        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDeleteButton);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        originalPos = rt.anchoredPosition;
        if (parentScroll != null) parentScroll.vertical = false;
    }

    public void OnDrag(PointerEventData e)
    {
        rt.anchoredPosition += new Vector2(e.delta.x, 0f);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (parentScroll != null) parentScroll.vertical = true;

        float deltaX = rt.anchoredPosition.x - originalPos.x;
        if (Mathf.Abs(deltaX) >= thresholdPixels)
        {
            float dir = Mathf.Sign(deltaX);
            MoveOnly(dir * moveDistance);
        }
        else
        {
            ReturnToOriginal();
        }
    }

    // 삭제 버튼 클릭 시 호출
    void OnDeleteButton()
    {
        // 중복 클릭 방지
        deleteButton.interactable = false;
        // 레이캐스트 막아서 터치 무시
        cg.blocksRaycasts = false;
        // 현재 위치 기준, 오른쪽으로 moveDistance만큼 날린 뒤 페이드아웃
        StartCoroutine(AnimateDelete(rt.anchoredPosition, rt.anchoredPosition + Vector2.right * moveDistance));
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
            float ease = 1f - (1f - t) * (1f - t);  // Ease-Out-Quad
            rt.anchoredPosition = Vector2.Lerp(from, to, ease);
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    // 슬라이드 + 페이드아웃 애니메이션
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
        // 최종 상태 보정
        rt.anchoredPosition = to;
        cg.alpha = 0f;
        Destroy(gameObject);
    }
}
