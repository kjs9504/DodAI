// 스크립트: UIButtonHandler.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIButtonHandler : MonoBehaviour
{
    public void LoadVirtualScene()
    {
        Debug.Log("버튼 클릭됨! 씬 전환 시도");
        SceneManager.LoadScene("VirtualScene"); // 씬 이름 정확히 입력
    }
}
