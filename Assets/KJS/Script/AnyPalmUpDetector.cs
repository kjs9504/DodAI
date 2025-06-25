using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.Hands;

[DisallowMultipleComponent]
public class AnyPalmUpDetector : MonoBehaviour
{
    [Header("감지할 관절 (보통 Palm)")]
    public XRHandJointID jointID = XRHandJointID.Palm;

    [Header("Up으로 판정할 Dot 임계값 (0~1)")]
    [Range(0f, 1f)]
    public float upDotThreshold = 0.5f;

    /// <summary>
    /// 하나 이상의 손바닥이 위로 향함 상태가 변경될 때마다 호출됩니다.
    /// 첫 번째 파라미터: true=하나 이상 Up, false=모두 Down
    /// 두 번째 파라미터: 이벤트를 발생시킨 손 (Left/Right)
    /// </summary>
    public event Action<bool, Handedness> OnAnyPalmUpChanged;

    bool _leftUp = false;
    bool _rightUp = false;
    bool _lastAnyUp = false;

    XRHandSubsystem _handSubsystem;

    void Awake()
    {
        // XRHandSubsystem 캐싱 (매 프레임 GetSubsystems 호출 방지)
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        _handSubsystem = subsystems.Find(s => s.running) ?? subsystems.FirstOrDefault();
        if (_handSubsystem == null)
        {
            Debug.LogError("XRHandSubsystem을 찾을 수 없습니다.", this);
            enabled = false;
        }
    }

    void Update()
    {
        if (_handSubsystem == null || !_handSubsystem.running)
            return;

        // 왼손/오른손 각각 Up 상태 판정
        bool newLeftUp = IsPalmUp(Handedness.Left);
        bool newRightUp = IsPalmUp(Handedness.Right);

        // 상태 변화가 있을 때만 검사
        if (newLeftUp != _leftUp) Fire(newLeftUp, Handedness.Left);
        if (newRightUp != _rightUp) Fire(newRightUp, Handedness.Right);

        // 내부 상태 갱신
        _leftUp = newLeftUp;
        _rightUp = newRightUp;
    }

    bool IsPalmUp(Handedness hand)
    {
        var handData = hand == Handedness.Left ? _handSubsystem.leftHand : _handSubsystem.rightHand;
        if (!handData.GetJoint(jointID).TryGetPose(out Pose pose))
            return false;

        // 팜 노말 계산
        Vector3 palmNormal = pose.rotation * Vector3.up;
        // Up 판정 (Dot ≥ Threshold)
        return Vector3.Dot(palmNormal, Vector3.up) >= upDotThreshold;
    }

    void Fire(bool thisUp, Handedness hand)
    {
        bool anyUp = (_leftUp | thisUp) || (_rightUp | thisUp);
        // anyUp이 이전과 달라질 때만 이벤트
        if (anyUp != _lastAnyUp)
        {
            OnAnyPalmUpChanged?.Invoke(anyUp, hand);
            _lastAnyUp = anyUp;
        }
    }

    /// <summary>현재 하나 이상의 손바닥이 위를 향하고 있는지</summary>
    public bool IsAnyUp => _leftUp || _rightUp;

    /// <summary>
    /// 위로 향한 손을 반환 (Left→Right 우선). 없으면 Invalid
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



