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

    [Header("���Ϻ� Ʈ���� �̺�Ʈ")]
    public UnityEvent onMonday;
    public UnityEvent onTuesday;
    public UnityEvent onWednesday;
    public UnityEvent onThursday;
    public UnityEvent onFriday;
    public UnityEvent onSaturday;
    public UnityEvent onSunday;

    // ���������� �̺�Ʈ�� ������ ��¥ (�ߺ� ���� ������)
    private DateTime lastTriggerDate = DateTime.MinValue.Date;

    private void Start()
    {
        if (uiText == null)
        {
            Debug.LogError("UI TextMeshProUGUI�� �Ҵ���� �ʾҽ��ϴ�!");
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
        // ȭ�鿡 ��¥�����ϡ��ð� ǥ��
        string date = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string weekday = now.ToString("dddd", CultureInfo.CreateSpecificCulture("ko-KR"));
        string time = now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        uiText.text = $"��¥: {date} ({weekday})\n�ð�: {time}";

        // �Ϸ翡 �� ���� ���� ���� �̺�Ʈ ȣ��
        if (now.Date != lastTriggerDate)
        {
            TriggerByWeekday(now.DayOfWeek);
            lastTriggerDate = now.Date;
        }
    }

    // DayOfWeek�� ���� UnityEvent ȣ��
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
