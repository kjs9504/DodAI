using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup), typeof(RectTransform), typeof(LayoutElement))]
public class TodoItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Animation Settings")]
    public float animDuration = 0.2f;

    [Header("수락(체크) 버튼")]
    public Image checkMarkImage; // 체크 애니메이션용 이미지(Inspector에서 연결)
    public float checkAnimDuration = 0.3f;

    [Header("Swipe Settings")]
    public float swipeThreshold = 100f; // 스와이프 인식 임계값
    public float swipeReturnSpeed = 5f; // 스와이프 후 복귀 속도

    [Header("Backend Settings")]
    public string backendUrl = "http://192.168.0.58:8080/api/tasks";

    // 이 아이템이 표현하는 Task 정보 (캘린더 생성 시 할당)
    [HideInInspector] public long id;
    [HideInInspector] public string todo;
    [HideInInspector] public string date;
    [HideInInspector] public string time;
    [HideInInspector] public TodoItemData data;

    // Reorder 관련
    bool isReordering = false;
    Transform originalParent;
    int originalSiblingIndex;
    GameObject placeholder;
    Vector2 dragOffset;

    // Swipe 관련
    bool isHorizontalSwiping = false;
    Vector2 dragStartPos;
    Vector2 originalPosition;
    bool isDeleteAction = false; // 삭제 액션인지 구분

    // UI 관련
    RectTransform rt;
    CanvasGroup cg;
    ScrollRect parentScroll;

    // 이동 코루틴 핸들
    Coroutine moveCoroutine;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        parentScroll = GetComponentInParent<ScrollRect>();

        // 체크마크 초기화
        if (checkMarkImage != null)
            checkMarkImage.enabled = false;
    }

    // 클릭 이벤트 처리 (기본 클릭만 처리)
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"=== TodoItem 클릭됨: {gameObject.name} ===");

        // 리오더링이나 스와이프 중이면 클릭 무시
        if (isReordering || isHorizontalSwiping) return;

        // 필요시 여기에 클릭 처리 로직 추가
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (parentScroll != null) parentScroll.vertical = false;
        isReordering = false;
        isHorizontalSwiping = false;
        dragStartPos = e.position;
        originalPosition = rt.anchoredPosition;
    }

    public void OnDrag(PointerEventData e)
    {
        Vector2 dragDelta = e.position - dragStartPos;

        // 아직 방향이 결정되지 않았다면
        if (!isReordering && !isHorizontalSwiping)
        {
            // 수직 드래그가 더 크면 리오더링
            if (Mathf.Abs(dragDelta.y) > Mathf.Abs(dragDelta.x) && Mathf.Abs(dragDelta.y) > 10f)
            {
                StartReorder();
                var canvasRect = rt.parent as RectTransform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, e.position, e.pressEventCamera, out Vector2 localPoint);
                dragOffset = rt.anchoredPosition - localPoint;
                isReordering = true;
            }
            // 수평 드래그가 더 크면 스와이프
            else if (Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.y) && Mathf.Abs(dragDelta.x) > 10f)
            {
                isHorizontalSwiping = true;
            }
        }

        // 리오더링 처리
        if (isReordering)
        {
            HandleReorderDrag(e);
        }
        // 스와이프 처리
        else if (isHorizontalSwiping)
        {
            HandleSwipeDrag(dragDelta.x);
        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (parentScroll != null) parentScroll.vertical = true;

        if (isReordering)
        {
            EndReorder();
        }
        else if (isHorizontalSwiping)
        {
            EndSwipe(e.position - dragStartPos);
        }
    }

    #region Swipe Logic

    void HandleSwipeDrag(float deltaX)
    {
        // 스와이프를 더 반응적으로 - 1:1 비율로 변경
        rt.anchoredPosition = new Vector2(originalPosition.x + deltaX, originalPosition.y);

        // 더 부드러운 알파값 변화
        float swipeProgress = Mathf.Abs(deltaX) / swipeThreshold;
        cg.alpha = Mathf.Lerp(1f, 0.3f, Mathf.Clamp01(swipeProgress));

        // 스와이프 방향에 따른 색상 힌트 (선택사항)
        var swipeImage = GetComponent<Image>();
        if (swipeImage != null)
        {
            if (deltaX < -50f)
            {
                // 왼쪽 스와이프 - 삭제 힌트 (빨간색 틴트)
                swipeImage.color = Color.Lerp(Color.white, Color.red, swipeProgress * 0.3f);
            }
            else if (deltaX > 50f)
            {
                // 오른쪽 스와이프 - 완료 힌트 (초록색 틴트)
                swipeImage.color = Color.Lerp(Color.white, Color.green, swipeProgress * 0.3f);
            }
        }
    }

    void EndSwipe(Vector2 totalDelta)
    {
        float swipeDistance = totalDelta.x;

        // 왼쪽 스와이프 (삭제)
        if (swipeDistance < -swipeThreshold)
        {
            Debug.Log("왼쪽 스와이프 - 삭제 실행");
            isDeleteAction = true;
            StartCoroutine(FastSwipeOut(Vector2.left, () => ExecuteDelete()));
        }
        // 오른쪽 스와이프 (완료)
        else if (swipeDistance > swipeThreshold)
        {
            Debug.Log("오른쪽 스와이프 - 완료 실행");
            isDeleteAction = false;

            // 완료는 체크 애니메이션 후 스와이프 아웃
            if (checkMarkImage != null)
                StartCoroutine(CheckMarkThenSwipeOut());
            else
                StartCoroutine(FastSwipeOut(Vector2.right, () => ExecuteComplete()));
        }
        // 임계값에 못 미치면 원위치로 복귀
        else
        {
            StartCoroutine(ReturnToOriginalPosition());
        }

        isHorizontalSwiping = false;
    }

    // 빠른 스와이프 아웃 애니메이션
    IEnumerator FastSwipeOut(Vector2 direction, System.Action onComplete = null)
    {
        cg.blocksRaycasts = false;

        Vector2 startPos = rt.anchoredPosition;
        Vector2 endPos = startPos + direction * Screen.width; // 화면 너비만큼 이동

        float elapsed = 0f;
        float fastDuration = 0.15f; // 빠른 애니메이션

        while (elapsed < fastDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fastDuration;

            // EaseInQuad로 가속감 있게
            float easeT = t * t;

            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, easeT);
            cg.alpha = Mathf.Lerp(1f, 0f, easeT);

            yield return null;
        }

        // 애니메이션 완료 후 백엔드 호출
        onComplete?.Invoke();
    }

    // 체크마크 후 스와이프 아웃
    IEnumerator CheckMarkThenSwipeOut()
    {
        // 체크 애니메이션
        if (checkMarkImage != null)
        {
            checkMarkImage.enabled = true;
            checkMarkImage.color = new Color(0, 1, 0, 0);

            float elapsed = 0;
            while (elapsed < checkAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / checkAnimDuration);
                checkMarkImage.color = new Color(0, 1, 0, t);
                checkMarkImage.transform.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one * 1.2f, t);
                yield return null;
            }

            // 잠깐 대기
            yield return new WaitForSeconds(0.1f);
        }

        // 오른쪽으로 빠르게 스와이프 아웃
        yield return StartCoroutine(FastSwipeOut(Vector2.right, () => ExecuteComplete()));
    }

    IEnumerator ReturnToOriginalPosition()
    {
        Vector2 currentPos = rt.anchoredPosition;
        float elapsed = 0f;
        float returnDuration = 0.15f; // 복귀도 빠르게

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;

            // EaseOutBack으로 살짝 튕기는 효과
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            rt.anchoredPosition = Vector2.Lerp(currentPos, originalPosition, easeT);
            cg.alpha = Mathf.Lerp(cg.alpha, 1f, easeT);

            // 색상 복원
            var returnImage = GetComponent<Image>();
            if (returnImage != null)
                returnImage.color = Color.Lerp(returnImage.color, Color.white, easeT);

            yield return null;
        }

        rt.anchoredPosition = originalPosition;
        cg.alpha = 1f;
        var finalImage = GetComponent<Image>();
        if (finalImage != null)
            finalImage.color = Color.white;
    }

    void ExecuteDelete()
    {
        Debug.Log($"삭제 실행: id={data.id}, todo={data.todo}");

        TodoListData deleteList = new TodoListData();
        deleteList.tasks = new List<TodoItemData> { data };
        string json = JsonUtility.ToJson(deleteList);

        StartCoroutine(SendDeleteRequest(json));
    }

    void ExecuteComplete()
    {
        Debug.Log("완료 실행!");

        TodoListData completeList = new TodoListData();
        completeList.tasks = new List<TodoItemData> { data };
        string json = JsonUtility.ToJson(completeList);

        StartCoroutine(SendCompleteRequest(json));
    }

    // 백엔드 요청들 (애니메이션 후 처리)
    IEnumerator SendDeleteRequest(string json)
    {
        using var req = new UnityWebRequest($"{backendUrl}/bulk", "DELETE");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" };
        req.downloadHandler = new DownloadHandlerBuffer();
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("삭제 요청 실패: " + req.error);
            // 실패해도 이미 UI에서 사라졌으므로 로그만 출력
        }

        // 성공/실패 관계없이 오브젝트 삭제
        Destroy(gameObject);
    }

    IEnumerator SendCompleteRequest(string json)
    {
        using var req = new UnityWebRequest($"{backendUrl}/bulk/accept", "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" };
        req.downloadHandler = new DownloadHandlerBuffer();
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("완료 요청 실패: " + req.error);
        }

        // 성공/실패 관계없이 오브젝트 삭제
        Destroy(gameObject);
    }

    #endregion

    #region Delete + Backend

    IEnumerator DeleteThenAnimate(string json)
    {
        using var req = new UnityWebRequest($"{backendUrl}/bulk", "DELETE");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" };
        req.downloadHandler = new DownloadHandlerBuffer();
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            StartCoroutine(AnimateDelete());
        }
        else
        {
            Debug.LogError("삭제 요청 실패: " + req.error);
            cg.blocksRaycasts = true;
            // 실패 시 원위치로 복귀
            StartCoroutine(ReturnToOriginalPosition());
        }
    }

    #endregion

    #region Complete + Backend

    IEnumerator CheckMarkAnimation()
    {
        checkMarkImage.enabled = true;
        checkMarkImage.color = new Color(0, 1, 0, 0); // 투명한 초록색
        float elapsed = 0;
        while (elapsed < checkAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / checkAnimDuration);
            checkMarkImage.color = new Color(0, 1, 0, t); // 점점 진해짐
            checkMarkImage.transform.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one, t);
            yield return null;
        }
        checkMarkImage.color = new Color(0, 1, 0, 1);
        checkMarkImage.transform.localScale = Vector3.one;
    }

    IEnumerator CompleteThenAnimate(string json)
    {
        using var req = new UnityWebRequest($"{backendUrl}/bulk/accept", "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" };
        req.downloadHandler = new DownloadHandlerBuffer();
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            // 체크 애니메이션 후 사라지게
            yield return new WaitForSeconds(0.2f);
            StartCoroutine(AnimateDelete());
        }
        else
        {
            Debug.LogError("완료 요청 실패: " + req.error);
            // 실패 시 체크 마크 숨기고 복구
            if (checkMarkImage != null) checkMarkImage.enabled = false;
            cg.blocksRaycasts = true;
            StartCoroutine(ReturnToOriginalPosition());
        }
    }

    #endregion

    #region Animation Helpers

    IEnumerator AnimateDelete()
    {
        Vector2 startPos = rt.anchoredPosition;
        // 삭제 액션이면 왼쪽으로, 완료 액션이면 오른쪽으로
        Vector2 endPos = startPos + (isDeleteAction ? Vector2.left : Vector2.right) * 300f;

        float elapsed = 0;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float e = 1f - (1f - t) * (1f - t); // EaseOut

            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, e);
            cg.alpha = Mathf.Lerp(1f, 0f, e);
            yield return null;
        }
        Destroy(gameObject);
    }

    #endregion

    #region Reorder Logic

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
        cg.blocksRaycasts = true;
        isReordering = false;
    }

    #endregion
}