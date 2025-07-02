using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TMPro;

[Serializable]
public class TodoItemData
{
    public string todo;   // "저녁 약속"
    public string date;   // "2025-06-27"
    public string time;   // "17:00:00"
}

[Serializable]
public class TodoListData
{
    public List<TodoItemData> tasks;  // JSON의 "tasks" 배열과 매핑
}

public class TodoListManager : MonoBehaviour
{
    [Header("ScrollRect → Viewport → Content")]
    public RectTransform content;    // ScrollRect.Content 에 드래그
    [Header("할 일 아이템 Prefab")]
    public GameObject itemPrefab;    // TodoItemPrefab 에 드래그

    void Start()
    {
        // 예시: 실제로는 AI에서 받은 JSON 문자열을 넣어 호출하세요.
        string jsonFromAI = @"{
          ""tasks"": [
            { ""todo"": ""프로젝트 회의"", ""date"": ""2025-07-01"", ""time"": ""04:00:00"" },
            { ""todo"": ""운동 하기"", ""date"": ""2025-07-04"", ""time"": ""20:00:00"" }
          ]
        }";
        LoadFromJson(jsonFromAI);
    }

    /// <summary>
    /// AI → Unity로 받은 JSON을 파싱해서 콘텐츠에 아이템 생성
    /// </summary>
    public void LoadFromJson(string json)
    {
        if (string.IsNullOrEmpty(json) || content == null || itemPrefab == null)
            return;

        // JSON → TodoListData
        var listData = JsonUtility.FromJson<TodoListData>(json);
        // 기존 자식 지우기 (필요하다면)
        foreach (Transform child in content) Destroy(child.gameObject);

        // 각각의 TaskData를 포맷팅 후 CreateItem 호출
        foreach (var t in listData.tasks)
        {
            // "yyyy-MM-dd HH:mm:ss" 형식으로 파싱
            DateTime dt = DateTime.ParseExact(
                $"{t.date} {t.time}",
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture
            );

            // 포맷: "6월 27일 17시에 저녁 약속"
            string formatted = $"{dt.Month}월 {dt.Day}일 {dt.Hour}시에 {t.todo}";
            CreateItem(formatted);
        }
    }

    /// <summary>
    /// 텍스트 하나로 TodoItemPrefab 생성
    /// </summary>
    public void CreateItem(string text)
    {
        var go = Instantiate(itemPrefab, content);
        go.transform.localScale = Vector3.one;  // 스케일 깨짐 방지

        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = text;
    }
}

