using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PalmUpRecognizer : MonoBehaviour
{
    [Tooltip("HandAnchorUpdater�� �����ϴ� Palm ��Ŀ Transform�� �Ҵ��ϼ���.")]
    public Transform palmAnchor;

    [Tooltip("�չٴ� ���� �� �����(dot) > threshold�� �� ������ ���ԡ����� �����մϴ�.")]
    [Range(0.5f, 1f)]
    public float threshold = 0.75f;

    /// <summary>
    /// �չٴ��� ���� ���ϴ� ���°� �ٲ� ������ ȣ��˴ϴ�. 
    /// true: ���� ����, false: ���� ������ ����
    /// </summary>
    public event Action<bool> OnPalmUpChanged;

    bool _wasUp;

    void Reset()
    {
        // �⺻������ �� ������Ʈ�� ���� GameObject�� Transform�� ��Ŀ�� ���
        if (palmAnchor == null)
            palmAnchor = transform;
    }

    void Update()
    {
        if (palmAnchor == null)
            return;

        // palmAnchor.up�� �չٴ� ���� ���ͷ� ����
        Vector3 palmNormal = palmAnchor.rotation * Vector3.up;
        bool isUp = Vector3.Dot(palmNormal, Vector3.up) > threshold;

        if (isUp != _wasUp)
        {
            _wasUp = isUp;
            OnPalmUpChanged?.Invoke(isUp);
        }
    }
}
