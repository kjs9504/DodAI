using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

[Serializable]
public class SnapData
{
    public string snapId;           // 각 Snap의 고유 ID
    public string snapName;         // Snap의 이름
    public Vector3 position;        // Snap의 위치
    public string attachedFruitId;  // 연결된 Fruit의 ID (없으면 null)
    public string emotion;          // 연결된 감정 (없으면 null)
    public string todo;             // 연결된 할일 내용 (없으면 null)
    public string date;             // 연결된 날짜 (없으면 null)
    public string time;             // 연결된 시간 (없으면 null)
    public string createdAt;        // 생성 시간
    public string updatedAt;        // 업데이트 시간
}

[Serializable]
public class SnapDataList
{
    public List<SnapData> snaps;
}

public class SnapDataManager : MonoBehaviour
{
    [Header("백엔드 설정")]
    public string snapDataUrl = "http://localhost:8080/api/snaps";
    
    [Header("Snap 오브젝트들")]
    public List<SnapData> snapDataList = new List<SnapData>();
    
    private Dictionary<string, SnapData> snapDataDict = new Dictionary<string, SnapData>();
    
    void Start()
    {
        // 기존 Snap 데이터 로드
        StartCoroutine(LoadSnapData());
    }
    
    /// <summary>
    /// 새로운 Snap 등록
    /// </summary>
    public void RegisterSnap(string snapId, string snapName, Vector3 position)
    {
        var snapData = new SnapData
        {
            snapId = snapId,
            snapName = snapName,
            position = position,
            createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        snapDataList.Add(snapData);
        snapDataDict[snapId] = snapData;
        
        Debug.Log($"새로운 Snap 등록: {snapId} - {snapName}");
    }
    
    /// <summary>
    /// Snap에 Fruit 데이터 연결
    /// </summary>
    public void AttachFruitToSnap(string snapId, FruitEmotionData fruitData)
    {
        if (!snapDataDict.ContainsKey(snapId))
        {
            Debug.LogWarning($"Snap ID {snapId}를 찾을 수 없습니다.");
            return;
        }
        
        var snapData = snapDataDict[snapId];
        snapData.attachedFruitId = fruitData.taskId.ToString();
        snapData.emotion = fruitData.emotion;
        snapData.todo = fruitData.todo;
        snapData.date = fruitData.date;
        snapData.time = fruitData.time;
        snapData.updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        Debug.Log($"Fruit을 Snap에 연결: {snapId} - {fruitData.todo}");
        
        // 백엔드에 저장
        StartCoroutine(SaveSnapData(snapData));
    }
    
    /// <summary>
    /// Snap에서 Fruit 데이터 분리
    /// </summary>
    public void DetachFruitFromSnap(string snapId)
    {
        if (!snapDataDict.ContainsKey(snapId))
        {
            Debug.LogWarning($"Snap ID {snapId}를 찾을 수 없습니다.");
            return;
        }
        
        var snapData = snapDataDict[snapId];
        snapData.attachedFruitId = null;
        snapData.emotion = null;
        snapData.todo = null;
        snapData.date = null;
        snapData.time = null;
        snapData.updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        Debug.Log($"Snap에서 Fruit 분리: {snapId}");
        
        // 백엔드에 저장
        StartCoroutine(SaveSnapData(snapData));
    }
    
    /// <summary>
    /// 특정 Snap의 데이터 가져오기
    /// </summary>
    public SnapData GetSnapData(string snapId)
    {
        return snapDataDict.ContainsKey(snapId) ? snapDataDict[snapId] : null;
    }
    
    /// <summary>
    /// 모든 Snap 데이터 가져오기
    /// </summary>
    public List<SnapData> GetAllSnapData()
    {
        return new List<SnapData>(snapDataList);
    }
    
    /// <summary>
    /// 백엔드에서 Snap 데이터 로드
    /// </summary>
    private IEnumerator LoadSnapData()
    {
        using (var www = UnityWebRequest.Get(snapDataUrl))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                Debug.Log($"Snap 데이터 로드 성공: {json}");
                
                try
                {
                    var snapList = JsonUtility.FromJson<SnapDataList>(json);
                    if (snapList != null && snapList.snaps != null)
                    {
                        snapDataList = snapList.snaps;
                        snapDataDict.Clear();
                        
                        foreach (var snap in snapDataList)
                        {
                            snapDataDict[snap.snapId] = snap;
                        }
                        
                        Debug.Log($"총 {snapDataList.Count}개의 Snap 데이터 로드 완료");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Snap 데이터 파싱 실패: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Snap 데이터 로드 실패: {www.error}");
            }
        }
    }
    
    /// <summary>
    /// 백엔드에 Snap 데이터 저장
    /// </summary>
    private IEnumerator SaveSnapData(SnapData snapData)
    {
        string json = JsonUtility.ToJson(snapData, true);
        Debug.Log($"Snap 데이터 저장: {json}");
        
        using (var req = new UnityWebRequest($"{snapDataUrl}/{snapData.snapId}", "PUT"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            
            yield return req.SendWebRequest();
            
            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Snap 데이터 저장 성공: {snapData.snapId}");
            }
            else
            {
                Debug.LogError($"Snap 데이터 저장 실패: {req.error}");
            }
        }
    }
    
    /// <summary>
    /// 모든 Snap 데이터를 백엔드에 저장
    /// </summary>
    public void SaveAllSnapData()
    {
        StartCoroutine(SaveAllSnapDataCoroutine());
    }
    
    private IEnumerator SaveAllSnapDataCoroutine()
    {
        var snapList = new SnapDataList { snaps = snapDataList };
        string json = JsonUtility.ToJson(snapList, true);
        
        using (var req = new UnityWebRequest(snapDataUrl, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            
            yield return req.SendWebRequest();
            
            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("모든 Snap 데이터 저장 성공");
            }
            else
            {
                Debug.LogError($"모든 Snap 데이터 저장 실패: {req.error}");
            }
        }
    }
} 