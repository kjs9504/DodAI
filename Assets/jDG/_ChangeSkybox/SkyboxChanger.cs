using UnityEngine;
using UnityEngine.UI;

public class SkyboxChanger : MonoBehaviour
{
    [Header("��ī�̹ڽ� ����")]
    public Material[] skyboxMaterials; // ��ī�̹ڽ� ��Ƽ���� �迭
    public Button changeButton; // ��ư ����

    private int currentSkyboxIndex = 0; // ���� ��ī�̹ڽ� �ε���
    private HDRIRotator hdriRotator; // HDRI ȸ���� ����

    void Start()
    {
        // HDRI ȸ���� ã��
        hdriRotator = FindObjectOfType<HDRIRotator>();

        // ��ư Ŭ�� �̺�Ʈ ����
        if (changeButton != null)
        {
            changeButton.onClick.AddListener(ChangeSkybox);
        }

        // �ʱ� ��ī�̹ڽ� ����
        if (skyboxMaterials.Length > 0)
        {
            RenderSettings.skybox = skyboxMaterials[0];
        }
    }

    // ��ī�̹ڽ� ���� �Լ�
    public void ChangeSkybox()
    {
        if (skyboxMaterials.Length == 0) return;

        // ���� ��ī�̹ڽ��� ����
        currentSkyboxIndex = (currentSkyboxIndex + 1) % skyboxMaterials.Length;
        RenderSettings.skybox = skyboxMaterials[currentSkyboxIndex];

        // ������� ��� ����
        DynamicGI.UpdateEnvironment();

        // HDRIRotator���� ���ο� ��ī�̹ڽ� ��Ƽ���� �˸�
        if (hdriRotator != null)
        {
            hdriRotator.RefreshSkyboxMaterial();
        }

        Debug.Log($"��ī�̹ڽ��� ����Ǿ����ϴ�: {skyboxMaterials[currentSkyboxIndex].name}");
    }

    // Ư�� �ε����� ��ī�̹ڽ��� ����
    public void SetSkybox(int index)
    {
        if (index < 0 || index >= skyboxMaterials.Length) return;

        currentSkyboxIndex = index;
        RenderSettings.skybox = skyboxMaterials[index];
        DynamicGI.UpdateEnvironment();

        // HDRIRotator���� ���ο� ��ī�̹ڽ� ��Ƽ���� �˸�
        if (hdriRotator != null)
        {
            hdriRotator.RefreshSkyboxMaterial();
        }

        Debug.Log($"��ī�̹ڽ��� {index}������ ����Ǿ����ϴ�: {skyboxMaterials[index].name}");
    }
}