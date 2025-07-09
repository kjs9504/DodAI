using UnityEngine;
using UnityEngine.UI;

public class SkyboxChanger : MonoBehaviour
{
    [Header("스카이박스 설정")]
    public Material[] skyboxMaterials; // 스카이박스 머티리얼 배열
    public Button changeButton; // 버튼 참조

    private int currentSkyboxIndex = 0; // 현재 스카이박스 인덱스
    private HDRIRotator hdriRotator; // HDRI 회전자 참조

    void Start()
    {
        // HDRI 회전자 찾기
        hdriRotator = FindObjectOfType<HDRIRotator>();

        // 버튼 클릭 이벤트 연결
        if (changeButton != null)
        {
            changeButton.onClick.AddListener(ChangeSkybox);
        }

        // 초기 스카이박스 설정
        if (skyboxMaterials.Length > 0)
        {
            RenderSettings.skybox = skyboxMaterials[0];
        }
    }

    // 스카이박스 변경 함수
    public void ChangeSkybox()
    {
        if (skyboxMaterials.Length == 0) return;

        // 다음 스카이박스로 변경
        currentSkyboxIndex = (currentSkyboxIndex + 1) % skyboxMaterials.Length;
        RenderSettings.skybox = skyboxMaterials[currentSkyboxIndex];

        // 변경사항 즉시 적용
        DynamicGI.UpdateEnvironment();

        // HDRIRotator에게 새로운 스카이박스 머티리얼 알림
        if (hdriRotator != null)
        {
            hdriRotator.RefreshSkyboxMaterial();
        }

        Debug.Log($"스카이박스가 변경되었습니다: {skyboxMaterials[currentSkyboxIndex].name}");
    }

    // 특정 인덱스의 스카이박스로 변경
    public void SetSkybox(int index)
    {
        if (index < 0 || index >= skyboxMaterials.Length) return;

        currentSkyboxIndex = index;
        RenderSettings.skybox = skyboxMaterials[index];
        DynamicGI.UpdateEnvironment();

        // HDRIRotator에게 새로운 스카이박스 머티리얼 알림
        if (hdriRotator != null)
        {
            hdriRotator.RefreshSkyboxMaterial();
        }

        Debug.Log($"스카이박스가 {index}번으로 변경되었습니다: {skyboxMaterials[index].name}");
    }
}