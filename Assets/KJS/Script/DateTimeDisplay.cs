using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Globalization;

public class DateTimeDisplay : MonoBehaviour
{
    [Header("Rich Text 활성화된 TMP 하나")]
    public TextMeshProUGUI uiText;

    [Header("시 크기")] public int hourSize = 200;
    [Header("분 크기")] public int minuteSize = 120;
    [Header("AMPM 크기")] public int ampmSize = 100;

    // em‑space (\u2003) / en‑space (\u2002) 등으로 간격 조정
    private const string spacer = "\u2003";

    void Start()
    {
        uiText.richText = true;
        StartCoroutine(UpdateTime());
    }

    private IEnumerator UpdateTime()
    {
        while (true)
        {
            DateTime now = DateTime.Now;
            string hh = now.ToString("hh", CultureInfo.InvariantCulture);
            string mm = now.ToString("mm", CultureInfo.InvariantCulture);
            string tt = now.ToString("tt", CultureInfo.InvariantCulture);

            uiText.text =
                $"<size={hourSize}>{hh}</size>" +
                $"<size={minuteSize}>:{mm}</size>" +
                spacer +
                $"<size={ampmSize}>{tt}</size>";

            yield return new WaitForSeconds(1f);
        }
    }
}




