using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    void Start()
    {
        // "VirtualScene" ���� Additive�� �ҷ�����
        SceneManager.LoadScene("VR_DodAI", LoadSceneMode.Additive);
    }
}
