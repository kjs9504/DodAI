using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;
using System.Text;
using System.Collections;


[Serializable]
public class FruitEmotionData
{
    public string emotion;        // 감정 정보 (필수)
    public string todo;          // 할 일 (필수)
    public string date;          // 날짜 (필수) - yyyy-MM-dd 형식
    public string time;          // 시간 (필수) - HH:mm:ss 형식
    public Position position;    // 위치 정보 (JSONB)
    public string acceptedAt;    // 수락 시간 (필수) - yyyy-MM-dd HH:mm:ss 형식
    public string createdAt;     // 생성 시간 (필수) - yyyy-MM-dd HH:mm:ss 형식
}

public class EmojiController : MonoBehaviour
{
    public FruitEmotionData lastEmotionData; // 감정 선택 시 여기에 저장

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

        // Item 클래스에 원래 스케일 저장
        [HideInInspector] public Vector3 originalScale;
    }

    [Tooltip("Public에서 할당한 UI 이미지(5개)에 1개씩 Item을 추가하세요.")]
    public List<Item> items = new List<Item>(5);

    // FruitInfoUI 참조 (감정 선택 시 데이터를 가져오기 위해)
    private FruitInfoUI currentFruitInfoUI;

    void Awake()
    {
        // 감정 타입 설정
        string[] emotions = { "즐거움", "슬픔", "분노", "허무감", "달성감" };
        
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

            // Awake()에서 저장
            it.originalScale = it.targetObject.transform.localScale;
        }
    }

    /// <summary>
    /// 현재 선택된 FruitInfoUI 설정 (감정 선택 시 데이터를 가져오기 위해)
    /// </summary>
    public void SetCurrentFruitInfoUI(FruitInfoUI fruitInfoUI)
    {
        currentFruitInfoUI = fruitInfoUI;
    }

    /// <summary>
    /// emotion 값에 따라 UI/3D 오브젝트 상태를 변경
    /// </summary>
    public void SetEmotion(string emotion)
    {
        Debug.Log($"[EmojiController] SetEmotion 호출: emotion={emotion}");
        foreach (var it in items)
        {
            Debug.Log($"[EmojiController] 비교: it.emotionType={it.emotionType}, emotion={emotion}");
            if (it.emotionType == emotion)
            {
                Debug.Log($"[EmojiController] 적용: {emotion} → pressedMesh/Material");
                if (it.pressedMesh != null) it.mf.mesh = it.pressedMesh;
                if (it.pressedMaterial != null) it.mr.material = it.pressedMaterial;
            }
            else
            {
                if (it.normalMesh != null) it.mf.mesh = it.normalMesh;
                if (it.normalMaterial != null) it.mr.material = it.normalMaterial;
            }
        }
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
        it.targetObject.transform.localScale = it.originalScale; // ← 문제의 코드

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

        lastEmotionData = new FruitEmotionData
        {
            emotion = emotion,
            todo = currentFruitInfoUI.todo,
            date = currentFruitInfoUI.date,  // 기존 Fruit의 date 사용
            time = currentFruitInfoUI.time,  // 기존 Fruit의 time 사용
            position = new Position { x = currentFruitInfoUI.transform.position.x, y = currentFruitInfoUI.transform.position.y, z = currentFruitInfoUI.transform.position.z },
            acceptedAt = currentFruitInfoUI.acceptedAt,  // 기존 Fruit의 acceptedAt 사용
            createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        };

        try
        {
            string json = JsonUtility.ToJson(lastEmotionData, true);
            Debug.Log("JsonUtility 결과: " + json);
            Debug.Log($"🍎 FRUIT JSON 생성 완료:\n{json}");
            
                    // 디버깅 정보
        Debug.Log($"🔍 emotion 값: {lastEmotionData.emotion}");
        Debug.Log($"🔍 todo 값: {lastEmotionData.todo}");
        Debug.Log($"🔍 date 값: {lastEmotionData.date}");
        Debug.Log($"🔍 time 값: {lastEmotionData.time}");
        Debug.Log($"🔍 acceptedAt 값: {lastEmotionData.acceptedAt}");
        Debug.Log($"🔍 position 값: ({lastEmotionData.position.x}, {lastEmotionData.position.y}, {lastEmotionData.position.z})");
        }
        catch (Exception e)
        {
            Debug.LogError("CreateFruitJSON 예외 발생: " + e);
        }
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






