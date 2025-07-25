using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// 감정 데이터 DTO (FruitEmotionData) 정의는 그대로 사용
// [System.Serializable]
// public class FruitEmotionData { ... }

public class FruitSaver : MonoBehaviour
{
    [Header("서버 기본 URL (포트까지)")]
    public string serverBaseUrl = "http://192.168.0.58:8080";

    [Header("API 엔드포인트 경로")]
    public string apiEndpoint = "/api/fruit"; // 서버 API에 맞게 설정

    [Header("전체 저장 설정")]
    public bool saveAllFruitsOnButtonPress = true; // 버튼 클릭 시 전체 저장 여부

    public EmojiController emojiController; // Inspector에서 연결 (개별 저장용)
    public FruitInfoUI fruitInfoUI; // Inspector에서 연결 (개별 저장용)

    /// <summary>
    /// 씬의 모든 과일을 찾아서 저장
    /// </summary>
    public void SaveAllFruitsInScene()
    {
        Debug.Log("[FruitSaver] 씬의 모든 과일 저장 시작");
        
        // 씬에서 모든 FruitInfoUI 컴포넌트를 찾기
        var allFruitInfoUIs = FindObjectsOfType<FruitInfoUI>();
        Debug.Log($"[FruitSaver] 발견된 과일 개수: {allFruitInfoUIs.Length}");
        
        if (allFruitInfoUIs.Length == 0)
        {
            Debug.LogWarning("[FruitSaver] 저장할 과일이 없습니다.");
            return;
        }

        // 각 과일의 데이터를 수집
        List<FruitEmotionData> allFruitData = new List<FruitEmotionData>();
        
        foreach (var fruitInfoUI in allFruitInfoUIs)
        {
            if (fruitInfoUI == null) continue;
            
            var fruitData = new FruitEmotionData
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
            
            allFruitData.Add(fruitData);
            
            Debug.Log($"[FruitSaver] 과일 데이터 수집: ID={fruitInfoUI.id}, 감정={fruitData.emotion}, 할일={fruitData.todo}");
        }
        
        // 전체 데이터를 JSON으로 변환하여 저장
        SaveAllFruitEmotions(allFruitData);
    }

    /// <summary>
    /// 여러 과일 데이터를 한 번에 저장
    /// </summary>
    public void SaveAllFruitEmotions(List<FruitEmotionData> fruitDataList)
    {
        if (fruitDataList == null || fruitDataList.Count == 0)
        {
            Debug.LogWarning("[FruitSaver] 저장할 과일 데이터가 없습니다.");
            return;
        }

        // 올바른 JSON 형태로 변환
        string json = CreateFruitsJson(fruitDataList);
        
        Debug.Log($"[FruitSaver] 전체 과일 저장 - 개수: {fruitDataList.Count}");
        Debug.Log($"[FruitSaver] JSON 데이터:\n{json}");
        
        StartCoroutine(PostAllFruitEmotions(json));
    }

    /// <summary>
    /// fruits 배열 형태의 JSON 생성
    /// </summary>
    private string CreateFruitsJson(List<FruitEmotionData> fruitDataList)
    {
        // 간단한 JSON 문자열 생성
        var jsonBuilder = new System.Text.StringBuilder();
        jsonBuilder.Append("{\n  \"fruits\": [\n");
        
        for (int i = 0; i < fruitDataList.Count; i++)
        {
            var fruit = fruitDataList[i];
            jsonBuilder.Append("    {\n");
            jsonBuilder.Append($"      \"emotion\": \"{fruit.emotion}\",\n");
            jsonBuilder.Append($"      \"todo\": \"{fruit.todo}\",\n");
            jsonBuilder.Append($"      \"date\": \"{fruit.date}\"\n");
            
            if (i < fruitDataList.Count - 1)
                jsonBuilder.Append("    },\n");
            else
                jsonBuilder.Append("    }\n");
        }
        
        jsonBuilder.Append("  ]\n}");
        return jsonBuilder.ToString();
    }

    /// <summary>
    /// 전체 과일 데이터를 서버로 전송
    /// </summary>
    private IEnumerator PostAllFruitEmotions(string json)
    {
        string url = $"{serverBaseUrl}{apiEndpoint}/bulk"; // bulk 엔드포인트 사용
        Debug.Log($"[FruitSaver] 전체 저장 요청 URL: {url}");
        Debug.Log($"[FruitSaver] 요청 메서드: POST");
        Debug.Log($"[FruitSaver] 요청 데이터 길이: {json.Length}");

        using (var req = new UnityWebRequest(url, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" };
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept", "application/json");

            yield return req.SendWebRequest();

            Debug.Log($"[FruitSaver] 응답 코드: {req.responseCode}");

            if (req.result == UnityWebRequest.Result.Success || req.responseCode == 201)
            {
                Debug.Log($"[FruitSaver] 전체 과일 저장 성공: {req.responseCode}");
                Debug.Log($"[FruitSaver] 응답 내용: {req.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"[FruitSaver] 전체 과일 저장 실패: {req.error} ({req.responseCode})");
                Debug.LogError($"[FruitSaver] 응답 내용: {req.downloadHandler.text}");
            }
        }
    }

    /// <summary>
    /// 개별 과일 저장 (기존 방식 유지)
    /// </summary>
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

    /// <summary>
    /// 개별 과일 감정 저장 (기존 방식 유지)
    /// </summary>
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

    /// <summary>
    /// 개별 과일 감정을 서버로 전송 (기존 방식 유지)
    /// </summary>
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

    /// <summary>
    /// Inspector에서 버튼으로 호출할 수 있는 메서드
    /// </summary>
    [ContextMenu("전체 과일 저장")]
    public void SaveAllFruitsButton()
    {
        SaveAllFruitsInScene();
    }

    /// <summary>
    /// 특정 과일만 저장 (ID로 찾기)
    /// </summary>
    public void SaveFruitById(long fruitId)
    {
        var fruitInfoUI = FindObjectsOfType<FruitInfoUI>()
            .FirstOrDefault(f => f.id == fruitId);
            
        if (fruitInfoUI != null)
        {
            fruitInfoUI.SetEmotion(fruitInfoUI.currentEmotion); // 현재 감정 유지
            SaveCurrentFruit();
        }
        else
        {
            Debug.LogWarning($"[FruitSaver] ID {fruitId}인 과일을 찾을 수 없습니다.");
        }
    }
}