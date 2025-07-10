using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraActivator : MonoBehaviour
{
    [Header("�� ��ũ��Ʈ�� Ȱ��ȭ�� GameObject")]
    [SerializeField] private GameObject objectToActivate;

    private void Awake()
    {
        if (objectToActivate == null)
            Debug.LogWarning($"[{name}] objectToActivate �� �Ҵ���� �ʾҽ��ϴ�.");
    }

    private void OnEnable()
    {
        // ���� �ε�� ������ �ݹ� ȣ��
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ���� ���� �ε�Ǹ� ȣ��˴ϴ�.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (objectToActivate != null)
            objectToActivate.SetActive(true);
    }
}