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
            // date가 유효한지 확인 후 파싱
            if (!string.IsNullOrEmpty(data.date))
            {
                try
                {
                    var dt = DateTime.ParseExact(data.date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    billboardText.text = $"{dt.Month}월 {dt.Day}일의 열매";
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[FruitInfoUI] 날짜 파싱 실패: {data.date}, 오류: {e.Message}");
                    billboardText.text = "열매";
                }
            }
            else
            {
                billboardText.text = "열매";
            }
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
        Debug.Log($"[FruitInfoUI] UpdateUI() 호출됨 - todo: '{todo}', date: '{date}', time: '{time}'");
        
        // 날짜와 시간 파싱 (안전하게 처리)
        DateTime dt = DateTime.Now; // 기본값
        TimeSpan timeSpan = TimeSpan.Zero; // 기본값
        
        if (!string.IsNullOrEmpty(date))
        {
            try
            {
                dt = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FruitInfoUI] UpdateUI - 날짜 파싱 실패: {date}, 오류: {e.Message}");
            }
        }
        
        if (!string.IsNullOrEmpty(time))
        {
            try
            {
                timeSpan = TimeSpan.Parse(time);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FruitInfoUI] UpdateUI - 시간 파싱 실패: {time}, 오류: {e.Message}");
            }
        }
        
        if (todoText != null) 
        {
            string finalText;
            if (!string.IsNullOrEmpty(date) && !string.IsNullOrEmpty(time))
            {
                finalText = $"{dt.Month}월{dt.Day}일 {timeSpan.Hours}시 {timeSpan.Minutes}분 {todo}";
            }
            else
            {
                finalText = todo;
            }
            
            todoText.text = finalText;
            Debug.Log($"[FruitInfoUI] todoText 설정: '{finalText}'");
        }
        else
        {
            Debug.LogWarning("[FruitInfoUI] todoText가 null입니다!");
        }
        
        if (userIdText != null) 
        {
            string userIdString = userId.HasValue ? $"User ID: {userId.Value}" : "User ID: (none)";
            userIdText.text = userIdString;
            Debug.Log($"[FruitInfoUI] userIdText 설정: '{userIdString}'");
        }
        else
        {
            Debug.LogWarning("[FruitInfoUI] userIdText가 null입니다!");
        }
    }
}

