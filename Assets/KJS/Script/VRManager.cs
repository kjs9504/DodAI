using UnityEngine;
using UnityEngine.SceneManagement;

public class VRManager : MonoBehaviour
{
    public string vrSceneName = "VR_DodAI";
    public string ovrCameraRigName = "OVRCameraRig"; // VR 씬 내 오브젝트 이름

    // VR 화면을 RawImage에 출력 시작할 때 호출
    public void ActivateVRCameraRig()
    {
        Scene vrScene = SceneManager.GetSceneByName(vrSceneName);
        if (!vrScene.isLoaded)
        {
            Debug.LogError("VR 씬이 아직 로드되지 않았습니다.");
            return;
        }

        foreach (var rootObj in vrScene.GetRootGameObjects())
        {
            var rig = rootObj.transform.Find(ovrCameraRigName);
            if (rig != null)
            {
                rig.gameObject.SetActive(true);
                Debug.Log("VR OVRCameraRig 활성화!");
                return;
            }
        }
        Debug.LogWarning("VR 씬에서 OVRCameraRig를 찾지 못했습니다.");
    }

    // 필요시 비활성화도 가능
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
                Debug.Log("VR OVRCameraRig 비활성화!");
                return;
            }
        }
    }
}
