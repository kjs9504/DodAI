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
            Debug.Log("OVRCameraRig Ȱ��ȭ �Ϸ�!");
        }
        else
        {
            Debug.LogWarning("OVRCameraRig�� ã�� ���߽��ϴ�.");
        }
    }
}