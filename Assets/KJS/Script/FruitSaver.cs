using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;


// 감정 데이터 DTO (FruitEmotionData) 정의는 그대로 사용
// [System.Serializable]
// public class FruitEmotionData { ... }

public class FruitSaver : MonoBehaviour
{
    [Header("서버 기본 URL (포트까지)")]
    public string serverBaseUrl = "http://localhost:8080";

    [Header("API 엔드포인트 경로")]
    public string apiEndpoint = "/api/fruit"; // 서버 API에 맞게 설정

    public EmojiController emojiController; // Inspector에서 연결
    public FruitInfoUI fruitInfoUI; // Inspector에서 연결

    public void SaveCurrentFruit()
    {
        if (fruitInfoUI == null)
        {
            Debug.LogWarning("FruitInfoUI가 연결되어 있지 않습니다!");
            return;
        }

        // FruitInfoUI에서 값 읽어서 새로 DTO 생성
        var data = new FruitEmotionData
        {
            emotion = fruitInfoUI.currentEmotion ?? "",
            todo = fruitInfoUI.todo,
            date = fruitInfoUI.date,
            time = fruitInfoUI.time,
            position = new Position
            {
                x = fruitInfoUI.transform.position.x,
                y = fruitInfoUI.transform.position.y,
                z = fruitInfoUI.transform.position.z
            },
            acceptedAt = fruitInfoUI.acceptedAt,
            createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        SaveFruitEmotion(data);
    }

    public void SaveFruitEmotion(FruitEmotionData data)
    {
        // 각 필드별 값 확인
        Debug.Log($"[FruitSaver] emotion: {data.emotion}");
        Debug.Log($"[FruitSaver] todo: {data.todo}");
        Debug.Log($"[FruitSaver] date: {data.date}");
        Debug.Log($"[FruitSaver] time: {data.time}");
        Debug.Log($"[FruitSaver] position: x={data.position.x}, y={data.position.y}, z={data.position.z}");
        Debug.Log($"[FruitSaver] acceptedAt: {data.acceptedAt}");
        Debug.Log($"[FruitSaver] createdAt: {data.createdAt}");
        
        string json = JsonUtility.ToJson(data);
        Debug.Log("[FruitSaver] POST FruitEmotionData: " + json);
        Debug.Log($"[FruitSaver] JSON 길이: {json.Length}");
        Debug.Log($"[FruitSaver] JSON 바이트: {System.Text.Encoding.UTF8.GetByteCount(json)}");
        
        // JSON 형식 검증
        Debug.Log($"[FruitSaver] JSON이 null인가: {json == null}");
        Debug.Log($"[FruitSaver] JSON이 비어있는가: {string.IsNullOrEmpty(json)}");
        
        StartCoroutine(PostFruitEmotion(json));
    }

    private IEnumerator PostFruitEmotion(string json)
    {
        // 서버 API에 맞게 POST /api/fruit로 요청
        string url = $"{serverBaseUrl}{apiEndpoint}";
        Debug.Log($"[FruitSaver] 요청 URL: {url}");
        Debug.Log($"[FruitSaver] 요청 메서드: POST");
        Debug.Log($"[FruitSaver] 요청 데이터: {json}");

        using (var req = new UnityWebRequest(url, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" };
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept", "application/json");

            yield return req.SendWebRequest();

            Debug.Log($"[FruitSaver] 응답 코드: {req.responseCode}");
            Debug.Log($"[FruitSaver] 응답 헤더: {req.GetResponseHeader("Allow")}");

            if (req.result == UnityWebRequest.Result.Success || req.responseCode == 201)
            {
                Debug.Log("[FruitSaver] 감정 저장 성공: " + req.responseCode);
                Debug.Log("[FruitSaver] 응답 내용: " + req.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"[FruitSaver] 감정 저장 실패: {req.error} ({req.responseCode})");
                Debug.LogError($"[FruitSaver] 응답 내용: {req.downloadHandler.text}");
            }
        }
    }
}