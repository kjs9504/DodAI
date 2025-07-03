using UnityEngine;
using UnityEngine.EventSystems;

public class CellDebugLogger : MonoBehaviour, IPointerClickHandler
{
    void Start()
    {
        var bc = GetComponent<BoxCollider>();
        if (bc != null)
        {
            // ���� ����
            Debug.Log($"{name} �� bc.center local = {bc.center}, size = {bc.size}");
            // ���� ����
            Vector3 worldCtr = transform.TransformPoint(bc.center);
            Debug.Log($"{name} �� bc.center world = {worldCtr}");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Vector3 wp = eventData.pointerCurrentRaycast.worldPosition;
        Debug.Log($"{name} Ŭ��! screenPos={eventData.position} worldPos={wp}");
    }

    void OnDrawGizmos()
    {
        var bc = GetComponent<BoxCollider>();
        if (bc != null)
        {
            Gizmos.color = Color.red;
            Vector3 worldCtr = transform.TransformPoint(bc.center);
            Gizmos.DrawWireCube(worldCtr, bc.size);
        }
    }
}



