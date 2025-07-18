using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

public class CalenderManager1 : MonoBehaviour
{
    [Header("캘린더 인터페이스 UI 참조")]
    public Transform gridParent;         // GridLayoutGroup을 가진 부모
    public GameObject dayCellPrefab;     // TextMeshProUGUI 컴포넌트 캘린더 셀 프리팹
    public TextMeshProUGUI monthEnglishLabel;   // 영문 월 표시 (January, February...)
    public TextMeshProUGUI monthNumberLabel;    // 숫자 월 표시 (01, 02, 03...)
    public TextMeshProUGUI yearLabel;           // 숫자 년도 표시 (2025)
    public TextMeshProUGUI dayNumberLabel;      // 일자 숫자 표시 (01, 02, 03...)
    public TextMeshProUGUI dayOfWeekLabel;      // 요일 영문 표시 (Monday, Tuesday...)
    public TodoListManager todoListManager;    // 인스펙터에서 연결

    private int year, month;
    private Queue<GameObject> cellPool = new Queue<GameObject>();
    private string lastDateClicked = null;

    enum DayType { PreviousMonth, CurrentMonth, Today, NextMonth }

    void Start()
    {
        var now = DateTime.Now;
        year = now.Year;
        month = now.Month;
        if (todoListManager != null)
            todoListManager.HideList();      // TodoListManager가 달린 GameObject 비활성화

        UpdateDateLabels(now);  // 초기 날짜 라벨 업데이트
        DrawCalendar();
    }

    private void UpdateDateLabels(DateTime currentDate)
    {
        // 영문 월 표시
        if (monthEnglishLabel != null)
            monthEnglishLabel.text = currentDate.ToString("MMMM", CultureInfo.InvariantCulture);

        // 숫자 월 표시
        if (monthNumberLabel != null)
            monthNumberLabel.text = currentDate.ToString("MM");

        // 숫자 년도 표시
        if (yearLabel != null)
            yearLabel.text = currentDate.ToString("yyyy");

        // 일자 숫자 표시
        if (dayNumberLabel != null)
            dayNumberLabel.text = currentDate.ToString("dd");

        // 요일 영문 표시
        if (dayOfWeekLabel != null)
            dayOfWeekLabel.text = currentDate.ToString("dddd", CultureInfo.InvariantCulture);
    }

    public void DrawCalendar()
    {
        // 1) 기존 셀 초기화
        foreach (Transform child in gridParent)
        {
            child.gameObject.SetActive(false);
            cellPool.Enqueue(child.gameObject);
        }

        // 2) 현재 날짜로 라벨 업데이트
        DateTime currentDate = new DateTime(year, month, 1);
        UpdateDateLabels(DateTime.Now);  // 오늘 날짜 기준으로 표시

        // 3) 첫째 날의 요일, 총 일수 계산
        DateTime firstDay = new DateTime(year, month, 1);
        int startWeekday = (int)firstDay.DayOfWeek;        // 일=0, 월=1...
        int daysInMonth = DateTime.DaysInMonth(year, month);

        int prevMonth = month - 1, prevYear = year;
        if (prevMonth < 1) { prevMonth = 12; prevYear--; }
        int daysInPrevMonth = DateTime.DaysInMonth(prevYear, prevMonth);

        // 4) 이전 달 날짜 채우기
        for (int i = 0; i < startWeekday; i++)
        {
            int dayNumber = daysInPrevMonth - startWeekday + i + 1;
            StyleCell(dayNumber, DayType.PreviousMonth);
        }

        // 5) 이번 달 날짜 채우기
        DateTime today = DateTime.Now.Date;
        for (int d = 1; d <= daysInMonth; d++)
        {
            DateTime thisDay = new DateTime(year, month, d);
            if (thisDay == today)
                StyleCell(d, DayType.Today);
            else
                StyleCell(d, DayType.CurrentMonth);
        }

        // 6) 다음 달 날짜 채우기
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

        // Text 설정
        var txt = cell.GetComponentInChildren<TextMeshProUGUI>();
        txt.text = dayNumber.ToString();

        // Background(Image) 설정
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
                // 텍스트는 진한 검정
                txt.color = new Color32(30, 30, 30, 255);

                if (bg != null)
                {
                    // 흰색 배경, 투명도 최대
                    bg.color = new Color32(1, 1, 1, 1);
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
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"📅 날짜 클릭: {dateStr}");
                if (todoListManager == null)
                {
                    Debug.LogError("❌ TodoListManager가 할당되지 않았습니다!");
                    return;
                }

                // ❸ 같은 날짜를 두 번 누르면 숨기고, 아니면 보여주면서 fetch
                if (todoListManager.gameObject.activeSelf
                    && lastDateClicked == dateStr)
                {
                    todoListManager.HideList();
                    lastDateClicked = null;
                }
                else
                {
                    lastDateClicked = dateStr;
                    todoListManager.ShowList();
                    StartCoroutine(todoListManager.FetchAndShowTasksForDate(dateStr));

                    // 클릭한 날짜로 라벨 업데이트
                    DateTime clickedDate = new DateTime(year, month, dayNumber);
                    UpdateDateLabels(clickedDate);
                }
            });
        }
    }
}