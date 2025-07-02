using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Globalization;
using UnityEngine.UI;

public class CalenderManager : MonoBehaviour
{
    [Header("월드-스페이스 UI 참조")]
    public Transform gridParent;                // GridLayoutGroup이 붙은 부모
    public GameObject dayCellPrefab;            // TextMeshProUGUI 포함된 캘린더 셀 프리팹
    public TextMeshProUGUI monthLabel;          // “2025년 06월” 등 표시할 텍스트

    private int year, month;
    private Queue<GameObject> cellPool = new Queue<GameObject>();

    void Start()
    {
        // 1) 시스템 시계로 현재 연·월 초기화
        DateTime now = DateTime.Now;
        year = now.Year;
        month = now.Month;

        DrawCalendar();
    }

    public void DrawCalendar()
    {
        // 1) 셀 풀링 및 초기화
        foreach (Transform child in gridParent)
        {
            child.gameObject.SetActive(false);
            cellPool.Enqueue(child.gameObject);
        }

        // 2) 레이블 갱신 (예: “2025년 07월”)
        monthLabel.text = new DateTime(year, month, 1)
                              .ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        // 3) 첫째 날 요일, 한 달 일수 계산
        DateTime firstDay = new DateTime(year, month, 1);
        int startWeekday = (int)firstDay.DayOfWeek;  // 일=0, 월=1...
        int daysInMonth = DateTime.DaysInMonth(year, month);

        // **이전 달 정보 구하기**
        int prevMonth = month - 1;
        int prevYear = year;
        if (prevMonth < 1) { prevMonth = 12; prevYear--; }
        int daysInPrevMonth = DateTime.DaysInMonth(prevYear, prevMonth);

        // 4) 앞쪽 빈 칸 → 이전 달 날짜로 채우기
        for (int i = 0; i < startWeekday; i++)
        {
            var cell = GetCell();
            cell.SetActive(true);

            // (1) 날짜 텍스트 세팅
            int dayNumber = daysInPrevMonth - startWeekday + i + 1;
            var txt = cell.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = dayNumber.ToString();
            // (2) 텍스트 색: 검정에 alpha 낮춰 희미하게
            txt.color = new Color(0f, 0f, 0f, 0.3f);

            // (3) 배경 투명도도 낮추고 색은 유지
            var bg = cell.GetComponent<Image>();
            if (bg != null)
            {
                var c = bg.color;
                c.a = 0.2f;         // 배경도 약간 보여주고 싶다면 0.2f 등
                bg.color = c;
            }
        }

        // 5) 이번 달 날짜 채우기 (기존 로직 유지)
        for (int day = 1; day <= daysInMonth; day++)
        {
            var cell = GetCell();
            cell.SetActive(true);

            var txt = cell.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = day.ToString();

            bool isToday = (year == DateTime.Now.Year &&
                            month == DateTime.Now.Month &&
                            day == DateTime.Now.Day);

            // (2) 텍스트 색
            txt.color = isToday ? Color.white : Color.black;

            // (3) 배경 패널(Image) 색
            var bg = cell.GetComponent<Image>();
            if (bg != null)
            {
                if (isToday)
                {
                    bg.color = new Color(0.6f, 0.2f, 1f, 1f);
                }
                else
                {
                    var c = bg.color;
                    c.a = 0f;
                    bg.color = c;
                }
            }
        }

        // 6) 뒷쪽 빈 칸 → 다음 달 날짜로 채우기 (6행 고정 등 원하는 방식으로)
        int totalCells = startWeekday + daysInMonth;
        int rows = Mathf.CeilToInt(totalCells / 7f);
        int slots = rows * 7;
        int nextMonthDays = slots - totalCells;

        for (int i = 1; i <= nextMonthDays; i++)
        {
            var cell = GetCell();
            cell.SetActive(true);

            var txt = cell.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = i.ToString();
            txt.color = new Color(0f, 0f, 0f, 0.3f);

            var bg = cell.GetComponent<Image>();
            if (bg != null)
            {
                var c = bg.color;
                c.a = 0.2f;
                bg.color = c;
            }
        }
    }

    // 풀에서 꺼내거나 새로 인스턴스화
    private GameObject GetCell()
    {
        GameObject cell;
        if (cellPool.Count > 0)
        {
            cell = cellPool.Dequeue();
            cell.transform.SetParent(gridParent, false);
        }
        else
        {
            cell = Instantiate(dayCellPrefab, gridParent);
        }

        //── RectTransform 정보 가져오기 ──
        var rt = cell.GetComponent<RectTransform>();
        //── BoxCollider 동기화 ──
        var bc = cell.GetComponent<BoxCollider>();
        if (bc == null) bc = cell.AddComponent<BoxCollider>();
        // Z 두께는 아주 얇게, UI는 Z=0 평면이므로 0.01f 정도면 충분
        bc.size = new Vector3(rt.rect.width, rt.rect.height, 0.01f);
        // Collider 중심을 RectTransform 중심(0,0)으로
        bc.center = Vector3.zero;

        return cell;
    }

}
