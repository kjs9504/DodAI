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
        // World-Space Canvas ã��
        _parentCanvas = GetComponentInParent<Canvas>();
        if (_parentCanvas == null)
            Debug.LogError("DraggableUI: �θ� Canvas�� �����ϴ�.");

        _canvasRect = _parentCanvas.GetComponent<RectTransform>();

        // Raycast ����� �ǵ��� blocksRaycasts �ѱ�
        var cg = GetComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
    }

    // �巡�� ���� �� �� �� ȣ��
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[DraggableUI] OnBeginDrag at screen {eventData.position}");

        Vector3 worldPoint;
        if (eventData.pointerCurrentRaycast.isValid)
        {
            worldPoint = eventData.pointerCurrentRaycast.worldPosition;
            Debug.Log($"[DraggableUI]  �� worldPoint from raycast: {worldPoint}");
        }
        else
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                _canvasRect,
                eventData.position,
                _parentCanvas.worldCamera,
                out worldPoint);
            Debug.Log($"[DraggableUI]  �� worldPoint from ScreenPoint: {worldPoint}");
        }

        _grabOffset = transform.position - worldPoint;
        Debug.Log($"[DraggableUI]  �� grabOffset: {_grabOffset}");

        Camera cam = eventData.enterEventCamera != null
             ? eventData.enterEventCamera
             : (_parentCanvas.worldCamera != null
                ? _parentCanvas.worldCamera
                : Camera.main);
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            _canvasRect, eventData.position, cam, out worldPoint);
    }

    // �巡�� �� �� ������ ȣ��
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

    // �巡�װ� ������ �� ȣ��
    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("[DraggableUI] OnEndDrag");
        // �ʿ��ϴٸ� �߰� ������
    }
}



