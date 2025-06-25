using UnityEngine;
using UnityEngine.SceneManagement;

public class ThumbnailLoader : MonoBehaviour
{
    void Start()
    {
        // ¾À1À» Additive·Î ·Îµå
        SceneManager.LoadSceneAsync("Thumbnail", LoadSceneMode.Additive);
    }
}