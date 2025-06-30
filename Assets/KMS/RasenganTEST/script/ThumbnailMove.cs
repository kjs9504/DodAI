using UnityEngine;

public class ThumbnailMove : MonoBehaviour
{
    private Transform uiTarget;
    private Vector3 offsetPosition;
    private Quaternion offsetRotation;

    void Start()
    {
        // Additive�� �ε�� ���̹Ƿ� ��� �� �����ؼ� Ž�� ����
        GameObject found = GameObject.FindWithTag("tntag");
        if (found != null)
        {
            uiTarget = found.transform;
            offsetPosition = transform.position - uiTarget.position;
            offsetRotation = Quaternion.Inverse(uiTarget.rotation) * transform.rotation;
        }
        else
        {
            Debug.LogError("UI Ÿ��(tntag)�� ã�� ���߽��ϴ�.");
        }
    }

    void LateUpdate()
    {
        if (uiTarget == null) return;

        // UI�� ��ġ�� ȸ���� ����
        transform.position = uiTarget.position + offsetPosition;
        transform.rotation = uiTarget.rotation * offsetRotation;
    }
}

