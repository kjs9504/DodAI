using UnityEngine;
using UnityEngine.XR.Hands;  // Handedness

[DisallowMultipleComponent]
public class ObjectCreateManager : MonoBehaviour
{
    [Tooltip("AnyPalmUpDetector를 할당하세요.")]
    public AnyPalmUpDetector detector;

    [Tooltip("스폰할 프리팹")]
    public GameObject prefab;

    [Tooltip("바닥 Y 좌표 (예: 0)")]
    public float groundY = 0f;

    [Tooltip("손 높이가 이 값(미터)까지 올라갈 때까지 체크")]
    public float maxHandHeight = 1.5f;

    [Tooltip("오브젝트 최고 상승 높이")]
    public float maxRiseHeight = 2f;

    GameObject _instance;

    void Start()
    {
        if (detector == null)
        {
            Debug.LogError("AnyPalmUpDetector를 할당하세요.", this);
            enabled = false;
            return;
        }
        detector.OnAnyPalmUpChanged += OnAnyPalmUpChanged;
    }

    void OnAnyPalmUpChanged(bool anyUp, Handedness hand)
    {
        if (anyUp && _instance == null)
        {
            // 바닥에 생성
            Vector3 spawnPos = new Vector3(0f, groundY, 0f);
            _instance = Instantiate(prefab, spawnPos, Quaternion.identity);
        }
        else if (!anyUp && _instance != null)
        {
            Destroy(_instance);
            _instance = null;
        }
    }

    void Update()
    {
        if (_instance == null)
            return;

        // 올라간 손의 Transform
        Transform upAnchor = null;
        if (detector.CurrentUpHand == Handedness.Left)
            upAnchor = detector.leftRecognizer.palmAnchor;
        else if (detector.CurrentUpHand == Handedness.Right)
            upAnchor = detector.rightRecognizer.palmAnchor;

        if (upAnchor == null)
            return;

        // 손 높이만큼 0~1 정규화
        float handY = upAnchor.position.y;
        float t = Mathf.Clamp01((handY - groundY) / maxHandHeight);
        // 바닥Y에서 maxRiseHeight까지 보간
        float newY = groundY + t * maxRiseHeight;

        var p = _instance.transform.position;
        _instance.transform.position = new Vector3(p.x, newY, p.z);
    }
}
