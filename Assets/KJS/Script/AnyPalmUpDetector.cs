using System;
using UnityEngine;
using UnityEngine.XR.Hands;  // Handedness

[DisallowMultipleComponent]
public class AnyPalmUpDetector : MonoBehaviour
{
    [Tooltip("�޼� PalmUpRecognizer")]
    public PalmUpRecognizer leftRecognizer;

    [Tooltip("������ PalmUpRecognizer")]
    public PalmUpRecognizer rightRecognizer;

    /// <summary>
    /// �ϳ� �̻��� �չٴ��� ���� ���� ���°� �ٲ� ������ ȣ��˴ϴ�.
    /// ù ��° �Ķ����: true = �ϳ� �̻� ���� ����, false = ��� ������
    /// �� ��° �Ķ����: �ֱٿ� �̺�Ʈ�� �߻���Ų �� (Handedness.Left / Right)
    /// </summary>
    public event Action<bool, Handedness> OnAnyPalmUpChanged;

    bool _leftUp;
    bool _rightUp;

    void Awake()
    {
        if (leftRecognizer != null)
            leftRecognizer.OnPalmUpChanged += up => { _leftUp = up; Fire(up, Handedness.Left); };
        if (rightRecognizer != null)
            rightRecognizer.OnPalmUpChanged += up => { _rightUp = up; Fire(up, Handedness.Right); };
    }

    void Fire(bool thisUp, Handedness hand)
    {
        // �ϳ��� up �̸� true, ��� false �� false
        bool anyUp = _leftUp || _rightUp;
        OnAnyPalmUpChanged?.Invoke(anyUp, hand);
    }

    /// <summary>���� �ϳ� �̻��� �չٴ��� ���� ���ϰ� �ִ���</summary>
    public bool IsAnyUp => _leftUp || _rightUp;

    /// <summary>���� ���� ���� Left/Right �� ���� �� �ְ�, 
    /// �켱������ Left �� Right (�ʿ�� ���� ����)</summary>
    public Handedness CurrentUpHand
    {
        get
        {
            if (_leftUp) return Handedness.Left;
            if (_rightUp) return Handedness.Right;
            return Handedness.Invalid;
        }
    }
}

