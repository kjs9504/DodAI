using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;

[Serializable]
public class FruitEmotionData
{
    public string emotion;
    public string todo;
    public string date;
    public string time;
    public Position position;
    public string acceptedAt;
    public string createdAt;
    public long? userId; // AcceptedTaskData와 호환성을 위해 추가
}

public class EmojiController : MonoBehaviour
{
    public FruitEmotionData lastEmotionData;

    [System.Serializable]
    public class Item
    {
        [Header("클릭할 UI Image (버튼)")]
        public Image uiButton;

        [Header("이 버튼이 제어할 3D 오브젝트 (Inspector에서 할당 가능)")]
        public GameObject targetObject;  // ← 수정: Inspector 우선 할당

        [Header("이 버튼의 눌린 상태 Mesh/Material")]
        public Mesh pressedMesh;
        public Material pressedMaterial;

        [HideInInspector] public Mesh normalMesh;
        [HideInInspector] public Material normalMaterial;
        [HideInInspector] public MeshFilter mf;
        [HideInInspector] public MeshRenderer mr;
        [HideInInspector] public string emotionType;
        [HideInInspector] public Vector3 originalScale;
    }

    public List<Item> items = new List<Item>(5);
    private FruitInfoUI currentFruitInfoUI;

    void Awake()
    {
        string[] emotions = { "즐거움", "슬픔", "분노", "허무감", "달성감" };

        for (int index = 0; index < items.Count; index++)
        {
            var it = items[index];
            it.emotionType = (index < emotions.Length) ? emotions[index] : $"Unknown{index}";

            // 1) uiButton 체크
            if (it.uiButton == null)
            {
                Debug.LogWarning($"[EmojiController] Item[{index}]의 uiButton이 할당되지 않았습니다");
                continue;
            }

            // 2) targetObject: Inspector에 할당된 게 있으면 그대로, 없으면 uiButton 자식에서 Sphere 검색
            if (it.targetObject == null)
            {
                var sphereTr = it.uiButton.transform
                    .GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "Sphere");
                if (sphereTr != null)
                {
                    it.targetObject = sphereTr.gameObject;
                    Debug.Log($"[EmojiController] Item[{index}] uiButton 자식 Sphere 할당: {it.targetObject.name}");
                }
                else
                {
                    it.targetObject = it.uiButton.gameObject;
                    Debug.LogWarning($"[EmojiController] Item[{index}] Sphere를 찾지 못해 uiButton으로 대체");
                }
            }
            else
            {
                Debug.Log($"[EmojiController] Item[{index}] Inspector로 targetObject 지정: {it.targetObject.name}");
            }

            // 3) MeshFilter/MeshRenderer 캐시
            it.mf = it.targetObject.GetComponentInChildren<MeshFilter>();
            it.mr = it.targetObject.GetComponentInChildren<MeshRenderer>();
            if (it.mf == null || it.mr == null)
            {
                Debug.LogError($"Item[{index}]에 MeshFilter/MeshRenderer 누락");
                continue;
            }

            // 4) 기본 상태 저장 (인스턴스 복사)
            it.normalMesh = it.mf.sharedMesh;
            it.normalMaterial = it.mr.sharedMaterial;
            
            // 기본 상태 저장 확인
            if (it.normalMesh != null)
            {
                Debug.Log($"[EmojiController] Item[{index}] normalMesh 저장 완료: {it.normalMesh.name}");
            }
            else
            {
                Debug.LogError($"[EmojiController] Item[{index}] normalMesh 저장 실패!");
            }
            
            if (it.normalMaterial != null)
            {
                Debug.Log($"[EmojiController] Item[{index}] normalMaterial 저장 완료: {it.normalMaterial.name}");
            }
            else
            {
                Debug.LogError($"[EmojiController] Item[{index}] normalMaterial 저장 실패!");
            }

            // 5) Raycast 활성화
            it.uiButton.raycastTarget = true;

            // 6) EventTrigger 연결
            var trig = it.uiButton.gameObject.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener((data) => OnPressed(it));
            trig.triggers.Add(down);
            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener((data) => OnReleased(it));
            trig.triggers.Add(up);

            it.originalScale = it.targetObject.transform.localScale;
        }
    }

    public void SetCurrentFruitInfoUI(FruitInfoUI fruitInfoUI)
    {
        currentFruitInfoUI = fruitInfoUI;
    }

    public void SetEmotion(string emotion)
    {
        Debug.Log($"[EmojiController] 감정 '{emotion}' 설정 시작");

        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("items가 초기화되지 않았습니다. 지연 적용.");
            StartCoroutine(SetEmotionDelayed(emotion));
            return;
        }

        // 'none' 값을 빈 문자열로 처리
        if (emotion == "none" || emotion == "NONE")
        {
            emotion = "";
            Debug.Log("[EmojiController] 'none' 값을 빈 문자열로 변환");
        }

        // --- 수정: 1) 모든 아이템을 기본 상태로 리셋 ---
        foreach (var it in items)
        {
            if (it.mf != null && it.normalMesh != null) 
            {
                it.mf.sharedMesh = it.normalMesh;
                Debug.Log($"[EmojiController] SetEmotion: '{it.emotionType}' 기본 메쉬 복원: {it.normalMesh.name}");
            }
            if (it.mr != null && it.normalMaterial != null) 
            {
                it.mr.sharedMaterial = it.normalMaterial;
                Debug.Log($"[EmojiController] SetEmotion: '{it.emotionType}' 기본 머티리얼 복원: {it.normalMaterial.name}");
            }
        }

        // 감정이 비어있으면 아무것도 적용하지 않음
        if (string.IsNullOrEmpty(emotion))
        {
            Debug.Log("[EmojiController] 감정이 비어있어서 아무것도 적용하지 않음");
            // FruitInfoUI의 currentEmotion도 빈 문자열로 설정
            if (currentFruitInfoUI != null)
                currentFruitInfoUI.SetEmotion("");
            return;
        }

        // --- 수정: 2) 해당 감정 아이템만 적용 ---
        var target = items.FirstOrDefault(it => it.emotionType == emotion);
        if (target != null)
        {
            if (target.pressedMesh != null && target.mf != null)
            {
                target.mf.sharedMesh = target.pressedMesh;
                Debug.Log($"[EmojiController] SetEmotion: '{emotion}' 눌린 메쉬 적용: {target.pressedMesh.name}");
            }
            if (target.pressedMaterial != null && target.mr != null)
            {
                target.mr.sharedMaterial = target.pressedMaterial;
                Debug.Log($"[EmojiController] SetEmotion: '{emotion}' 눌린 머티리얼 적용: {target.pressedMaterial.name}");
            }
            Debug.Log($"[EmojiController] 감정 '{emotion}' 적용 완료");
            // FruitInfoUI의 currentEmotion도 갱신
            if (currentFruitInfoUI != null)
                currentFruitInfoUI.SetEmotion(emotion);
        }
        else
        {
            Debug.LogWarning($"감정 '{emotion}' 타입을 찾을 수 없습니다. 사용 가능한 감정: {string.Join(", ", items.Select(it => it.emotionType))}");
            // FruitInfoUI의 currentEmotion은 빈 문자열로 설정
            if (currentFruitInfoUI != null)
                currentFruitInfoUI.SetEmotion("");
        }
    }

    private void OnPressed(Item it)
    {
        if (it.mf == null || it.mr == null) return;
        
        // pressedMesh와 pressedMaterial 적용
        if (it.pressedMesh != null) 
        {
            it.mf.sharedMesh = it.pressedMesh;
            Debug.Log($"[EmojiController] {it.emotionType} 아이템에 pressedMesh 적용: {it.pressedMesh.name}");
        }
        if (it.pressedMaterial != null) 
        {
            it.mr.sharedMaterial = it.pressedMaterial;
            Debug.Log($"[EmojiController] {it.emotionType} 아이템에 pressedMaterial 적용: {it.pressedMaterial.name}");
        }
    }

    private void OnReleased(Item it)
    {
    // ✅ 손을 떼면 SetEmotion을 호출해서 선택된 감정의 Mesh를 유지
    if (currentFruitInfoUI != null)
    {
        currentFruitInfoUI.SetEmotion(it.emotionType);
    }

    // ✅ 이게 pressedMesh를 유지하게 해줌
    SetEmotion(it.emotionType);

        // 3) JSON 생성 (로컬 저장용)
        CreateFruitJSON(it.emotionType);

        // 4) 원래 스케일로 복원
        it.targetObject.transform.localScale = it.originalScale;
        
        Debug.Log($"[EmojiController] 감정 '{it.emotionType}' 설정 완료. 저장하려면 저장 버튼을 눌러주세요.");
    }

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
            date = currentFruitInfoUI.date,
            time = currentFruitInfoUI.time,
            position = new Position
            {
                x = currentFruitInfoUI.transform.position.x,
                y = currentFruitInfoUI.transform.position.y,
                z = currentFruitInfoUI.transform.position.z
            },
            acceptedAt = currentFruitInfoUI.acceptedAt,
            createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            userId = currentFruitInfoUI.userId // AcceptedTaskData와 호환성을 위해 추가
        };
        string json = JsonUtility.ToJson(lastEmotionData, true);
        Debug.Log($"🍎 FRUIT JSON:\n{json}");
    }

    private IEnumerator SetEmotionDelayed(string emotion)
    {
        while (items == null || items.Count == 0)
            yield return null;
        SetEmotion(emotion);
    }

    void OnDestroy()
    {
        Debug.Log($"{gameObject.name} Destroy (StackTrace: {Environment.StackTrace})");
    }
}