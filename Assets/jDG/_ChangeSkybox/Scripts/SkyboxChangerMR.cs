using UnityEngine;
using UnityEngine.Events;

public class SkyboxChangerMR : MonoBehaviour
{
    [Header("��ī�̹ڽ� ����")]
    public Material[] skyboxMaterials; // ��ī�̹ڽ� ��Ƽ���� �迭

    private int currentSkyboxIndex = 0; // ���� ��ī�̹ڽ� �ε���
    private HDRIRotator hdriRotator; // HDRI ȸ���� ����

    void Start()
    {
        // HDRI ȸ���� ã��
        hdriRotator = FindObjectOfType<HDRIRotator>();

        // �ʱ� ��ī�̹ڽ� ����
        if (skyboxMaterials.Length > 0)
        {
            RenderSettings.skybox = skyboxMaterials[0];
        }

        // ���� ��� SkyboxButtonMR ã�Ƽ� �̺�Ʈ ����
        SkyboxButtonMR[] buttons = FindObjectsOfType<SkyboxButtonMR>();
        foreach (SkyboxButtonMR button in buttons)
        {
            button.Initialize(this);
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

    // ��ƽ �ǵ�� (���û���)
    public void TriggerHapticFeedback()
    {
        // ��Ʈ�ѷ� ���� (OVRInput ���)
        OVRInput.SetControllerVibration(0.2f, 0.2f, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0.2f, 0.2f, OVRInput.Controller.LTouch);

        // ��� �� ���� ���߱�
        Invoke("StopHapticFeedback", 0.1f);
    }

    private void StopHapticFeedback()
    {
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
    }
}

// MR ȯ��� ���� ��ư ������Ʈ
public class SkyboxButtonMR : MonoBehaviour
{
    [Header("��ư ����")]
    public int skyboxIndex = -1; // -1�̸� ���� ��ī�̹ڽ���, 0 �̻��̸� Ư�� ��ī�̹ڽ���
    public string buttonName = "MR Skybox Button"; // ��ư �̸�

    [Header("�ð��� �ǵ��")]
    public bool useScaleEffect = true; // ������ ȿ�� ���
    public float scaleMultiplier = 1.1f; // ������ ���
    public float scaleSpeed = 5f; // ������ �ִϸ��̼� �ӵ�

    [Header("Ŭ�� �̺�Ʈ")]
    public UnityEvent OnButtonClick; // ��ư Ŭ�� �̺�Ʈ

    private SkyboxChangerMR skyboxChanger;
    private Vector3 originalScale;
    private bool isHovered = false;

    void Start()
    {
        originalScale = transform.localScale;

        // �ݶ��̴� Ȯ��
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
            Debug.Log($"{buttonName}�� BoxCollider�� �ڵ����� �߰��Ǿ����ϴ�.");
        }

        // �̺�Ʈ ����
        OnButtonClick.AddListener(OnButtonClicked);
    }

    void Update()
    {
        // ������ �ִϸ��̼�
        if (useScaleEffect)
        {
            Vector3 targetScale = isHovered ? originalScale * scaleMultiplier : originalScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
        }
    }

    public void Initialize(SkyboxChangerMR changer)
    {
        skyboxChanger = changer;
    }

    // �ܺο��� ȣ�� ���� ���� (Oculus Interaction �̺�Ʈ���� ȣ��)
    public void SetHoverState(bool hovered)
    {
        isHovered = hovered;
    }

    // ��ư Ŭ�� ó�� (Oculus Interaction �̺�Ʈ���� ȣ��)
    public void OnButtonClicked()
    {
        if (skyboxChanger == null) return;

        Debug.Log($"MR ��ư Ŭ��: {buttonName}");

        // ��ƽ �ǵ��
        skyboxChanger.TriggerHapticFeedback();

        // ��ī�̹ڽ� ����
        if (skyboxIndex >= 0)
        {
            skyboxChanger.SetSkybox(skyboxIndex);
        }
        else
        {
            skyboxChanger.ChangeSkybox();
        }
    }

    // ������ ��ư Ŭ�� (���� ȣ���)
    public void ClickButton()
    {
        OnButtonClick.Invoke();
    }
}