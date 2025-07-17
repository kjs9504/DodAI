using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
public class EmojiController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rt;
    private Canvas rootCanvas;
    private CanvasGroup cg;
    private Vector2 pointerOffset;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
            Debug.LogError("DragDropController: �θ� Canvas�� �����ϴ�.");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        cg.blocksRaycasts = false;  // �巡�� �߿� Raycast ���� �ٸ� UI�� �浹 ����

        // Ŭ���� ������ ��� ��Ŀ ��ġ �� ������ ���
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt, eventData.position, eventData.pressEventCamera, out pointerOffset);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // ȭ�� ��ǥ �� ĵ���� ���� ��ǥ ��ȯ
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPos);

        // ��Ŀ�� ��ġ ����
        rt.anchoredPosition = localPointerPos + pointerOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        cg.blocksRaycasts = true;
        // ��� �� �߰� ������ �ʿ��ϸ� ���⿡!
    }
}

