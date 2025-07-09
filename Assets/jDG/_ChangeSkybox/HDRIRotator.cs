using UnityEngine;

public class HDRIRotator : MonoBehaviour
{
    [Header("ȸ�� ����")]
    [SerializeField] private float rotationSpeed = 5f; // ȸ�� �ӵ�
    [SerializeField] private bool autoRotate = true; // �ڵ� ȸ�� Ȱ��ȭ
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // ȸ�� ��

    private Material skyboxMaterial;
    private float currentRotation = 0f;

    void Start()
    {
        InitializeSkybox();
    }

    void Update()
    {
        if (skyboxMaterial == null) return;

        // �ڵ� ȸ��
        if (autoRotate)
        {
            AutoRotate();
        }

        // ��ī�̹ڽ� ȸ�� ����
        ApplyRotation();
    }

    private void InitializeSkybox()
    {
        skyboxMaterial = RenderSettings.skybox;
        if (skyboxMaterial == null)
        {
            Debug.LogError("��ī�̹ڽ� ��Ƽ������ �������� �ʾҽ��ϴ�!");
            Debug.LogError("Window > Rendering > Lighting Settings���� Skybox Material�� �����ϼ���.");
            return;
        }

        Debug.Log($"��ī�̹ڽ� ��Ƽ����: {skyboxMaterial.name}");
        Debug.Log($"���̴�: {skyboxMaterial.shader.name}");

        // �ʱ� ȸ���� ����
        SetInitialRotation();
    }

    private void SetInitialRotation()
    {
        string[] rotationProperties = { "_Rotation", "_RotationY", "_Rotate", "_SkyRotation" };
        foreach (string prop in rotationProperties)
        {
            if (skyboxMaterial.HasProperty(prop))
            {
                currentRotation = skyboxMaterial.GetFloat(prop);
                Debug.Log($"ȸ�� ������Ƽ �߰�: {prop}");
                break;
            }
        }
    }

    private void AutoRotate()
    {
        currentRotation += rotationSpeed * Time.deltaTime;

        // 360���� ������ 0���� ����
        if (currentRotation >= 360f)
        {
            currentRotation -= 360f;
        }
    }

    private void ApplyRotation()
    {
        // ȸ������ 0-360 ������ ����ȭ
        currentRotation = currentRotation % 360f;
        if (currentRotation < 0) currentRotation += 360f;

        // �پ��� ȸ�� ������Ƽ �õ�
        string[] rotationProperties = { "_Rotation", "_RotationY", "_Rotate", "_SkyRotation" };
        bool applied = false;

        foreach (string prop in rotationProperties)
        {
            if (skyboxMaterial.HasProperty(prop))
            {
                skyboxMaterial.SetFloat(prop, currentRotation);
                applied = true;
                break;
            }
        }

        if (!applied)
        {
            Debug.LogWarning("ȸ�� ������Ƽ�� ã�� �� ���� ȸ���� ������� �ʽ��ϴ�.");
        }
    }

    // ��ī�̹ڽ� ��Ƽ���� ���ΰ�ħ - SkyboxChanger���� ȣ��
    public void RefreshSkyboxMaterial()
    {
        Material newSkyboxMaterial = RenderSettings.skybox;

        if (newSkyboxMaterial != skyboxMaterial)
        {
            skyboxMaterial = newSkyboxMaterial;
            Debug.Log($"���ο� ��ī�̹ڽ� ��Ƽ����� ������Ʈ: {skyboxMaterial.name}");

            // ���ο� ��Ƽ������ �ʱ� ȸ���� ����
            SetInitialRotation();
        }
    }

    // �ܺ� ���� �Լ���
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }

    public void SetRotation(float rotation)
    {
        currentRotation = rotation;
    }

    public void ToggleAutoRotation()
    {
        autoRotate = !autoRotate;
        Debug.Log($"�ڵ� ȸ��: {(autoRotate ? "Ȱ��ȭ" : "��Ȱ��ȭ")}");
    }

    public void ResetRotation()
    {
        currentRotation = 0f;
        Debug.Log("HDRI ȸ�� ����");
    }

    public float GetCurrentRotation()
    {
        return currentRotation;
    }
}