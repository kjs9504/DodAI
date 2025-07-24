using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Meta XR 환경에서 UI 버튼을 통해 비활성화된 3D 오브젝트를 활성화하고 오디오 재생
/// </summary>
public class XRObjectActivator : MonoBehaviour
{
    [Header("활성화할 3D 오브젝트들")]
    public GameObject[] objects3D;

    [Header("오브젝트별 사운드")]
    public AudioClip[] audioClips;

    [Header("UI 버튼들")]
    public Button[] activateButtons;

    [Header("오디오")]
    public AudioSource audioSource;

    [Header("효과 설정")]
    public bool useScaleAnimation = true; // 스케일 애니메이션 사용 여부
    public float animationDuration = 0.3f; // 애니메이션 지속 시간

    [Header("둥둥 떠있는 애니메이션")]
    public bool useFloatingAnimation = true; // 떠있는 애니메이션 사용 여부
    public float floatingRange = 0.1f; // 움직임 범위 (전체적인 크기)
    public float floatingSpeed = 0.8f; // 떠있는 속도
    public float rotationRange = 3f; // 회전 범위 (도 단위)

    [Header("UI 숨김 설정")]
    public bool hideUIOnClick = true; // 버튼 클릭 시 UI 숨김 여부
    public GameObject uiPanel; // 숨길 UI 패널 (Canvas 또는 Panel)
    public float uiHideDuration = 0.3f; // UI 숨김 애니메이션 시간
    public bool oneTimeUIHide = true; // 일회용 UI 숨김 (다시 나타나지 않음)

    // 현재 활성화된 오브젝트들과 코루틴을 추적
    private Dictionary<GameObject, Coroutine> activeCoroutines = new Dictionary<GameObject, Coroutine>();

    // 떠있는 애니메이션을 위한 오브젝트별 원래 위치와 회전 저장
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> originalRotations = new Dictionary<GameObject, Quaternion>();
    private Dictionary<GameObject, Coroutine> floatingCoroutines = new Dictionary<GameObject, Coroutine>();

    // UI 상태 추적
    private bool isUIVisible = true;
    private Vector3 originalUIScale;
    private CanvasGroup uiCanvasGroup;

    void Start()
    {
        // AudioSource가 없으면 생성
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // 모든 3D 오브젝트를 비활성화하고 원래 위치와 회전 저장
        foreach (GameObject obj in objects3D)
        {
            if (obj != null)
            {
                originalPositions[obj] = obj.transform.position;
                originalRotations[obj] = obj.transform.rotation;
                obj.SetActive(false);
            }
        }

        // UI 패널 설정
        SetupUIPanel();

        // 버튼 이벤트 연결
        SetupButtonEvents();
    }

    /// <summary>
    /// UI 패널을 설정합니다
    /// </summary>
    void SetupUIPanel()
    {
        // UI 패널이 설정되지 않았다면 첫 번째 버튼의 부모를 찾아서 설정
        if (uiPanel == null && activateButtons.Length > 0 && activateButtons[0] != null)
        {
            // 버튼의 부모 Canvas나 Panel을 찾아서 자동 설정
            Transform parent = activateButtons[0].transform.parent;
            while (parent != null)
            {
                if (parent.GetComponent<Canvas>() != null || parent.name.ToLower().Contains("panel"))
                {
                    uiPanel = parent.gameObject;
                    break;
                }
                parent = parent.parent;
            }
        }
    }

    /// <summary>
    /// 버튼 이벤트를 설정합니다
    /// </summary>
    void SetupButtonEvents()
    {
        for (int i = 0; i < activateButtons.Length && i < objects3D.Length; i++)
        {
            int index = i; // 클로저 문제 해결
            activateButtons[i].onClick.AddListener(() => ActivateObject(index));
        }
    }

    /// <summary>
    /// 지정된 인덱스의 오브젝트를 활성화합니다
    /// </summary>
    public void ActivateObject(int objectIndex)
    {
        if (objectIndex < 0 || objectIndex >= objects3D.Length || objects3D[objectIndex] == null)
        {
            Debug.LogWarning($"잘못된 오브젝트 인덱스: {objectIndex}");
            return;
        }

        GameObject targetObject = objects3D[objectIndex];

        // UI 숨김 처리
        if (hideUIOnClick && isUIVisible)
        {
            StartCoroutine(HideUI());
        }

        // 이미 활성화 중인 오브젝트라면 기존 코루틴 중지
        if (activeCoroutines.ContainsKey(targetObject))
        {
            StopCoroutine(activeCoroutines[targetObject]);
            activeCoroutines.Remove(targetObject);
        }

        // 떠있는 애니메이션 코루틴도 중지
        if (floatingCoroutines.ContainsKey(targetObject))
        {
            StopCoroutine(floatingCoroutines[targetObject]);
            floatingCoroutines.Remove(targetObject);
        }

        // 오브젝트 활성화 및 오디오 재생
        Coroutine newCoroutine = StartCoroutine(ActivateObjectCoroutine(targetObject, objectIndex));
        activeCoroutines[targetObject] = newCoroutine;

        Debug.Log($"오브젝트 활성화됨: {targetObject.name}");
    }

