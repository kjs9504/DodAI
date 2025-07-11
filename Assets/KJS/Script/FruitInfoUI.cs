using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Globalization;
using System;

[RequireComponent(typeof(Collider))] // 클릭을 받으려면 Collider 필요
public class FruitInfoUI : MonoBehaviour, IPointerClickHandler
{
    [Header("▼ 이 오브젝트의 Task 데이터 (FruitManager 에서 Initialize 해 줄 것)")]
    public long id;
    public string todo;
    public string date;
    public string time;
    public string acceptedAt;
    public long? userId;

    [Header("▼ 클릭 시 보여줄 UI 패널")]
    [Tooltip("Inspector 에서 할당해야 합니다.")]
    public GameObject infoPanel;

    [Header("▼ UI 패널 안의 텍스트들")]
    public TextMeshProUGUI todoText;
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI acceptedAtText;
    public TextMeshProUGUI userIdText;

    private void Awake()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    public TextMeshProUGUI billboardText;

    /// <summary>
    /// FruitManager 에서 레시피 데이터를 전달해 줄 때 호출하세요.
    /// </summary>
    public void Initialize(AcceptedTaskData data)
    {
        id = data.id;
        todo = data.todo;
        date = data.date;
        time = data.time;
        acceptedAt = data.acceptedAt;
        userId = data.userId;

        if (billboardText != null)
        {
            // "2025-07-04" 같은 문자열에서 월·일 파싱
            var dt = DateTime.ParseExact(data.date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            billboardText.text = $"{dt.Month}월 {dt.Day}일의 열매";
        }
    }

    /// <summary>
    /// 클릭되면 UI 패널을 토글하면서, 텍스트를 갱신합니다.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (infoPanel == null) return;

        bool now = !infoPanel.activeSelf;
        infoPanel.SetActive(now);

        if (now)
            UpdateUI();
    }

    private void UpdateUI()
    {
        if (todoText != null) todoText.text = $"Todo: {todo}";
        if (dateText != null) dateText.text = $"Date: {date}";
        if (timeText != null) timeText.text = $"Time: {time}";
        if (acceptedAtText != null) acceptedAtText.text = $"Accepted: {acceptedAt}";
        if (userIdText != null) userIdText.text = userId.HasValue
                                        ? $"User ID: {userId.Value}"
                                        : "User ID: (none)";
    }
}

