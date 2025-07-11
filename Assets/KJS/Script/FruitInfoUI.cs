using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Globalization;
using System;

[RequireComponent(typeof(Collider))] // Ŭ���� �������� Collider �ʿ�
public class FruitInfoUI : MonoBehaviour, IPointerClickHandler
{
    [Header("�� �� ������Ʈ�� Task ������ (FruitManager ���� Initialize �� �� ��)")]
    public long id;
    public string todo;
    public string date;
    public string time;
    public string acceptedAt;
    public long? userId;

    [Header("�� Ŭ�� �� ������ UI �г�")]
    [Tooltip("Inspector ���� �Ҵ��ؾ� �մϴ�.")]
    public GameObject infoPanel;

    [Header("�� UI �г� ���� �ؽ�Ʈ��")]
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
    /// FruitManager ���� ������ �����͸� ������ �� �� ȣ���ϼ���.
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
            // "2025-07-04" ���� ���ڿ����� ������ �Ľ�
            var dt = DateTime.ParseExact(data.date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            billboardText.text = $"{dt.Month}�� {dt.Day}���� ����";
        }
    }

    /// <summary>
    /// Ŭ���Ǹ� UI �г��� ����ϸ鼭, �ؽ�Ʈ�� �����մϴ�.
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