    /// <summary>
    /// 오브젝트 활성화 및 비활성화 코루틴
    /// </summary>
    IEnumerator ActivateObjectCoroutine(GameObject obj, int audioIndex)
    {
        // 오브젝트 위치와 회전을 원래 상태로 복원
        obj.transform.position = originalPositions[obj];
        obj.transform.rotation = originalRotations[obj];

        // 오브젝트 활성화
        obj.SetActive(true);

        // 스케일 애니메이션 시작
        if (useScaleAnimation)
        {
            yield return StartCoroutine(ScaleInAnimation(obj));
        }

        // 떠있는 애니메이션 시작
        if (useFloatingAnimation)
        {
            Coroutine floatingCoroutine = StartCoroutine(FloatingAnimation(obj));
            floatingCoroutines[obj] = floatingCoroutine;
        }

        // 오디오 재생
        AudioClip clipToPlay = null;
        if (audioIndex < audioClips.Length && audioClips[audioIndex] != null)
        {
            clipToPlay = audioClips[audioIndex];
            audioSource.PlayOneShot(clipToPlay);
        }

        // 오디오가 끝날 때까지 대기
        if (clipToPlay != null)
        {
            yield return new WaitForSeconds(clipToPlay.length);
        }
        else
        {
            // 오디오가 없으면 기본 2초 대기
            yield return new WaitForSeconds(2f);
        }

        // 떠있는 애니메이션 중지
        if (floatingCoroutines.ContainsKey(obj))
        {
            StopCoroutine(floatingCoroutines[obj]);
            floatingCoroutines.Remove(obj);
        }

        // 스케일 아웃 애니메이션
        if (useScaleAnimation)
        {
            yield return StartCoroutine(ScaleOutAnimation(obj));
        }

        // 오브젝트 비활성화
        obj.SetActive(false);

        // 코루틴 딕셔너리에서 제거
        if (activeCoroutines.ContainsKey(obj))
        {
            activeCoroutines.Remove(obj);
        }

        Debug.Log($"오브젝트 비활성화됨: {obj.name}");

        // 일회용이 아닌 경우에만 UI 다시 표시
        if (activeCoroutines.Count == 0 && !isUIVisible && !oneTimeUIHide)
        {
            StartCoroutine(ShowUI());
        }
    }

    /// <summary>
    /// 자연스럽게 둥둥 떠있는 애니메이션 (상하좌우 + 미세한 회전)
    /// </summary>
    IEnumerator FloatingAnimation(GameObject obj)
    {
        Vector3 originalPos = originalPositions[obj];
        Quaternion originalRot = originalRotations[obj];

        float timeX = Random.Range(0f, Mathf.PI * 2f); // X축 시작 위상 랜덤
        float timeY = Random.Range(0f, Mathf.PI * 2f); // Y축 시작 위상 랜덤
        float timeZ = Random.Range(0f, Mathf.PI * 2f); // Z축 시작 위상 랜덤
        float timeRotX = Random.Range(0f, Mathf.PI * 2f); // 회전 X축 시작 위상 랜덤
        float timeRotZ = Random.Range(0f, Mathf.PI * 2f); // 회전 Z축 시작 위상 랜덤

        while (obj.activeInHierarchy)
        {
            timeX += Time.deltaTime * floatingSpeed;
            timeY += Time.deltaTime * floatingSpeed * 0.7f; // 약간 다른 속도로
            timeZ += Time.deltaTime * floatingSpeed * 1.3f; // 약간 다른 속도로
            timeRotX += Time.deltaTime * floatingSpeed * 0.5f;
            timeRotZ += Time.deltaTime * floatingSpeed * 0.6f;

            // 3D 공간에서 자연스러운 움직임 (8자 형태 + 상하 움직임)
            float xOffset = Mathf.Sin(timeX) * floatingRange * 0.3f; // 좌우 움직임 (작게)
            float yOffset = Mathf.Sin(timeY) * floatingRange; // 상하 움직임
            float zOffset = Mathf.Cos(timeZ) * floatingRange * 0.2f; // 앞뒤 움직임 (더 작게)

            Vector3 newPosition = originalPos + new Vector3(xOffset, yOffset, zOffset);
            obj.transform.position = newPosition;

            // 미세한 회전 움직임 (자연스러운 느낌)
            float rotX = Mathf.Sin(timeRotX) * rotationRange;
            float rotZ = Mathf.Cos(timeRotZ) * rotationRange;

            Quaternion floatingRotation = Quaternion.Euler(rotX, 0, rotZ);
            obj.transform.rotation = originalRot * floatingRotation;

            yield return null;
        }

        // 애니메이션 종료 시 원래 위치와 회전으로 복원
        obj.transform.position = originalPos;
        obj.transform.rotation = originalRot;
    }

    /// <summary>
    /// 스케일 인 애니메이션
    /// </summary>
    IEnumerator ScaleInAnimation(GameObject obj)
    {
        Vector3 originalScale = obj.transform.localScale;
        obj.transform.localScale = Vector3.zero;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;

            // EaseOutBack 효과를 위한 계산
            float easeProgress = EaseOutBack(progress);
            obj.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, easeProgress);

