using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections.Generic; // Added for List

[RequireComponent(typeof(CanvasGroup), typeof(RectTransform), typeof(LayoutElement))]
public class TodoItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Swipe Settings")]
    public float thresholdPixels = 50f;
    public float moveDistance = 150f;
    public float animDuration = 0.2f;
    public float revealDeleteThreshold = 50f;

    [Header("Delete Button")]
    public Button deleteButton;

    [Header("Apply Button")]
    public Button applyButton;

    [Header("Backend Settings")]
    public string backendUrl = "http://localhost:8080/api/tasks";

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

    // Swipe용
    RectTransform rt;
    CanvasGroup cg;
    ScrollRect parentScroll;
    Vector2 originalPos;
    float swipeStartLocalX;

    // 이동 코루틴 핸들
    Coroutine moveCoroutine;

    enum SwipeState { Normal, DeleteShown, ApplyShown, Moving }
    SwipeState swipeState = SwipeState.Normal;

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
        if (swipeState == SwipeState.Normal)
            originalPos = rt.anchoredPosition;

        var canvasRect = rt.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, e.position, e.pressEventCamera, out Vector2 localPoint);
        swipeStartLocalX = localPoint.x;

        if (parentScroll != null) parentScroll.vertical = false;
        isReordering = false;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!isReordering && Mathf.Abs(e.delta.y) > Mathf.Abs(e.delta.x))
        {
            StartReorder();
            var canvasRect = rt.parent as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, e.position, e.pressEventCamera, out Vector2 localPoint);
            dragOffset = rt.anchoredPosition - localPoint;
            isReordering = true;
        }

        if (isReordering) HandleReorderDrag(e);
        else HandleSwipeDrag(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (parentScroll != null) parentScroll.vertical = true;
        if (isReordering) EndReorder();
        else HandleSwipeEnd(e);
    }

    #region Swipe Logic

    void HandleSwipeDrag(PointerEventData e)
    {
        var canvasRect = rt.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, e.position, e.pressEventCamera, out Vector2 localPoint);

        float deltaX = localPoint.x - swipeStartLocalX;
        rt.anchoredPosition = originalPos + new Vector2(deltaX, 0f);
    }

    void HandleSwipeEnd(PointerEventData e)
    {
        if (swipeState == SwipeState.Moving) return;
        float finalDelta = rt.anchoredPosition.x - originalPos.x;

        if (swipeState == SwipeState.DeleteShown && finalDelta >= thresholdPixels)
        {
            ReturnToOriginal(); return;
        }
        if (swipeState == SwipeState.ApplyShown && finalDelta <= -revealDeleteThreshold)
        {
            ReturnToOriginal(); return;
        }

        if (finalDelta <= -revealDeleteThreshold)
        {
            ShowDeleteButton(); HideApplyButton();
            Move(-moveDistance, () => swipeState = SwipeState.DeleteShown);
            swipeState = SwipeState.Moving;
        }
        else if (finalDelta >= thresholdPixels)
        {
            ShowApplyButton(); HideDeleteButton();
            Move(+moveDistance, () => swipeState = SwipeState.ApplyShown);
            swipeState = SwipeState.Moving;
        }
        else
        {
            HideDeleteButton(); HideApplyButton();
            Move(0f, () => swipeState = SwipeState.Normal);
            swipeState = SwipeState.Moving;
        }
    }

    void ShowDeleteButton() { deleteButton?.gameObject.SetActive(true); deleteButton.interactable = true; swipeState = SwipeState.DeleteShown; }
    void HideDeleteButton() { deleteButton?.gameObject.SetActive(false); deleteButton.interactable = false; if (swipeState == SwipeState.DeleteShown) swipeState = SwipeState.Normal; }
    void ShowApplyButton() { applyButton?.gameObject.SetActive(true); applyButton.interactable = true; swipeState = SwipeState.ApplyShown; }
    void HideApplyButton() { applyButton?.gameObject.SetActive(false); applyButton.interactable = false; if (swipeState == SwipeState.ApplyShown) swipeState = SwipeState.Normal; }

    #endregion

    #region Delete + Backend

    public void OnDeleteButton()
    {
        Debug.Log($"삭제 버튼 클릭: id={data.id}, todo={data.todo}");
        // 삭제용 JSON을 data 전체로 생성
        TodoListData deleteList = new TodoListData();
        deleteList.tasks = new List<TodoItemData> { data };
        string json = JsonUtility.ToJson(deleteList);
        Debug.Log("삭제용 JSON: " + json);
        deleteButton.interactable = false;
        cg.blocksRaycasts = false;
        StartCoroutine(DeleteThenAnimate(json));
    }

    IEnumerator DeleteThenAnimate(string json)
    {
        // 1) id 포함 전체 JSON 사용
        using var req = new UnityWebRequest($"{backendUrl}/bulk", "DELETE");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" };
        req.downloadHandler = new DownloadHandlerBuffer();
        yield return req.SendWebRequest();

        // 3) 성공 시 애니메이션, 실패 시 복구
        if (req.result == UnityWebRequest.Result.Success)
        {
            Move(+moveDistance, () => {
                StartCoroutine(AnimateDelete(
                    rt.anchoredPosition,
                    rt.anchoredPosition + Vector2.right * moveDistance
                ));
            });
        }
        else
        {
            ReturnToOriginal();
            cg.blocksRaycasts = true;
        }
    }

    #endregion

    #region Apply + Backend

    void OnApplyButton()
    {
        HideApplyButton();
        cg.blocksRaycasts = false;
        StartCoroutine(ApplyThenAnimate());
    }

    IEnumerator ApplyThenAnimate()
    {
        var wrapper = new { tasks = new[] { new { todo = this.todo, date = this.date, time = this.time } } };
        string json = JsonUtility.ToJson(wrapper);

        // userId 없이 bulk/accept 호출
        using var req = new UnityWebRequest($"{backendUrl}/bulk/accept", "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" };
        req.downloadHandler = new DownloadHandlerBuffer();
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Move(+moveDistance, () => {
                StartCoroutine(AnimateDelete(rt.anchoredPosition, rt.anchoredPosition + Vector2.right * moveDistance));
            });
        }
        else
        {
            ReturnToOriginal();
            cg.blocksRaycasts = true;
        }
    }

    #endregion

    #region Animation Helpers

    void ReturnToOriginal()
    {
        HideDeleteButton(); HideApplyButton();
        Move(0f, () => swipeState = SwipeState.Normal);
        swipeState = SwipeState.Moving;
    }

    void Move(float distance, Action onComplete)
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        Vector2 target = originalPos + new Vector2(distance, 0f);
        moveCoroutine = StartCoroutine(AnimateMove(rt.anchoredPosition, target, onComplete));
    }

    IEnumerator AnimateMove(Vector2 from, Vector2 to, Action onComplete)
    {
        float elapsed = 0;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float e = 1f - (1f - t) * (1f - t);
            rt.anchoredPosition = Vector2.Lerp(from, to, e);
            yield return null;
        }
        rt.anchoredPosition = to;
        onComplete?.Invoke();
    }

    IEnumerator AnimateDelete(Vector2 from, Vector2 to)
    {
        float elapsed = 0;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float e = 1f - (1f - t) * (1f - t);
            rt.anchoredPosition = Vector2.Lerp(from, to, e);
            cg.alpha = Mathf.Lerp(1f, 0f, e);
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