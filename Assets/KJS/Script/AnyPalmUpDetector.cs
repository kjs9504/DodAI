using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.Hands;

[DisallowMultipleComponent]
public class AnyPalmUpDetector : MonoBehaviour
{
    [Header("������ ���� (���� Palm)")]
    public XRHandJointID jointID = XRHandJointID.Palm;

    [Header("Up���� ������ Dot �Ӱ谪 (0~1)")]
    [Range(0f, 1f)]
    public float upDotThreshold = 0.5f;

    /// <summary>
    /// �ϳ� �̻��� �չٴ��� ���� ���� ���°� ����� ������ ȣ��˴ϴ�.
    /// ù ��° �Ķ����: true=�ϳ� �̻� Up, false=��� Down
    /// �� ��° �Ķ����: �̺�Ʈ�� �߻���Ų �� (Left/Right)
    /// </summary>
    public event Action<bool, Handedness> OnAnyPalmUpChanged;

    bool _leftUp = false;
    bool _rightUp = false;
    bool _lastAnyUp = false;

    XRHandSubsystem _handSubsystem;

    void Awake()
    {
        // XRHandSubsystem ĳ�� (�� ������ GetSubsystems ȣ�� ����)
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        _handSubsystem = subsystems.Find(s => s.running) ?? subsystems.FirstOrDefault();
        if (_handSubsystem == null)
        {
            Debug.LogError("XRHandSubsystem�� ã�� �� �����ϴ�.", this);
            enabled = false;
        }
    }

    void Update()
    {
        if (_handSubsystem == null || !_handSubsystem.running)
            return;

        // �޼�/������ ���� Up ���� ����
        bool newLeftUp = IsPalmUp(Handedness.Left);
        bool newRightUp = IsPalmUp(Handedness.Right);

        // ���� ��ȭ�� ���� ���� �˻�
        if (newLeftUp != _leftUp) Fire(newLeftUp, Handedness.Left);
        if (newRightUp != _rightUp) Fire(newRightUp, Handedness.Right);

        // ���� ���� ����
        _leftUp = newLeftUp;
        _rightUp = newRightUp;
    }

    bool IsPalmUp(Handedness hand)
    {
        var handData = hand == Handedness.Left ? _handSubsystem.leftHand : _handSubsystem.rightHand;
        if (!handData.GetJoint(jointID).TryGetPose(out Pose pose))
            return false;

        // �� �븻 ���
        Vector3 palmNormal = pose.rotation * Vector3.up;
        // Up ���� (Dot �� Threshold)
        return Vector3.Dot(palmNormal, Vector3.up) >= upDotThreshold;
    }

    void Fire(bool thisUp, Handedness hand)
    {
        bool anyUp = (_leftUp | thisUp) || (_rightUp | thisUp);
        // anyUp�� ������ �޶��� ���� �̺�Ʈ
        if (anyUp != _lastAnyUp)
        {
            OnAnyPalmUpChanged?.Invoke(anyUp, hand);
            _lastAnyUp = anyUp;
        }
    }

    /// <summary>���� �ϳ� �̻��� �չٴ��� ���� ���ϰ� �ִ���</summary>
    public bool IsAnyUp => _leftUp || _rightUp;

    /// <summary>
    /// ���� ���� ���� ��ȯ (Left��Right �켱). ������ Invalid
    /// </summary>
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



