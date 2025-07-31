using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;
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
    public long? userId;
}

public class EmojiController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public FruitEmotionData lastEmotionData;

    [System.Serializable]
    public class Item
    {
        [Header("클릭할 UI Image (버튼)")]
        public Image uiButton;

        [Header("이 버튼이 제어할 3D 오브젝트 (Inspector에서 할당 가능)")]
        public GameObject targetObject;

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
    private Item currentPressedItem;

    void Awake()
    {
        string[] emotions = { "즐거움", "슬픔", "분노", "허무감", "달성감" };

        for (int index = 0; index < items.Count; index++)
        {
            var it = items[index];
            it.emotionType = (index < emotions.Length) ? emotions[index] : $"Unknown{index}";

            // 버튼 체크
            if (it.uiButton == null)
            {
                Debug.LogWarning($"[EmojiController] Item[{index}] uiButton 미할당");
                continue;
            }

            // targetObject 설정
            if (it.targetObject == null)
            {
                var sphereTr = it.uiButton.transform
                    .GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "Sphere");
                it.targetObject = sphereTr != null ? sphereTr.gameObject : it.uiButton.gameObject;
            }

            // MeshFilter & MeshRenderer
            it.mf = it.targetObject.GetComponentInChildren<MeshFilter>();
            it.mr = it.targetObject.GetComponentInChildren<MeshRenderer>();
            if (it.mf == null || it.mr == null)
            {
                Debug.LogError($"Item[{index}] MeshFilter/MeshRenderer 누락");
                continue;
            }

            // 기본 Mesh & Material 저장
            it.normalMesh = it.mf.sharedMesh;
            it.normalMaterial = it.mr.sharedMaterial;
            it.originalScale = it.targetObject.transform.localScale;
        }
    }

    public void SetCurrentFruitInfoUI(FruitInfoUI fruitInfoUI)
    {
        currentFruitInfoUI = fruitInfoUI;
    }

    public void SetEmotion(string emotion)
    {
        Debug.Log($"[EmojiController] SetEmotion('{emotion}') 호출됨");

        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("[EmojiController] SetEmotion: items가 null 또는 비어있음");
            return;
        }

        if (emotion == "none" || emotion == "NONE") emotion = "";

        // 모든 아이템 기본 상태로 복원
        foreach (var it in items)
        {
            if (it.mf != null) it.mf.sharedMesh = it.normalMesh;
            if (it.mr != null) it.mr.sharedMaterial = it.normalMaterial;
        }

        if (string.IsNullOrEmpty(emotion))
        {
            if (currentFruitInfoUI != null) currentFruitInfoUI.SetEmotion("");
            return;
        }

        // 해당 감정만 눌림 상태로 적용
        var target = items.FirstOrDefault(it => it.emotionType == emotion);
        if (target != null)
        {
            if (target.pressedMesh != null) target.mf.sharedMesh = target.pressedMesh;
            if (target.pressedMaterial != null) target.mr.sharedMaterial = target.pressedMaterial;

            if (currentFruitInfoUI != null) currentFruitInfoUI.SetEmotion(emotion);
        }
        else
        {
            Debug.LogWarning($"[EmojiController] '{emotion}' 감정 아이템 찾지 못함");
            if (currentFruitInfoUI != null) currentFruitInfoUI.SetEmotion("");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        var pressedObj = eventData.pointerPressRaycast.gameObject;
        Debug.Log($"[EmojiController] OnPointerDown 대상: {pressedObj?.name}");

        var item = FindItemByObject(pressedObj);
        if (item == null)
        {
            Debug.LogWarning("[EmojiController] OnPointerDown: item null");
            return;
        }

        currentPressedItem = item;

        // 눌린 상태 적용
        if (item.pressedMesh != null) item.mf.sharedMesh = item.pressedMesh;
        if (item.pressedMaterial != null) item.mr.sharedMaterial = item.pressedMaterial;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (currentPressedItem == null)
        {
            Debug.LogWarning("[EmojiController] OnPointerUp: currentPressedItem null");
            return;
        }

        // 감정 적용
        if (currentFruitInfoUI != null)
            currentFruitInfoUI.SetEmotion(currentPressedItem.emotionType);

        SetEmotion(currentPressedItem.emotionType);

        // Fruit JSON 생성
        CreateFruitJSON(currentPressedItem.emotionType);

        // 눌림 상태 유지하지 않으려면 이거 복원 제거 가능
        currentPressedItem = null;
    }

    private void CreateFruitJSON(string emotion)
    {
        if (currentFruitInfoUI == null) return;

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
            userId = currentFruitInfoUI.userId
        };

        Debug.Log($"🍎 FRUIT JSON:\n{JsonUtility.ToJson(lastEmotionData, true)}");
    }

    /// <summary>
    /// 눌린 오브젝트와 부모 트리까지 검사하여 아이템 찾기
    /// </summary>
    private Item FindItemByObject(GameObject go)
    {
        if (go == null) return null;

        foreach (var it in items)
        {
            if (it.uiButton == null) continue;

            if (go == it.uiButton.gameObject || go == it.targetObject)
                return it;

            // 부모/자식까지 체크
            if (go.transform.IsChildOf(it.uiButton.transform) || go.transform.IsChildOf(it.targetObject.transform))
                return it;
        }

        return null;
    }
}
