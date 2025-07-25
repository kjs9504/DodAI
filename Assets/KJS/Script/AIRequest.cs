using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;

public class AIRequester : MonoBehaviour
{
    [Header("요청 설정")]
    public string requestUrl = "http://localhost:8080/api/ai"; // 요청을 보낼 URL
    public string requestMethod = "POST"; // HTTP 메서드 (GET, POST, PUT, DELETE)
    
    [Header("시간 설정")]
    public int days = 0;
    public int hours = 0;
    public int minutes = 0;
    public int seconds = 10;
    [Header("세션 간에도 유지하려면 PlayerPrefs 사용")]
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
        Debug.Log($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] AI 요청 시작 - URL: {requestUrl}");
        StartCoroutine(SendRequestCoroutine());
    }
    
    private IEnumerator SendRequestCoroutine()
    {
        using (UnityWebRequest request = new UnityWebRequest(requestUrl, requestMethod))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            
            // POST 요청인 경우 기본 데이터 추가 (필요시 수정)
            if (requestMethod.ToUpper() == "POST")
            {
                string jsonData = "{\"timestamp\":\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\"}";
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", "application/json");
            }
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] AI 요청 성공: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] AI 요청 실패: {request.error} (응답코드: {request.responseCode})");
            }
        }
    }
}

