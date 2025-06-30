using UnityEngine;

public class ThumbnailMove : MonoBehaviour
{
    private Transform uiTarget;
    private Vector3 offsetPosition;
    private Quaternion offsetRotation;

    void Start()
    {
        // Additive로 로드된 씬이므로 모든 씬 포함해서 탐색 가능
        GameObject found = GameObject.FindWithTag("tntag");
        if (found != null)
        {
            uiTarget = found.transform;
            offsetPosition = transform.position - uiTarget.position;
            offsetRotation = Quaternion.Inverse(uiTarget.rotation) * transform.rotation;
        }
        else
        {
            Debug.LogError("UI 타겟(tntag)을 찾지 못했습니다.");
        }
    }

    void LateUpdate()
    {
        if (uiTarget == null) return;

        // UI의 위치와 회전을 따라감
        transform.position = uiTarget.position + offsetPosition;
        transform.rotation = uiTarget.rotation * offsetRotation;
    }
}

