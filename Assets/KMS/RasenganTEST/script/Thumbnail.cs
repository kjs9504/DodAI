using UnityEngine;
using UnityEngine.SceneManagement;

public class ThumbnailLoader : MonoBehaviour
{
    void Start()
    {
        // ��1�� Additive�� �ε�
        SceneManager.LoadSceneAsync("Thumbnail", LoadSceneMode.Additive);
    }
}