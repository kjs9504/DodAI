using UnityEngine;
using Oculus.Interaction;

[RequireComponent(typeof(PointableCanvasModule))]
public class DebugPointerPlane : MonoBehaviour
{
    public float radius = 0.01f;
    void OnDrawGizmos()
    {
        var module = GetComponent<PointableCanvasModule>();
        if (!module) return;
        // 내부 카메라 가져오기 (리플렉션)
        var camField = typeof(PointableCanvasModule)
            .GetField("_pointerEventCamera",
                      System.Reflection.BindingFlags.NonPublic |
                      System.Reflection.BindingFlags.Instance);
        var cam = camField?.GetValue(module) as Camera;
        var canvas = GetComponent<Canvas>();
        if (cam == null || canvas == null) return;

        Plane p = new Plane(-canvas.transform.forward, canvas.transform.position);
        Ray r = new Ray(cam.transform.position, cam.transform.forward);
        if (p.Raycast(r, out float enter))
        {
            Vector3 hit = r.GetPoint(enter);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(hit, radius);
        }
    }
}

