using UnityEngine;
using UnityEngine.XR;

public class PersistOVRCameraRig : MonoBehaviour
{
    // 싱글톤 인스턴스 저장용
    private static PersistOVRCameraRig _instance;

    void Awake()
    {
        // 이미 인스턴스가 있으면 자신은 파괴
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 첫 생성된 인스턴스면 여기에 할당하고 파괴 방지
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}

