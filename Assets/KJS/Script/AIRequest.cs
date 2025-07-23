using UnityEngine;
using System;

public class AIRequester : MonoBehaviour
{
    [Header("인터벌 설정")]
    public int days = 0;
    public int hours = 0;
    public int minutes = 0;
    public int seconds = 10;
    [Header("앱 재시작 후에도 유지하려면 PlayerPrefs 사용")]
    public bool persistBetweenSessions = false;

    private TimeSpan _interval;
    private DateTime _nextRequestTime;

    void Start()
    {
        _interval = new TimeSpan(days, hours, minutes, seconds);

        if (persistBetweenSessions && PlayerPrefs.HasKey("NextRequestTime"))
        {
            long ticks = Convert.ToInt64(PlayerPrefs.GetString("NextRequestTime"));
            _nextRequestTime = new DateTime(ticks);
        }
        else
        {
            _nextRequestTime = DateTime.Now + _interval;
            if (persistBetweenSessions)
                PlayerPrefs.SetString("NextRequestTime", _nextRequestTime.Ticks.ToString());
        }
    }

    void Update()
    {
        if (DateTime.Now >= _nextRequestTime)
        {
            SendAIRequest();

            _nextRequestTime = DateTime.Now + _interval;
            if (persistBetweenSessions)
                PlayerPrefs.SetString("NextRequestTime", _nextRequestTime.Ticks.ToString());
        }
    }

    void SendAIRequest()
    {
        Debug.Log($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] AI 요청 전송");
        // AI 요청 로직
    }
}

