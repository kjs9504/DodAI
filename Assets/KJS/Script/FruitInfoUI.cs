using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Globalization;
using System;
using Oculus.Interaction;

[RequireComponent(typeof(Collider))] // Ŭ���� �������� Collider �ʿ�
public class FruitInfoUI : MonoBehaviour, IPointerClickHandler
{
    [Header("�� �� ������Ʈ�� Task ������ (FruitManager ���� Initialize �� �� ��)")]
    public long id;
    public string todo;
    public string date;
    public string time;
    public string acceptedAt;
    public long? userId;

    [Header("�� Ŭ�� �� ������ UI �г�")]
    [Tooltip("Inspector ���� �Ҵ��ؾ� �մϴ�.")]
    public GameObject infoPanel;

    [Header("�� UI �г� ���� �ؽ�Ʈ��")]
    public TextMeshProUGUI todoText;
    public TextMeshProUGUI userIdText;

    [Header("감정 선택 컨트롤러")]
    public EmojiController emojiController;

    private void Awake()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
        
        // 이 FruitInfoUI를 emojiController에 등록
        if (emojiController != null)
            emojiController.SetCurrentFruitInfoUI(this);
    }

    public TextMeshProUGUI billboardText;

    /// <summary>
    /// FruitManager ���� ������ �����͸� ������ �� �� ȣ���ϼ���.
    /// </summary>
    public void Initialize(AcceptedTaskData data)
    {
        id = data.id;
        todo = data.todo;
        date = data.date;
        time = data.time;
        acceptedAt = data.acceptedAt;
        userId = data.userId;

        if (billboardText != null)
        {
            // "2025-07-04" 문자열에서 날짜 파싱
            var dt = DateTime.ParseExact(data.date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            billboardText.text = $"{dt.Month}월 {dt.Day}일의 열매";
        }

        // 데이터 초기화 후 UI 업데이트
        UpdateUI();
    }

    /// <summary>
    /// Ŭ���Ǹ� UI �г��� ����ϸ鼭, �ؽ�Ʈ�� �����մϴ�.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (infoPanel == null) return;

        bool now = !infoPanel.activeSelf;
        infoPanel.SetActive(now);

        // billboardText 토글 (infoPanel과 반대로)
        if (billboardText != null)
            billboardText.gameObject.SetActive(!now);

        if (now)
            UpdateUI();
    }

    private void UpdateUI()
    {
        // 날짜와 시간 파싱
        var dt = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var timeSpan = TimeSpan.Parse(time);
        
        if (todoText != null) todoText.text = $"{dt.Month}월{dt.Day}일 {timeSpan.Hours}시 {timeSpan.Minutes}분 {todo}";
        if (userIdText != null) userIdText.text = userId.HasValue
                                        ? $"User ID: {userId.Value}"
                                        : "User ID: (none)";
    }
}

