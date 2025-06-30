using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TMPro;

[Serializable]
public class TodoItemData
{
    public string todo;   // "���� ���"
    public string date;   // "2025-06-27"
    public string time;   // "17:00:00"
}

[Serializable]
public class TodoListData
{
    public List<TodoItemData> tasks;  // JSON�� "tasks" �迭�� ����
}

public class TodoListManager : MonoBehaviour
{
    [Header("ScrollRect �� Viewport �� Content")]
    public RectTransform content;    // ScrollRect.Content �� �巡��
    [Header("�� �� ������ Prefab")]
    public GameObject itemPrefab;    // TodoItemPrefab �� �巡��

    void Start()
    {
        // ����: �����δ� AI���� ���� JSON ���ڿ��� �־� ȣ���ϼ���.
        string jsonFromAI = @"{
          ""tasks"": [
            { ""todo"": ""������Ʈ ȸ��"", ""date"": ""2025-04-30"", ""time"": ""04:00:00"" },
            { ""todo"": ""� �ϱ�"", ""date"": ""2024-03-13"", ""time"": ""20:00:00"" }
          ]
        }";
        LoadFromJson(jsonFromAI);
    }

    /// <summary>
    /// AI �� Unity�� ���� JSON�� �Ľ��ؼ� �������� ������ ����
    /// </summary>
    public void LoadFromJson(string json)
    {
        if (string.IsNullOrEmpty(json) || content == null || itemPrefab == null)
            return;

        // JSON �� TodoListData
        var listData = JsonUtility.FromJson<TodoListData>(json);
        // ���� �ڽ� ����� (�ʿ��ϴٸ�)
        foreach (Transform child in content) Destroy(child.gameObject);

        // ������ TaskData�� ������ �� CreateItem ȣ��
        foreach (var t in listData.tasks)
        {
            // "yyyy-MM-dd HH:mm:ss" �������� �Ľ�
            DateTime dt = DateTime.ParseExact(
                $"{t.date} {t.time}",
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture
            );

            // ����: "6�� 27�� 17�ÿ� ���� ���"
            string formatted = $"{dt.Month}�� {dt.Day}�� {dt.Hour}�ÿ� {t.todo}";
            CreateItem(formatted);
        }
    }

    /// <summary>
    /// �ؽ�Ʈ �ϳ��� TodoItemPrefab ����
    /// </summary>
    public void CreateItem(string text)
    {
        var go = Instantiate(itemPrefab, content);
        go.transform.localScale = Vector3.one;  // ������ ���� ����

        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = text;
    }
}

