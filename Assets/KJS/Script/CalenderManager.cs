using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Globalization;
using UnityEngine.UI;

public class CalenderManager : MonoBehaviour
{
    [Header("����-�����̽� UI ����")]
    public Transform gridParent;                // GridLayoutGroup�� ���� �θ�
    public GameObject dayCellPrefab;            // TextMeshProUGUI ���Ե� Ķ���� �� ������
    public TextMeshProUGUI monthLabel;          // ��2025�� 06���� �� ǥ���� �ؽ�Ʈ

    [Header("XR UI ����")]
    public XRRayInteractor rayInteractor;       // XR ���� ĳ���� (EventSystem�� XRUIInputModule ����)

    private int year, month;
    private Queue<GameObject> cellPool = new Queue<GameObject>();

    void Start()
    {
        // 1) �ý��� �ð�� ���� ������ �ʱ�ȭ
        DateTime now = DateTime.Now;
        year = now.Year;
        month = now.Month;

        DrawCalendar();
    }

    public void DrawCalendar()
    {
        // 2) ���� �� ��� ��Ȱ��ȭ�ϰ� Ǯ�� ��ȯ
        foreach (Transform child in gridParent)
        {
            child.gameObject.SetActive(false);
            cellPool.Enqueue(child.gameObject);
        }

        // 3) ���̺� ����
        monthLabel.text = new DateTime(year, month, 1)
                              .ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        // 4) ù° �� ����, �� �� �ϼ� ���
        DateTime firstDay = new DateTime(year, month, 1);
        int startWeekday = (int)firstDay.DayOfWeek;        // ��=0, ��=1...
        int daysInMonth = DateTime.DaysInMonth(year, month);

        // 5) �� ĭ(�� ù �� �պκ�) ä���
        for (int i = 0; i < startWeekday; i++)
        {
            var blank = GetCell();
            blank.SetActive(true);
        }

        // 6) ��¥ �� ����/��Ȱ��
        for (int day = 1; day <= daysInMonth; day++)
        {
            var cell = GetCell();
            cell.SetActive(true);

            var txt = cell.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = day.ToString();

            bool isToday = (year == DateTime.Now.Year &&
                        month == DateTime.Now.Month &&
                        day == DateTime.Now.Day);

            // (2) �ؽ�Ʈ ��
            txt.color = isToday ? Color.white : Color.black;

            // (3) ��� �г�(Image) �����ͼ� �� �ٲٱ�
            var bg = cell.GetComponent<Image>();
            if (bg != null)
            {
                if (isToday)
                {
                    // ����: �����, ������
                    bg.color = new Color(0.6f, 0.2f, 1f, 1f);
                }
                else
                {
                    // �⺻: ���� (��=0), �÷��� ���� �״�� �����ϰų� ������� ���� ����
                    var c = bg.color;
                    c.a = 0f;
                    bg.color = c;
                }
            }
        }
    }

    // Ǯ���� �����ų� ���� �ν��Ͻ�ȭ
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

    // UI ��ư(�Ǵ� XR Ŭ��)�� ����
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
