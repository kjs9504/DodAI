using UnityEngine;
using UnityEngine.EventSystems;

public class DebugPointer : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public void OnPointerClick(PointerEventData e)
    {
        Debug.Log(">> OnPointerClick");
    }
    public void OnBeginDrag(PointerEventData e)
    {
        Debug.Log(">> OnBeginDrag");
    }
    public void OnDrag(PointerEventData e)
    {
        Debug.Log(">> OnDrag: " + e.delta);
    }
    public void OnEndDrag(PointerEventData e)
    {
        Debug.Log(">> OnEndDrag");
    }
}

