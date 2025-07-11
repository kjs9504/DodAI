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
        // ī�޶� ���ϵ��� ȸ��
        transform.rotation = Quaternion.LookRotation(transform.position - cam.position);
    }
}
