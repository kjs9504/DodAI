using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.Hands;    // XRHandSubsystem, XRHand, Handedness, XRHandJointID

[DisallowMultipleComponent]
public class ObjectCreateManager : MonoBehaviour
{
    [Header("1) AnyPalmUpDetector�� �Ҵ��ϼ���.")]
    public AnyPalmUpDetector detector;

    [Header("2) ������ ������")]
    public GameObject prefab;

    [Header("3) �ٴ� Y ��ǥ (��: 0)")]
    public float groundY = 0f;

    [Header("4) �ü� ���� ���� �Ÿ� (����)")]
    public float spawnDistance = 1.0f;

    [Header("5) �� ���̰� �� ������ �ö� �� ���� (����)")]
    public float maxHandHeight = 1.5f;

    [Header("6) ������Ʈ �ְ� ��� ���� (����)")]
    public float maxRiseHeight = 2f;

    [Header("7) ������ ���� (���� Palm)")]
    public XRHandJointID jointID = XRHandJointID.Palm;

    GameObject _instance;
    XRHandSubsystem _handSubsystem;
    Handedness _currentUpHand = Handedness.Invalid;   // �̺�Ʈ�� ���� ���� ����

    void Awake()
    {
        if (detector == null)
        {
            Debug.LogError("AnyPalmUpDetector�� �Ҵ��ϼ���.", this);
            enabled = false;
            return;
        }
        // XRHandSubsystem ĳ��
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        _handSubsystem = subsystems.FirstOrDefault(s => s.running)
                       ?? subsystems.FirstOrDefault();
        if (_handSubsystem == null)
        {
            Debug.LogError("XRHandSubsystem�� ã�� �� �����ϴ�.", this);
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

    // �� �̺�Ʈ �Ķ���� hand �� �ٷ� _currentUpHand �� ����
    void OnAnyPalmUpChanged(bool anyUp, Handedness hand)
    {
        Debug.Log($"OnAnyPalmUpChanged �� anyUp: {anyUp}, hand: {hand}");
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

    // (���� ���� �и�)
    void SpawnObject()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("Main Camera�� ã�� �� �����ϴ�.");
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
        // �� _currentUpHand �� ��ȿ�� ���� Y ����
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
