using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PalmUpRecognizer : MonoBehaviour
{
    [Tooltip("HandAnchorUpdater가 갱신하는 Palm 앵커 Transform을 할당하세요.")]
    public Transform palmAnchor;

    [Tooltip("손바닥 법선 · 월드업(dot) > threshold일 때 ‘위로 향함’으로 간주합니다.")]
    [Range(0.5f, 1f)]
    public float threshold = 0.75f;

    /// <summary>
    /// 손바닥이 위로 향하는 상태가 바뀔 때마다 호출됩니다. 
    /// true: 위로 향함, false: 위로 향하지 않음
    /// </summary>
    public event Action<bool> OnPalmUpChanged;

    bool _wasUp;

    void Reset()
    {
        // 기본적으로 이 컴포넌트가 붙은 GameObject의 Transform을 앵커로 사용
        if (palmAnchor == null)
            palmAnchor = transform;
    }

    void Update()
    {
        if (palmAnchor == null)
            return;

        // palmAnchor.up을 손바닥 법선 벡터로 가정
        Vector3 palmNormal = palmAnchor.rotation * Vector3.up;
        bool isUp = Vector3.Dot(palmNormal, Vector3.up) > threshold;

        if (isUp != _wasUp)
        {
            _wasUp = isUp;
            OnPalmUpChanged?.Invoke(isUp);
        }
    }
}
