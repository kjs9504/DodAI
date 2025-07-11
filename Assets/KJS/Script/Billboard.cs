using UnityEngine;

public class Billboard : MonoBehaviour
{
    Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        // 카메라를 향하도록 회전
        transform.rotation = Quaternion.LookRotation(transform.position - cam.position);
    }
}
