using UnityEngine.UI;             // Unity 내장 Text 사용 시
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Globalization;    // 요일 로컬라이즈용

public class DateTimeDisplay : MonoBehaviour
{
    [Header("Assign UI TextMeshProUGUI component")]
    public TextMeshProUGUI uiText;

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
        // 1초마다 갱신
        while (true)
        {
            UpdateTime();
            yield return new WaitForSeconds(1f);
        }
    }

    private void UpdateTime()
    {
        DateTime now = DateTime.Now;
        // 날짜
        string date = now.ToString("yyyy-MM-dd");             // ex: 2025-06-17
        // 요일 (한국어)
        string weekday = now.ToString("dddd", CultureInfo.CreateSpecificCulture("ko-KR"));
        // 시간
        string time = now.ToString("HH:mm:ss");               // ex: 14:23:45

        uiText.text = $"날짜: {date} ({weekday})\n시간: {time}";
    }
}


