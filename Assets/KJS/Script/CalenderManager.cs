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

    [Header("XR UI 설정")]
    public XRRayInteractor rayInteractor;       // XR 레이 캐스터 (EventSystem에 XRUIInputModule 설정)

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
        // 2) 기존 셀 모두 비활성화하고 풀에 반환
        foreach (Transform child in gridParent)
        {
            child.gameObject.SetActive(false);
            cellPool.Enqueue(child.gameObject);
        }

        // 3) 레이블 갱신
        monthLabel.text = new DateTime(year, month, 1)
                              .ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        // 4) 첫째 날 요일, 한 달 일수 계산
        DateTime firstDay = new DateTime(year, month, 1);
        int startWeekday = (int)firstDay.DayOfWeek;        // 일=0, 월=1...
        int daysInMonth = DateTime.DaysInMonth(year, month);

        // 5) 빈 칸(월 첫 주 앞부분) 채우기
        for (int i = 0; i < startWeekday; i++)
        {
            var blank = GetCell();
            blank.SetActive(true);
        }

        // 6) 날짜 셀 생성/재활용
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

            // (3) 배경 패널(Image) 가져와서 색 바꾸기
            var bg = cell.GetComponent<Image>();
            if (bg != null)
            {
                if (isToday)
                {
                    // 오늘: 보라색, 불투명
                    bg.color = new Color(0.6f, 0.2f, 1f, 1f);
                }
                else
                {
                    // 기본: 투명 (α=0), 컬러는 기존 그대로 유지하거나 흰색으로 설정 가능
                    var c = bg.color;
                    c.a = 0f;
                    bg.color = c;
                }
            }
        }
    }

    // 풀에서 꺼내거나 새로 인스턴스화
    private GameObject GetCell()
    {
        if (cellPool.Count > 0)
        {
            var obj = cellPool.Dequeue();
            obj.transform.SetParent(gridParent, false);
            return obj;
        }
        else
        {
            return Instantiate(dayCellPrefab, gridParent);
        }
    }

    // UI 버튼(또는 XR 클릭)에 연결
    public void OnNextMonth()
    {
        month++;
        if (month > 12) { month = 1; year++; }
        DrawCalendar();
    }

    public void OnPrevMonth()
    {
        month--;
        if (month < 1) { month = 12; year--; }
        DrawCalendar();
    }
}
