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

        // 시간과 분만 "HH:mm" 형식으로 가져옴 (예: "14:23")
        string time = now.ToString("HH:mm");

        uiText.text = time;
    }
}


