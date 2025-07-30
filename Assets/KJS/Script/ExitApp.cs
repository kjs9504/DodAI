using UnityEngine;

public class ExitApp : MonoBehaviour
{
    // 버튼에서 호출할 함수
    public void ExitApplication()
    {
        // Unity 에디터에서는 종료가 안 되므로 로그만 출력
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
