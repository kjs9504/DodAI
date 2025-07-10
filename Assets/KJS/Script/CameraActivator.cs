using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraActivator : MonoBehaviour
{
    [Header("이 스크립트로 활성화할 GameObject")]
    [SerializeField] private GameObject objectToActivate;

    private void Awake()
    {
        if (objectToActivate == null)
            Debug.LogWarning($"[{name}] objectToActivate 가 할당되지 않았습니다.");
    }

    private void OnEnable()
    {
        // 씬이 로드될 때마다 콜백 호출
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬이 새로 로드되면 호출됩니다.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (objectToActivate != null)
            objectToActivate.SetActive(true);
    }
}