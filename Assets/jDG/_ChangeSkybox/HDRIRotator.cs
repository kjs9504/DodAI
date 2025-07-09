using UnityEngine;

public class HDRIRotator : MonoBehaviour
{
    [Header("회전 설정")]
    [SerializeField] private float rotationSpeed = 5f; // 회전 속도
    [SerializeField] private bool autoRotate = true; // 자동 회전 활성화
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // 회전 축

    private Material skyboxMaterial;
    private float currentRotation = 0f;

    void Start()
    {
        InitializeSkybox();
    }

    void Update()
    {
        if (skyboxMaterial == null) return;

        // 자동 회전
        if (autoRotate)
        {
            AutoRotate();
        }

        // 스카이박스 회전 적용
        ApplyRotation();
    }

    private void InitializeSkybox()
    {
        skyboxMaterial = RenderSettings.skybox;
        if (skyboxMaterial == null)
        {
            Debug.LogError("스카이박스 머티리얼이 설정되지 않았습니다!");
            Debug.LogError("Window > Rendering > Lighting Settings에서 Skybox Material을 설정하세요.");
            return;
        }

        Debug.Log($"스카이박스 머티리얼: {skyboxMaterial.name}");
        Debug.Log($"셰이더: {skyboxMaterial.shader.name}");

        // 초기 회전값 설정
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
                Debug.Log($"회전 프로퍼티 발견: {prop}");
                break;
            }
        }
    }

    private void AutoRotate()
    {
        currentRotation += rotationSpeed * Time.deltaTime;

        // 360도를 넘으면 0도로 리셋
        if (currentRotation >= 360f)
        {
            currentRotation -= 360f;
        }
    }

    private void ApplyRotation()
    {
        // 회전값을 0-360 범위로 정규화
        currentRotation = currentRotation % 360f;
        if (currentRotation < 0) currentRotation += 360f;

        // 다양한 회전 프로퍼티 시도
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
            Debug.LogWarning("회전 프로퍼티를 찾을 수 없어 회전이 적용되지 않습니다.");
        }
    }

    // 스카이박스 머티리얼 새로고침 - SkyboxChanger에서 호출
    public void RefreshSkyboxMaterial()
    {
        Material newSkyboxMaterial = RenderSettings.skybox;

        if (newSkyboxMaterial != skyboxMaterial)
        {
            skyboxMaterial = newSkyboxMaterial;
            Debug.Log($"새로운 스카이박스 머티리얼로 업데이트: {skyboxMaterial.name}");

            // 새로운 머티리얼의 초기 회전값 설정
            SetInitialRotation();
        }
    }

    // 외부 제어 함수들
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
        Debug.Log($"자동 회전: {(autoRotate ? "활성화" : "비활성화")}");
    }

    public void ResetRotation()
    {
        currentRotation = 0f;
        Debug.Log("HDRI 회전 리셋");
    }

    public float GetCurrentRotation()
    {
        return currentRotation;
    }
}