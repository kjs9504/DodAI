using System;
using UnityEngine;
using UnityEngine.XR.Hands;  // Handedness

[DisallowMultipleComponent]
public class AnyPalmUpDetector : MonoBehaviour
{
    [Tooltip("왼손 PalmUpRecognizer")]
    public PalmUpRecognizer leftRecognizer;

    [Tooltip("오른손 PalmUpRecognizer")]
    public PalmUpRecognizer rightRecognizer;

    /// <summary>
    /// 하나 이상의 손바닥이 위로 향함 상태가 바뀔 때마다 호출됩니다.
    /// 첫 번째 파라미터: true = 하나 이상 위로 향함, false = 모두 내려감
    /// 두 번째 파라미터: 최근에 이벤트를 발생시킨 손 (Handedness.Left / Right)
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
        // 하나라도 up 이면 true, 모두 false 면 false
        bool anyUp = _leftUp || _rightUp;
        OnAnyPalmUpChanged?.Invoke(anyUp, hand);
    }

    /// <summary>현재 하나 이상의 손바닥이 위를 향하고 있는지</summary>
    public bool IsAnyUp => _leftUp || _rightUp;

    /// <summary>위를 향한 손이 Left/Right 둘 다일 수 있고, 
    /// 우선순위는 Left → Right (필요시 로직 수정)</summary>
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

