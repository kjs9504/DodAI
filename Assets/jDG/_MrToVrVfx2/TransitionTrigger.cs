// ✅ 1. TransitionTrigger.cs - 클릭 시 UI 확대 애니메이션 재생 후 씬 전환
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionTrigger : MonoBehaviour
{
    public Animator animator;          // UI 확대 애니메이터
    public string nextSceneName;       // 이동할 씬 이름

    public void StartSceneTransition()
    {
        Debug.Log("[TransitionTrigger] 트리거 실행됨");
        animator.SetTrigger("Start");  // Animator에서 "Start" 트리거 활성화
    }

    // 애니메이션 이벤트에서 호출될 함수
    public void OnTransitionComplete()
    {
        Debug.Log("[TransitionTrigger] 애니메이션 완료 → 씬 전환");
        SceneManager.LoadScene(nextSceneName);
    }
}
