using UnityEngine;
using UnityEngine.XR.Hands;  // Handedness

[DisallowMultipleComponent]
public class ObjectCreateManager : MonoBehaviour
{
    [Tooltip("AnyPalmUpDetector�� �Ҵ��ϼ���.")]
    public AnyPalmUpDetector detector;

    [Tooltip("������ ������")]
    public GameObject prefab;

    [Tooltip("�ٴ� Y ��ǥ (��: 0)")]
    public float groundY = 0f;

    [Tooltip("�� ���̰� �� ��(����)���� �ö� ������ üũ")]
    public float maxHandHeight = 1.5f;

    [Tooltip("������Ʈ �ְ� ��� ����")]
    public float maxRiseHeight = 2f;

    GameObject _instance;

    void Start()
    {
        if (detector == null)
        {
            Debug.LogError("AnyPalmUpDetector�� �Ҵ��ϼ���.", this);
            enabled = false;
            return;
        }
        detector.OnAnyPalmUpChanged += OnAnyPalmUpChanged;
    }

    void OnAnyPalmUpChanged(bool anyUp, Handedness hand)
    {
        if (anyUp && _instance == null)
        {
            // �ٴڿ� ����
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

        // �ö� ���� Transform
        Transform upAnchor = null;
        if (detector.CurrentUpHand == Handedness.Left)
            upAnchor = detector.leftRecognizer.palmAnchor;
        else if (detector.CurrentUpHand == Handedness.Right)
            upAnchor = detector.rightRecognizer.palmAnchor;

        if (upAnchor == null)
            return;

        // �� ���̸�ŭ 0~1 ����ȭ
        float handY = upAnchor.position.y;
        float t = Mathf.Clamp01((handY - groundY) / maxHandHeight);
        // �ٴ�Y���� maxRiseHeight���� ����
        float newY = groundY + t * maxRiseHeight;

        var p = _instance.transform.position;
        _instance.transform.position = new Vector3(p.x, newY, p.z);
    }
}
