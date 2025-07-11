using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraActivator : MonoBehaviour
{
    void Start()
    {
        var rig = FindObjectOfType<OVRCameraRig>(true);
        if (rig != null)
        {
            rig.gameObject.SetActive(true);
            Debug.Log("OVRCameraRig 활성화 완료!");
        }
        else
        {
            Debug.LogWarning("OVRCameraRig를 찾지 못했습니다.");
        }
    }
}