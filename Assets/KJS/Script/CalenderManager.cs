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
        // 1) �� Ǯ�� �� �ʱ�ȭ
        foreach (Transform child in gridParent)
        {
            child.gameObject.SetActive(false);
            cellPool.Enqueue(child.gameObject);
        }

        // 2) ���̺� ���� (��: ��2025�� 07����)
        monthLabel.text = new DateTime(year, month, 1)
                              .ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        // 3) ù° �� ����, �� �� �ϼ� ���
        DateTime firstDay = new DateTime(year, month, 1);
        int startWeekday = (int)firstDay.DayOfWeek;  // ��=0, ��=1...
        int daysInMonth = DateTime.DaysInMonth(year, month);

        // **���� �� ���� ���ϱ�**
        int prevMonth = month - 1;
        int prevYear = year;
        if (prevMonth < 1) { prevMonth = 12; prevYear--; }
        int daysInPrevMonth = DateTime.DaysInMonth(prevYear, prevMonth);

        // 4) ���� �� ĭ �� ���� �� ��¥�� ä���
        for (int i = 0; i < startWeekday; i++)
        {
            var cell = GetCell();
            cell.SetActive(true);

            // (1) ��¥ �ؽ�Ʈ ����
            int dayNumber = daysInPrevMonth - startWeekday + i + 1;
            var txt = cell.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = dayNumber.ToString();
            // (2) �ؽ�Ʈ ��: ������ alpha ���� ����ϰ�
            txt.color = new Color(0f, 0f, 0f, 0.3f);

            // (3) ��� ������ ���߰� ���� ����
            var bg = cell.GetComponent<Image>();
            if (bg != null)
            {
                var c = bg.color;
                c.a = 0.2f;         // ��浵 �ణ �����ְ� �ʹٸ� 0.2f ��
                bg.color = c;
            }
        }

        // 5) �̹� �� ��¥ ä��� (���� ���� ����)
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

            // (3) ��� �г�(Image) ��
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

        // 6) ���� �� ĭ �� ���� �� ��¥�� ä��� (6�� ���� �� ���ϴ� �������)
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

    // Ǯ���� �����ų� ���� �ν��Ͻ�ȭ
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

        //���� RectTransform ���� �������� ����
        var rt = cell.GetComponent<RectTransform>();
        //���� BoxCollider ����ȭ ����
        var bc = cell.GetComponent<BoxCollider>();
        if (bc == null) bc = cell.AddComponent<BoxCollider>();
        // Z �β��� ���� ���, UI�� Z=0 ����̹Ƿ� 0.01f ������ ���
        bc.size = new Vector3(rt.rect.width, rt.rect.height, 0.01f);
        // Collider �߽��� RectTransform �߽�(0,0)����
        bc.center = Vector3.zero;

        return cell;
    }

}