            yield return null;
        }

        obj.transform.localScale = originalScale;
    }

    /// <summary>
    /// 스케일 아웃 애니메이션
    /// </summary>
    IEnumerator ScaleOutAnimation(GameObject obj)
    {
        Vector3 originalScale = obj.transform.localScale;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;

            // EaseInBack 효과
            float easeProgress = EaseInBack(progress);
            obj.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, easeProgress);

            yield return null;
        }

        obj.transform.localScale = Vector3.zero;
    }

    /// <summary>
    /// EaseOutBack 이징 함수
    /// </summary>
    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1;

        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }

    /// <summary>
    /// EaseInBack 이징 함수
    /// </summary>
    float EaseInBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1;

        return c3 * t * t * t - c1 * t * t;
    }

    /// <summary>
    /// UI를 숨깁니다
    /// </summary>
    IEnumerator HideUI()
    {
        if (uiPanel == null || !isUIVisible) yield break;

        isUIVisible = false;

        float elapsed = 0f;
        Vector3 startScale = uiPanel.transform.localScale;
        float startAlpha = uiCanvasGroup != null ? uiCanvasGroup.alpha : 1f;

        while (elapsed < uiHideDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / uiHideDuration;

            // 스케일 애니메이션 (축소)
            uiPanel.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, EaseInBack(progress));

            // 페이드 아웃
            if (uiCanvasGroup != null)
            {
                uiCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
            }

            yield return null;
        }

        // 최종 상태 설정
        uiPanel.transform.localScale = Vector3.zero;
        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.alpha = 0f;
            uiCanvasGroup.interactable = false; // 상호작용 비활성화
        }

        Debug.Log("UI 패널 숨김 완료");
    }

    /// <summary>
    /// UI를 다시 표시합니다
    /// </summary>
    IEnumerator ShowUI()
    {
        if (uiPanel == null || isUIVisible) yield break;

        isUIVisible = true;

        // 상호작용 다시 활성화
        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.interactable = true;
        }

        float elapsed = 0f;
        Vector3 startScale = uiPanel.transform.localScale;
        float startAlpha = uiCanvasGroup != null ? uiCanvasGroup.alpha : 0f;

        while (elapsed < uiHideDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / uiHideDuration;

            // 스케일 애니메이션 (확대)
            uiPanel.transform.localScale = Vector3.Lerp(startScale, originalUIScale, EaseOutBack(progress));

            // 페이드 인
            if (uiCanvasGroup != null)
            {
                uiCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, progress);
            }

            yield return null;
        }

        // 최종 상태 설정
        uiPanel.transform.localScale = originalUIScale;
        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.alpha = 1f;
        }

        Debug.Log("UI 패널 표시 완료");
    }

    /// <summary>
    /// UI를 수동으로 토글합니다
    /// </summary>
    public void ToggleUI()
    {
        if (isUIVisible)
        {
            StartCoroutine(HideUI());
        }
        else if (!oneTimeUIHide) // 일회용이 아닌 경우에만 다시 표시 가능
        {
            StartCoroutine(ShowUI());
        }
    }

    /// <summary>
    /// UI를 강제로 다시 표시합니다 (일회용 설정 무시)
    /// </summary>
    public void ForceShowUI()
    {
        if (!isUIVisible)
        {
            StartCoroutine(ShowUI());
        }
    }

    /// <summary>
    /// 일회용 설정을 리셋합니다
    /// </summary>
    public void ResetOneTimeUIHide()
    {
        oneTimeUIHide = false;
        Debug.Log("일회용 UI 숨김 설정이 해제되었습니다. 이제 UI가 다시 나타날 수 있습니다.");
    }

    /// <summary>
    /// 모든 활성화된 오브젝트를 즉시 비활성화합니다
    /// </summary>
    public void DeactivateAllObjects()
    {
        // 모든 코루틴 중지
        foreach (var kvp in activeCoroutines)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
            if (kvp.Key != null)
            {
                kvp.Key.SetActive(false);
            }
        }

        // 모든 떠있는 애니메이션 코루틴 중지
        foreach (var kvp in floatingCoroutines)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }

        activeCoroutines.Clear();
        floatingCoroutines.Clear();

        // 오디오 중지
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // 일회용이 아닌 경우에만 UI 다시 표시
        if (!isUIVisible && !oneTimeUIHide)
        {
            StartCoroutine(ShowUI());
        }
    }

    /// <summary>
    /// 특정 오브젝트 활성화 (외부에서 호출용)
    /// </summary>
    public void ActivateObject1() => ActivateObject(0);
    public void ActivateObject2() => ActivateObject(1);
    public void ActivateObject3() => ActivateObject(2);
    public void ActivateObject4() => ActivateObject(3);

    void OnDestroy()
    {
        // 모든 코루틴 중지
        StopAllCoroutines();

        // 버튼 이벤트 해제
        foreach (Button button in activateButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }
    }
}