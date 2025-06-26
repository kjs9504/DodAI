using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    void Start()
    {
        // "VirtualScene" 씬을 Additive로 불러오기
        SceneManager.LoadScene("VirtualScene", LoadSceneMode.Additive);
    }
}
