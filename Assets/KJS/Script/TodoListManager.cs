using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

[Serializable]
public class TodoItemData
{
    public long id;      // 추가
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

    public List<TodoItemData> allTasks = new List<TodoItemData>();

    void Start()
    {
        // 예시: 실제로는 AI에서 받은 JSON 문자열을 넣어 호출하세요.
        //string jsonFromAI = @"{
        //  ""tasks"": [
        //    { ""todo"": ""프로젝트 회의"", ""date"": ""2025-07-01"", ""time"": ""04:00:00"" },
        //    { ""todo"": ""운동 하기"", ""date"": ""2025-07-04"", ""time"": ""20:00:00"" },
        //    { ""todo"": ""목욕 하기"", ""date"": ""2025-07-04"", ""time"": ""17:00:00"" }
        //  ]
        //}";
        //LoadFromJson(jsonFromAI);
    }

    /// <summary>
    /// AI → Unity로 받은 JSON을 파싱해서 콘텐츠에 아이템 생성
    /// </summary>
    public void LoadFromJson(string json)
    {
        Debug.Log("받아온 JSON: " + json); // ★ 받아온 원본 JSON 로그

        if (string.IsNullOrEmpty(json) || content == null || itemPrefab == null)
            return;

        // JSON → TodoListData
        var listData = JsonUtility.FromJson<TodoListData>(json);
        allTasks = listData.tasks; // 전체 할 일 저장

        // 각 항목의 id 로그 출력
        foreach (var t in allTasks)
        {
            Debug.Log($"할 일: id={t.id}, todo={t.todo}, date={t.date}, time={t.time}");
        }

        // 기존 자식 지우기 (필요하다면)
        foreach (Transform child in content) Destroy(child.gameObject);
    }

    /// <summary>
    /// 텍스트 하나로 TodoItemPrefab 생성
    /// </summary>
    public void CreateItem(TodoItemData data)
    {
        var go = Instantiate(itemPrefab, content);
        var item = go.GetComponent<TodoItem>();
        item.data = data; // ★ id 포함 전체 복사
        go.transform.localScale = Vector3.one;  // 스케일 깨짐 방지

        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            DateTime dt = DateTime.ParseExact($"{data.date} {data.time}", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            tmp.text = $"{dt.Month}월 {dt.Day}일 {dt.Hour}시에 {data.todo}";
        }
    }

    /// <summary>
    /// 특정 날짜의 할 일만 표시
    /// </summary>
    public void ShowTasksForDate(string date)
    {
        // 기존 UI 아이템 삭제
        foreach (Transform child in content) Destroy(child.gameObject);

        // 해당 날짜의 할 일만 생성
        foreach (var t in allTasks)
        {
            if (t.date == date)
            {
                DateTime dt = DateTime.ParseExact($"{t.date} {t.time}", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                string formatted = $"{dt.Month}월 {dt.Day}일 {dt.Hour}시에 {t.todo}";
                CreateItem(t); // 해당 TodoItemData를 사용하여 CreateItem 호출
            }
        }
    }

    /// <summary>
    /// TodoList 전체 UI를 보이게
    /// </summary>
    public void ShowList()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// TodoList 전체 UI를 숨김
    /// </summary>
    public void HideList()
    {
        gameObject.SetActive(false);
    }

    public IEnumerator FetchAndShowTasksForDate(string dateStr)
    {
        string url = "http://localhost:8080/api/tasks/show"; // 백엔드 주소에 맞게 수정
        Debug.Log($"🌐 백엔드 요청 시작: {url}");
        
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                Debug.Log($"✅ 백엔드 응답 성공: {json}");
                LoadFromJson(json); // 전체 할 일 목록 갱신
                ShowList();
                ShowTasksForDate(dateStr); // 해당 날짜만 표시
            }
            else
            {
                Debug.LogError($"❌ 할 일 목록 불러오기 실패: {www.error} (응답코드: {www.responseCode})");
            }
        }
    }

    public void DeleteTask(TodoItemData data)
    {
        Debug.Log($"삭제 요청: id={data.id}, todo={data.todo}");
        // 삭제용 JSON 생성
        TodoListData deleteList = new TodoListData();
        deleteList.tasks = new List<TodoItemData> { data };
        string json = JsonUtility.ToJson(deleteList);
        // ... 서버로 전송 ...
    }
}