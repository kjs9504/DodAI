using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

public class CalenderManager : MonoBehaviour
{
    [Header("����-�����̽� UI ����")]
    public Transform gridParent;         // GridLayoutGroup�� ���� �θ�
    public GameObject dayCellPrefab;     // TextMeshProUGUI ���Ե� Ķ���� �� ������
    public TextMeshProUGUI monthLabel;   // ��2025�� 07���� �� ǥ���� �ؽ�Ʈ
    public TodoListManager todoListManager; // 인스펙터에서 연결

    private int year, month;
    private Queue<GameObject> cellPool = new Queue<GameObject>();

    enum DayType { PreviousMonth, CurrentMonth, Today, NextMonth }

    void Start()
    {
        var now = DateTime.Now;
        year = now.Year;
        month = now.Month;
        DrawCalendar();
    }

    public void DrawCalendar()
    {
        // 1) ���� �� �ʱ�ȭ
        foreach (Transform child in gridParent)
        {
            child.gameObject.SetActive(false);
            cellPool.Enqueue(child.gameObject);
        }

        // 2) ���̺� ����
        monthLabel.text = new DateTime(year, month, 1)
                              .ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        // 3) ù° ���ϡ��ϼ�, ���������� �� ����
        DateTime firstDay = new DateTime(year, month, 1);
        int startWeekday = (int)firstDay.DayOfWeek;        // ��=0, ��=1...
        int daysInMonth = DateTime.DaysInMonth(year, month);

        int prevMonth = month - 1, prevYear = year;
        if (prevMonth < 1) { prevMonth = 12; prevYear--; }
        int daysInPrevMonth = DateTime.DaysInMonth(prevYear, prevMonth);

        // 4) ���� �� ��¥ ä���
        for (int i = 0; i < startWeekday; i++)
        {
            int dayNumber = daysInPrevMonth - startWeekday + i + 1;
            StyleCell(dayNumber, DayType.PreviousMonth);
        }

        // 5) �̹� �� ��¥ ä���
        DateTime today = DateTime.Now.Date;
        for (int d = 1; d <= daysInMonth; d++)
        {
            DateTime thisDay = new DateTime(year, month, d);
            if (thisDay == today)
                StyleCell(d, DayType.Today);
            else
                StyleCell(d, DayType.CurrentMonth);
        }

        // 6) ���� �� ��¥ ä���
        int total = startWeekday + daysInMonth;
        int rows = Mathf.CeilToInt(total / 7f);
        int slots = rows * 7;
        int nextDays = slots - total;

        for (int d = 1; d <= nextDays; d++)
            StyleCell(d, DayType.NextMonth);
    }

    private void StyleCell(int dayNumber, DayType type)
    {
        // Get or create
        GameObject cell = cellPool.Count > 0
            ? cellPool.Dequeue()
            : Instantiate(dayCellPrefab, gridParent);
        cell.transform.SetParent(gridParent, false);
        cell.SetActive(true);

        // Text ����
        var txt = cell.GetComponentInChildren<TextMeshProUGUI>();
        txt.text = dayNumber.ToString();

        // Background(Image) ����
        var bg = cell.GetComponent<Image>();
        switch (type)
        {
            case DayType.PreviousMonth:
                txt.color = new Color(0, 0, 0, 0.3f);
                if (bg != null) bg.color = new Color(1, 1, 1, 0.2f);
                break;

            case DayType.NextMonth:
                txt.color = new Color(0, 0, 0, 0.3f);
                if (bg != null) bg.color = new Color(1, 1, 1, 0.2f);
                break;

            case DayType.CurrentMonth:
                // �ؽ�Ʈ�� ���� ����
                txt.color = new Color32(30, 30, 30, 255);

                if (bg != null)
                {
                    // ��� ���, ���� ������
                    bg.color = new Color32(255, 255, 255, 255);
                }
                break;

            case DayType.Today:
                txt.color = Color.white;
                if (bg != null) bg.color = new Color(0.6f, 0.2f, 1f, 1f);
                break;
        }

        // 날짜 셀 클릭 이벤트 연결 (현재 달만)
        var btn = cell.GetComponent<Button>();
        if (btn != null && (type == DayType.CurrentMonth || type == DayType.Today))
        {
            string dateStr = $"{year}-{month:D2}-{dayNumber:D2}";
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                Debug.Log($"📅 날짜 클릭: {dateStr}");
                if (todoListManager != null)
                {
                    StartCoroutine(todoListManager.FetchAndShowTasksForDate(dateStr));
                }
                else
                {
                    Debug.LogError("❌ TodoListManager가 할당되지 않았습니다!");
                }
            });
        }
    }
}
