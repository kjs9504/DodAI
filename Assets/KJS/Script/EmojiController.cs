using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

[Serializable]
public class FruitEmotionData
{
    public long taskId;
    public string emotion; // "Fun", "슬픔", "분노", "허무감", "달성감"
    public string todo;
    public string date;
    public string time;
    public string acceptedAt;
    public long? userId; // 나중에 필요하면 값 할당
    public Vector3 position;
    public string createdAt;
}

public class EmojiController : MonoBehaviour
{
    [System.Serializable]
    public class Item
    {
        [Header("클릭할 UI Image (버튼)")]
        public Image uiButton;          // 클릭 입력 받는 UI

        [Header("이 버튼이 제어할 3D 오브젝트")]
        public GameObject targetObject;  // 이 오브젝트의 MeshFilter/MeshRenderer 제어

        [Header("이 버튼의 눌린 상태 Mesh/Material")]
        public Mesh pressedMesh;
        public Material pressedMaterial;

        // 기본 상태에서 사용되는 데이터
        [HideInInspector] public Mesh normalMesh;
        [HideInInspector] public Material normalMaterial;

        // 컴포넌트 캐시
        [HideInInspector] public MeshFilter mf;
        [HideInInspector] public MeshRenderer mr;

        // 감정 타입 (1번: Fun, 2번: 슬픔, 3번: 분노, 4번: 허무감, 5번: 달성감)
        [HideInInspector] public string emotionType;
    }

    [Tooltip("Public에서 할당한 UI 이미지(5개)에 1개씩 Item을 추가하세요.")]
    public List<Item> items = new List<Item>(5);

    // FruitInfoUI 참조 (감정 선택 시 데이터를 가져오기 위해)
    private FruitInfoUI currentFruitInfoUI;

