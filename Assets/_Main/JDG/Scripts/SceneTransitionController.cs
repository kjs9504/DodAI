using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionController : MonoBehaviour
{
    public Animator animator;
    public string sceneName;

    public void StartTransition()
    {
        Debug.Log("▶ [SceneTransitionController] StartTransition() 호출됨");
        animator.SetTrigger("Start");
    }

    public void OnAnimationComplete()
    {
        Debug.Log("🚪 [SceneTransitionController] OnAnimationComplete() - 씬 전환: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}
