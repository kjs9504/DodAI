using UnityEngine;

public class UITransformLock : MonoBehaviour
{
    private Vector3 lockedScale = new Vector3(0.627529442f, 0.888999999f, 0.5f);
    private Quaternion lockedRotation;

    void Start()
    {
        lockedRotation = transform.rotation;
    }

    void LateUpdate()
    {
        // 스케일 고정
        transform.localScale = lockedScale;

        // 회전도 원하는 대로 고정 (선택사항)
        // transform.rotation = lockedRotation;
    }
}