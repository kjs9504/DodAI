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

        // �ð��� �и� "HH:mm" �������� ������ (��: "14:23")
        string time = now.ToString("HH:mm");

        uiText.text = time;
    }
}


