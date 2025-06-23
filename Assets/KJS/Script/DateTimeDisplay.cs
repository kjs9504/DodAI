using UnityEngine.UI;             // Unity ���� Text ��� ��
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Globalization;    // ���� ���ö������

public class DateTimeDisplay : MonoBehaviour
{
    [Header("Assign UI TextMeshProUGUI component")]
    public TextMeshProUGUI uiText;

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
        // 1�ʸ��� ����
        while (true)
        {
            UpdateTime();
            yield return new WaitForSeconds(1f);
        }
    }

    private void UpdateTime()
    {
        DateTime now = DateTime.Now;
        // ��¥
        string date = now.ToString("yyyy-MM-dd");             // ex: 2025-06-17
        // ���� (�ѱ���)
        string weekday = now.ToString("dddd", CultureInfo.CreateSpecificCulture("ko-KR"));
        // �ð�
        string time = now.ToString("HH:mm:ss");               // ex: 14:23:45

        uiText.text = $"��¥: {date} ({weekday})\n�ð�: {time}";
    }
}


