using UnityEngine;

public class VRSceneTransitionTrigger : MonoBehaviour
{
    public Animator animator;

    public void TriggerTransition()
    {
        Debug.Log("🔘 [VRSceneTransitionTrigger] TriggerTransition() 호출됨");

        if (animator != null)
        {
            Debug.Log("✅ Animator 있음 → SetTrigger(\"Start\") 실행");
            animator.SetTrigger("Start");
        }
        else
        {
            Debug.LogWarning("⚠️ Animator가 비어 있음 (null)");
        }
    }
}
