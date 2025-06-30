using UnityEngine;
using UnityEngine.XR;

public class PersistOVRCameraRig : MonoBehaviour
{
    // �̱��� �ν��Ͻ� �����
    private static PersistOVRCameraRig _instance;

    void Awake()
    {
        // �̹� �ν��Ͻ��� ������ �ڽ��� �ı�
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // ù ������ �ν��Ͻ��� ���⿡ �Ҵ��ϰ� �ı� ����
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}

