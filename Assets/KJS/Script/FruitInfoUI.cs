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

    public string currentEmotion; // 현재 선택된 감정

    public void SetEmotion(string emotion)
    {
        currentEmotion = emotion;
        Debug.Log($"[FruitInfoUI] currentEmotion 갱신: {currentEmotion}");
    }

    /// <summary>
    /// 이 과일의 감정을 설정 (저장은 별도 버튼으로)
    /// </summary>
    public void SetEmotionOnly(string emotion)
    {
        SetEmotion(emotion);
        Debug.Log($"[FruitInfoUI] 감정 '{emotion}' 설정 완료. 저장하려면 저장 버튼을 눌러주세요.");
    }

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

       // 디버그 로그 추가
        Debug.Log($"[FruitInfoUI] Initialize - id={id}, todo={todo}, date={date}, time={time}, acceptedAt={acceptedAt}, userId={userId}");

        if (billboardText != null)
        {
            var dt = DateTime.ParseExact(data.date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            billboardText.text = $"{dt.Month}월 {dt.Day}일의 열매";
        }

        UpdateUI();
    }

    /// <summary>
    /// ŬǸ UIг ϸ鼭, ؽƮ մϴ.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[FruitInfoUI] OnPointerClick 호출됨!");
        if (infoPanel == null) return;

        bool now = !infoPanel.activeSelf;
        infoPanel.SetActive(now);

        // billboardText 토글 (infoPanel과 반대로)
        if (billboardText != null)
            billboardText.gameObject.SetActive(!now);

        if (now)
            UpdateUI();

        // 과일 클릭 시 FruitSaver의 fruitInfoUI를 자신(this)으로 갱신
        var saver = FindObjectOfType<FruitSaver>();
        if (saver != null)
        {
            saver.fruitInfoUI = this;
            Debug.Log($"[FruitInfoUI] FruitSaver의 fruitInfoUI를 {this.gameObject.name}으로 갱신");
        }
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

