using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TodoListManager : MonoBehaviour
{
    [Header("ScrollRect → Viewport → Content")]
    public RectTransform content;       // ScrollRect.Content 에 드래그
    [Header("할 일 아이템 Prefab")]
    public GameObject itemPrefab;       // TodoItemPrefab 에 드래그
    [Header("초기 샘플 할 일들")]
    public List<string> initialTasks;   // 인스펙터에서 항목 추가

    void Start()
    {
        // Start 시 초기 목록 생성
        foreach (var task in initialTasks)
            CreateItem(task);
    }

    // 항목 하나 생성
    public void CreateItem(string text)
    {
        if (content == null || itemPrefab == null) return;

        // Instantiate, 부모(Content)에 자동 정렬
        var go = Instantiate(itemPrefab, content);
        go.transform.localScale = Vector3.one;  // 스케일 깨짐 방지

        // 텍스트 세팅
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
    }
}

