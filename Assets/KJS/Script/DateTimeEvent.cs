using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System;
using System.Collections;
using System.Globalization;

public class DateTimeEvent : MonoBehaviour
{
    [Header("Assign UI TextMeshProUGUI component")]
    public TextMeshProUGUI uiText;

    [Header("요일별 트리거 이벤트")]
    public UnityEvent onMonday;
    public UnityEvent onTuesday;
    public UnityEvent onWednesday;
    public UnityEvent onThursday;
    public UnityEvent onFriday;
    public UnityEvent onSaturday;
    public UnityEvent onSunday;

    // 마지막으로 이벤트를 실행한 날짜 (중복 실행 방지용)
    private DateTime lastTriggerDate = DateTime.MinValue.Date;

    private void Start()
    {
        if (uiText == null)
        {
            Debug.LogError("UI TextMeshProUGUI가 할당되지 않았습니다!");
            enabled = false;
            return;
        }
        StartCoroutine(UpdateTimeCoroutine());
    }

    private IEnumerator UpdateTimeCoroutine()
    {
        while (true)
        {
            UpdateTimeAndTrigger();
            yield return new WaitForSeconds(1f);
        }
    }

    private void UpdateTimeAndTrigger()
    {
        DateTime now = DateTime.Now;
        // 화면에 날짜·요일·시간 표시
        string date = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string weekday = now.ToString("dddd", CultureInfo.CreateSpecificCulture("ko-KR"));
        string time = now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        uiText.text = $"날짜: {date} ({weekday})\n시간: {time}";

        // 하루에 한 번만 같은 요일 이벤트 호출
        if (now.Date != lastTriggerDate)
        {
            TriggerByWeekday(now.DayOfWeek);
            lastTriggerDate = now.Date;
        }
    }

    // DayOfWeek에 따라 UnityEvent 호출
    private void TriggerByWeekday(DayOfWeek day)
    {
        switch (day)
        {
            case DayOfWeek.Monday: onMonday?.Invoke(); break;
            case DayOfWeek.Tuesday: onTuesday?.Invoke(); break;
            case DayOfWeek.Wednesday: onWednesday?.Invoke(); break;
            case DayOfWeek.Thursday: onThursday?.Invoke(); break;
            case DayOfWeek.Friday: onFriday?.Invoke(); break;
            case DayOfWeek.Saturday: onSaturday?.Invoke(); break;
            case DayOfWeek.Sunday: onSunday?.Invoke(); break;
        }
    }
}
