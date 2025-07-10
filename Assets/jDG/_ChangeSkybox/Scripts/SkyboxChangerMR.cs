using UnityEngine;
using UnityEngine.Events;

public class SkyboxChangerMR : MonoBehaviour
{
    [Header("스카이박스 설정")]
    public Material[] skyboxMaterials; // 스카이박스 머티리얼 배열

    private int currentSkyboxIndex = 0; // 현재 스카이박스 인덱스
    private HDRIRotator hdriRotator; // HDRI 회전자 참조

    void Start()
    {
        // HDRI 회전자 찾기
        hdriRotator = FindObjectOfType<HDRIRotator>();

        // 초기 스카이박스 설정
        if (skyboxMaterials.Length > 0)
        {
            RenderSettings.skybox = skyboxMaterials[0];
        }

        // 씬의 모든 SkyboxButtonMR 찾아서 이벤트 연결
        SkyboxButtonMR[] buttons = FindObjectsOfType<SkyboxButtonMR>();
        foreach (SkyboxButtonMR button in buttons)
        {
            button.Initialize(this);
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

    // 햅틱 피드백 (선택사항)
    public void TriggerHapticFeedback()
    {
        // 컨트롤러 진동 (OVRInput 사용)
        OVRInput.SetControllerVibration(0.2f, 0.2f, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0.2f, 0.2f, OVRInput.Controller.LTouch);

        // 잠시 후 진동 멈추기
        Invoke("StopHapticFeedback", 0.1f);
    }

    private void StopHapticFeedback()
    {
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
    }
}

// MR 환경용 개별 버튼 컴포넌트
public class SkyboxButtonMR : MonoBehaviour
{
    [Header("버튼 설정")]
    public int skyboxIndex = -1; // -1이면 다음 스카이박스로, 0 이상이면 특정 스카이박스로
    public string buttonName = "MR Skybox Button"; // 버튼 이름

    [Header("시각적 피드백")]
    public bool useScaleEffect = true; // 스케일 효과 사용
    public float scaleMultiplier = 1.1f; // 스케일 배수
    public float scaleSpeed = 5f; // 스케일 애니메이션 속도

    [Header("클릭 이벤트")]
    public UnityEvent OnButtonClick; // 버튼 클릭 이벤트

    private SkyboxChangerMR skyboxChanger;
    private Vector3 originalScale;
    private bool isHovered = false;

    void Start()
    {
        originalScale = transform.localScale;

        // 콜라이더 확인
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
            Debug.Log($"{buttonName}에 BoxCollider가 자동으로 추가되었습니다.");
        }

        // 이벤트 연결
        OnButtonClick.AddListener(OnButtonClicked);
    }

    void Update()
    {
        // 스케일 애니메이션
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

    // 외부에서 호버 상태 설정 (Oculus Interaction 이벤트에서 호출)
    public void SetHoverState(bool hovered)
    {
        isHovered = hovered;
    }

    // 버튼 클릭 처리 (Oculus Interaction 이벤트에서 호출)
    public void OnButtonClicked()
    {
        if (skyboxChanger == null) return;

        Debug.Log($"MR 버튼 클릭: {buttonName}");

        // 햅틱 피드백
        skyboxChanger.TriggerHapticFeedback();

        // 스카이박스 변경
        if (skyboxIndex >= 0)
        {
            skyboxChanger.SetSkybox(skyboxIndex);
        }
        else
        {
            skyboxChanger.ChangeSkybox();
        }
    }

    // 간단한 버튼 클릭 (직접 호출용)
    public void ClickButton()
    {
        OnButtonClick.Invoke();
    }
}