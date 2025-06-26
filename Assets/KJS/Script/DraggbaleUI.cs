using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    Canvas _parentCanvas;
    RectTransform _canvasRect;
    Vector3 _grabOffset;

    void Awake()
    {
        // World-Space Canvas 찾기
        _parentCanvas = GetComponentInParent<Canvas>();
        if (_parentCanvas == null)
            Debug.LogError("DraggableUI: 부모 Canvas가 없습니다.");

        _canvasRect = _parentCanvas.GetComponent<RectTransform>();

        // Raycast 대상이 되도록 blocksRaycasts 켜기
        var cg = GetComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
    }

    // 드래그 시작 시 한 번 호출
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[DraggableUI] OnBeginDrag at screen {eventData.position}");

        Vector3 worldPoint;
        if (eventData.pointerCurrentRaycast.isValid)
        {
            worldPoint = eventData.pointerCurrentRaycast.worldPosition;
            Debug.Log($"[DraggableUI]  → worldPoint from raycast: {worldPoint}");
        }
        else
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                _canvasRect,
                eventData.position,
                _parentCanvas.worldCamera,
                out worldPoint);
            Debug.Log($"[DraggableUI]  → worldPoint from ScreenPoint: {worldPoint}");
        }

        _grabOffset = transform.position - worldPoint;
        Debug.Log($"[DraggableUI]  → grabOffset: {_grabOffset}");

        Camera cam = eventData.enterEventCamera != null
             ? eventData.enterEventCamera
             : (_parentCanvas.worldCamera != null
                ? _parentCanvas.worldCamera
                : Camera.main);
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            _canvasRect, eventData.position, cam, out worldPoint);
    }

    // 드래그 중 매 프레임 호출
    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log($"[DraggableUI] OnDrag delta {eventData.delta}");

        Vector3 worldPoint;
        if (eventData.pointerCurrentRaycast.isValid)
        {
            worldPoint = eventData.pointerCurrentRaycast.worldPosition;
        }
        else
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                _canvasRect,
                eventData.position,
                _parentCanvas.worldCamera,
                out worldPoint);
        }

        transform.position = worldPoint + _grabOffset;

        Camera cam = eventData.enterEventCamera != null
             ? eventData.enterEventCamera
             : (_parentCanvas.worldCamera != null
                ? _parentCanvas.worldCamera
                : Camera.main);
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            _canvasRect, eventData.position, cam, out worldPoint);
    }

    // 드래그가 끝났을 때 호출
    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("[DraggableUI] OnEndDrag");
        // 필요하다면 추가 로직…
    }
}



