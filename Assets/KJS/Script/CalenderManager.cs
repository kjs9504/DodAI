using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

public class CalenderManager : MonoBehaviour
{
    [Header("월드-스페이스 UI 참조")]
    public Transform gridParent;         // GridLayoutGroup이 붙은 부모
    public GameObject dayCellPrefab;     // TextMeshProUGUI 포함된 캘린더 셀 프리팹
    public TextMeshProUGUI monthLabel;   // “2025년 07월” 등 표시할 텍스트

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
        // 1) 기존 셀 초기화
        foreach (Transform child in gridParent)
        {
            child.gameObject.SetActive(false);
            cellPool.Enqueue(child.gameObject);
        }

        // 2) 레이블 갱신
        monthLabel.text = new DateTime(year, month, 1)
                              .ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        // 3) 첫째 요일·일수, 이전·다음 달 정보
        DateTime firstDay = new DateTime(year, month, 1);
        int startWeekday = (int)firstDay.DayOfWeek;        // 일=0, 월=1...
        int daysInMonth = DateTime.DaysInMonth(year, month);

        int prevMonth = month - 1, prevYear = year;
        if (prevMonth < 1) { prevMonth = 12; prevYear--; }
        int daysInPrevMonth = DateTime.DaysInMonth(prevYear, prevMonth);

        // 4) 지난 달 날짜 채우기
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

        // Text 세팅
        var txt = cell.GetComponentInChildren<TextMeshProUGUI>();
        txt.text = dayNumber.ToString();

        // Background(Image) 세팅
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
                    // 흰색 배경, 완전 불투명
                    bg.color = new Color32(255, 255, 255, 255);
                }
                break;

            case DayType.Today:
                txt.color = Color.white;
                if (bg != null) bg.color = new Color(0.6f, 0.2f, 1f, 1f);
                break;
        }
    }
}
