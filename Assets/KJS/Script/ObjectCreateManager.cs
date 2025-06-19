using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.Hands;    // XRHandSubsystem, XRHand, Handedness, XRHandJointID

[DisallowMultipleComponent]
public class ObjectCreateManager : MonoBehaviour
{
    [Header("1) AnyPalmUpDetector를 할당하세요.")]
    public AnyPalmUpDetector detector;

    [Header("2) 스폰할 프리팹")]
    public GameObject prefab;

    [Header("3) 바닥 Y 좌표 (예: 0)")]
    public float groundY = 0f;

    [Header("4) 시선 기준 생성 거리 (미터)")]
    public float spawnDistance = 1.0f;

    [Header("5) 손 높이가 이 값까지 올라갈 때 보간 (미터)")]
    public float maxHandHeight = 1.5f;

    [Header("6) 오브젝트 최고 상승 높이 (미터)")]
    public float maxRiseHeight = 2f;

    [Header("7) 감지할 관절 (보통 Palm)")]
    public XRHandJointID jointID = XRHandJointID.Palm;

    GameObject _instance;
    XRHandSubsystem _handSubsystem;
    Handedness _currentUpHand = Handedness.Invalid;   // 이벤트로 받은 손을 저장

    void Awake()
    {
        if (detector == null)
        {
            Debug.LogError("AnyPalmUpDetector를 할당하세요.", this);
            enabled = false;
            return;
        }
        // XRHandSubsystem 캐싱
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        _handSubsystem = subsystems.FirstOrDefault(s => s.running)
                       ?? subsystems.FirstOrDefault();
        if (_handSubsystem == null)
        {
            Debug.LogError("XRHandSubsystem을 찾을 수 없습니다.", this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        detector.OnAnyPalmUpChanged += OnAnyPalmUpChanged;
    }

    void OnDisable()
    {
        detector.OnAnyPalmUpChanged -= OnAnyPalmUpChanged;
    }

    // ① 이벤트 파라미터 hand 를 바로 _currentUpHand 에 저장
    void OnAnyPalmUpChanged(bool anyUp, Handedness hand)
    {
        Debug.Log($"OnAnyPalmUpChanged → anyUp: {anyUp}, hand: {hand}");
        if (anyUp && _instance == null)
        {
            _currentUpHand = hand;
            SpawnObject();
        }
        else if (!anyUp && _instance != null)
        {
            Destroy(_instance);
            _instance = null;
            _currentUpHand = Handedness.Invalid;
        }
    }

    // (스폰 로직 분리)
    void SpawnObject()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("Main Camera를 찾을 수 없습니다.");
            return;
        }

        Vector3 forward = cam.transform.forward.normalized;
        Vector3 spawnPos = cam.transform.position + forward * spawnDistance;
        spawnPos.y = groundY;

        Quaternion spawnRot = Quaternion.LookRotation(forward, Vector3.up);
        _instance = Instantiate(prefab, spawnPos, spawnRot);
    }

    void Update()
    {
        // ② _currentUpHand 이 유효할 때만 Y 보간
        if (_instance == null || _currentUpHand == Handedness.Invalid)
            return;

        XRHand hand = (_currentUpHand == Handedness.Left)
                      ? _handSubsystem.leftHand
                      : _handSubsystem.rightHand;

        if (!hand.GetJoint(jointID).TryGetPose(out Pose pose))
            return;

        float t = Mathf.Clamp01((pose.position.y - groundY) / maxHandHeight);
        float newY = groundY + t * maxRiseHeight;

        var p = _instance.transform.position;
        _instance.transform.position = new Vector3(p.x, newY, p.z);
    }
}