    void Awake()
    {
        // 감정 타입 설정
        string[] emotions = { "Fun", "슬픔", "분노", "허무감", "달성감" };
        
        foreach (var it in items)
        {
            // 감정 타입 할당
            int index = items.IndexOf(it);
            if (index < emotions.Length)
                it.emotionType = emotions[index];

            // 1) UI Image 할당 체크
            if (it.uiButton == null)
            {
                Debug.LogWarning($"MultiPokeUIToMeshController: uiButton이 할당되지 않았습니다 (Item 인덱스 {items.IndexOf(it)})");
                continue;
            }

            // 2) targetObject가 없으면 Self(버튼 오브젝트) 사용
            if (it.targetObject == null)
                it.targetObject = it.uiButton.gameObject;

            // 3) MeshFilter / MeshRenderer 찾기 (자식 포함)
            if (!it.targetObject.TryGetComponent<MeshFilter>(out it.mf))
                it.mf = it.targetObject.GetComponentInChildren<MeshFilter>();
            if (!it.targetObject.TryGetComponent<MeshRenderer>(out it.mr))
                it.mr = it.targetObject.GetComponentInChildren<MeshRenderer>();

            if (it.mf == null || it.mr == null)
            {
                Debug.LogError($"[{name}] Item[{items.IndexOf(it)}]: '{it.targetObject.name}'에 MeshFilter/MeshRenderer가 없습니다.");
                continue;
            }

            // 4) 기본 상태 저장
            it.normalMesh = it.mf.mesh;
            it.normalMaterial = it.mr.material;

            // 5) UI Image의 RaycastTarget 활성화
            it.uiButton.raycastTarget = true;

            // 6) EventTrigger 추가
            var trig = it.uiButton.gameObject.AddComponent<EventTrigger>();

            // PointerDown에서 OnPressed
            var downEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };
            downEntry.callback.AddListener((data) => OnPressed(it));
            trig.triggers.Add(downEntry);

            // PointerUp에서 OnReleased
            var upEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerUp
            };
            upEntry.callback.AddListener((data) => OnReleased(it));
            trig.triggers.Add(upEntry);
        }
    }

    /// <summary>
    /// 현재 선택된 FruitInfoUI 설정 (감정 선택 시 데이터를 가져오기 위해)
    /// </summary>
    public void SetCurrentFruitInfoUI(FruitInfoUI fruitInfoUI)
    {
        currentFruitInfoUI = fruitInfoUI;
    }

    private void OnPressed(Item it)
    {
        if (it.mf == null || it.mr == null) return;

        // 원본 mesh/material이 null일 때만 저장 (한 번만)
        if (it.normalMesh == null) it.normalMesh = it.mf.mesh;
        if (it.normalMaterial == null) it.normalMaterial = it.mr.material;

        if (it.pressedMesh != null) it.mf.mesh = it.pressedMesh;
        if (it.pressedMaterial != null) it.mr.material = it.pressedMaterial;
    
    }

    private void OnReleased(Item it)
    {
        if (it.mf == null || it.mr == null) return;

        // 아래 두 줄을 주석 처리 또는 삭제!
        // if (it.normalMesh != null) it.mf.mesh = it.normalMesh;
        // if (it.normalMaterial != null) it.mr.material = it.normalMaterial;
        it.targetObject.transform.localScale = Vector3.one;

        // FRUIT JSON 생성
        CreateFruitJSON(it.emotionType);
    }

    /// <summary>
    /// 감정 데이터와 FruitInfoUI 데이터를 합쳐서 FRUIT JSON 생성
    /// </summary>
    private void CreateFruitJSON(string emotion)
    {
        if (currentFruitInfoUI == null)
        {
            Debug.LogWarning("FruitInfoUI가 설정되지 않았습니다.");
            return;
        }

        var fruitData = new FruitEmotionData
        {
            taskId = currentFruitInfoUI.id,
            emotion = emotion,
            todo = currentFruitInfoUI.todo,
            date = currentFruitInfoUI.date,
            time = currentFruitInfoUI.time,
            acceptedAt = currentFruitInfoUI.acceptedAt,
            userId = currentFruitInfoUI.userId, // FruitInfoUI.userId가 null이면 그대로 null
            position = currentFruitInfoUI.transform.position,
            createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        try
        {
            string json = JsonUtility.ToJson(fruitData, true);
            Debug.Log("JsonUtility 결과: " + json);
            Debug.Log($"🍎 FRUIT JSON 생성 완료:\n{json}");
        }
        catch (Exception e)
        {
            Debug.LogError("CreateFruitJSON 예외 발생: " + e);
        }

        // 현재 Fruit이 Snap되어 있는지 확인하고 업데이트
        UpdateSnapDataIfNeeded(fruitData);

        // 여기서 서버로 전송하거나 다른 처리를 할 수 있습니다
        // SendToServer(json);
    }

    /// <summary>
    /// 현재 Fruit이 Snap되어 있다면 Snap 데이터 업데이트
    /// </summary>
    private void UpdateSnapDataIfNeeded(FruitEmotionData fruitData)
    {
        // SnapDataManager 찾기
        var snapDataManager = FindObjectOfType<SnapDataManager>();
        if (snapDataManager == null)
        {
            Debug.LogWarning("SnapDataManager를 찾을 수 없습니다.");
            return;
        }

        // 현재 Fruit이 Snap되어 있는 Snap 찾기
        var allSnapData = snapDataManager.GetAllSnapData();
        foreach (var snapData in allSnapData)
        {
            if (snapData.attachedFruitId == fruitData.taskId.ToString())
            {
                // Snap 데이터 업데이트
                snapDataManager.AttachFruitToSnap(snapData.snapId, fruitData);
                Debug.Log($"Snap 데이터 업데이트 완료: {snapData.snapId} - 감정: {fruitData.emotion}");
                return;
            }
        }

        Debug.Log("현재 Fruit이 Snap되어 있지 않습니다.");
    }

    /// <summary>
    /// 서버로 FRUIT JSON 전송 (필요시 구현)
    /// </summary>
    private void SendToServer(string json)
    {
        // TODO: 서버 전송 로직 구현
        Debug.Log("서버로 FRUIT JSON 전송: " + json);
    }

    void OnDestroy()
    {
        Debug.Log($"{gameObject.name}이(가) Destroy 되었습니다! (StackTrace: {Environment.StackTrace}) 부모: {transform.parent?.name}");
    }
}






