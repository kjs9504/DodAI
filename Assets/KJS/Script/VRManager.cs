using UnityEngine;
using UnityEngine.SceneManagement;

public class VRManager : MonoBehaviour
{
    public string vrSceneName = "VR_DodAI";
    public string ovrCameraRigName = "OVRCameraRig"; // VR �� �� ������Ʈ �̸�

    // VR ȭ���� RawImage�� ��� ������ �� ȣ��
    public void ActivateVRCameraRig()
    {
        Scene vrScene = SceneManager.GetSceneByName(vrSceneName);
        if (!vrScene.isLoaded)
        {
            Debug.LogError("VR ���� ���� �ε���� �ʾҽ��ϴ�.");
            return;
        }

        foreach (var rootObj in vrScene.GetRootGameObjects())
        {
            var rig = rootObj.transform.Find(ovrCameraRigName);
            if (rig != null)
            {
                rig.gameObject.SetActive(true);
                Debug.Log("VR OVRCameraRig Ȱ��ȭ!");
                return;
            }
        }
        Debug.LogWarning("VR ������ OVRCameraRig�� ã�� ���߽��ϴ�.");
    }

    // �ʿ�� ��Ȱ��ȭ�� ����
    public void DeactivateVRCameraRig()
    {
        Scene vrScene = SceneManager.GetSceneByName(vrSceneName);
        if (!vrScene.isLoaded) return;

        foreach (var rootObj in vrScene.GetRootGameObjects())
        {
            var rig = rootObj.transform.Find(ovrCameraRigName);
            if (rig != null)
            {
                rig.gameObject.SetActive(false);
                Debug.Log("VR OVRCameraRig ��Ȱ��ȭ!");
                return;
            }
        }
    }
}
