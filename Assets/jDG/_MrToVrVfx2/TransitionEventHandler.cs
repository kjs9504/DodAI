using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionEventHandler : MonoBehaviour
{
    public string nextSceneName = "VR_DodAI"; // 이동할 씬 이름

    public void OnTransitionComplete()
    {
        Debug.Log("✅ 애니메이션 끝! 씬 전환 중...");
        SceneManager.LoadScene(nextSceneName);
    }
}