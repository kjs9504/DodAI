using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// JSON ↔ C# 매핑용
[Serializable]
public class Position
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class AcceptedTaskData
{
    public long id;
    public string todo, date, time, acceptedAt;
    public long? userId;
}
[Serializable]
public class AcceptedListData
{
    // 백엔드에서 { "tasks":[ … ] } 형태로 내려온다고 가정
    public List<AcceptedTaskData> tasks;
}

[Serializable]
public class FruitData
{
    public long id;
    public long acceptedTaskId;
    public float posX, posY, posZ;
    public string extraData;
    public string createdAt;
    public string emotion; // ← 추가
    public Position position; // ← 추가 (백엔드에서 position json이 올 경우)
}
[Serializable]
public class FruitListData
{
    // 백엔드에서 { "fruits":[ … ] } 형태로 내려온다고 가정
    public List<FruitData> fruits;
}

public class FruitManager : MonoBehaviour
{
    [Header("수락된 할 일 리스트 URL")]
    public string acceptsUrl = "http://localhost:8080/api/tasks/accepted";
    [Header("Fruit API URL")]
    public string fruitUrl = "http://localhost:8080/api/fruits";

    [Header("생성할 Prefab")]
    public GameObject fruitPrefab;
    [Header("고정 스폰 위치 (Transform)")]
    public Transform spawnPoint;
    [Header("랜덤 스폰 범위 (±값, 예: 2,1,2)")]
    public Vector3 spawnRange = new Vector3(2, 1, 2); // Inspector에서 조절
    [Header("한 층에 들어갈 과일 개수")]
    public int fruitsPerLayer = 5; // Inspector에서 조절
    [Header("과일 높이(쌓임 간격)")]
    public float fruitHeight = 1.0f; // Inspector에서 조절

    private List<FruitData> spawnedFruits = new List<FruitData>();

    void Start()
    {
        Debug.Log("[FruitManager] Start() 호출됨");
        Debug.Log($"[FruitManager] fruitPrefab: {(fruitPrefab != null ? "설정됨" : "NULL!")}");
        Debug.Log($"[FruitManager] spawnPoint: {(spawnPoint != null ? "설정됨" : "NULL!")}");
        Debug.Log($"[FruitManager] acceptsUrl: {acceptsUrl}");
        Debug.Log($"[FruitManager] fruitUrl: {fruitUrl}");
        
        StartCoroutine(InitFruits());
    }

    private IEnumerator InitFruits()
    {
        Debug.Log("[FruitManager] InitFruits() 시작");
        
        using (var www = UnityWebRequest.Get(acceptsUrl))
        {
            Debug.Log($"[FruitManager] AcceptedTasks GET 요청: {acceptsUrl}");
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"AcceptedTasks GET 실패: {www.error}");
                Debug.LogError($"응답 코드: {www.responseCode}");
                yield break;
            }

            string rawJson = www.downloadHandler.text;
            Debug.Log($"[FruitManager] AcceptedTasks Raw JSON:\n{rawJson}");

            // ② JSON → 객체 파싱
            var list = JsonUtility.FromJson<AcceptedListData>(rawJson);
            Debug.Log($"[FruitManager] JSON 파싱 결과: list={(list != null ? "성공" : "NULL!")}");

            // ③ 파싱된 각 항목을 로그에 출력
            if (list?.tasks != null && list.tasks.Count > 0)
            {
                Debug.Log($"[FruitManager] AcceptedTasks 개수: {list.tasks.Count}");
                foreach (var task in list.tasks)
                {
                                    Debug.Log($"[FruitManager] AcceptedTask → " +
                          $"id={task.id}, todo={task.todo}, date={task.date}, time={task.time}, acceptedAt={task.acceptedAt}, userId={task.userId}");
                }
            }
            else
            {
                Debug.LogWarning("[FruitManager] AcceptedTasks 목록이 비어 있습니다.");
                yield break; // 목록이 비어있으면 더 이상 진행하지 않음
            }

            // ④ 기존 로직: 각 task 처리
            Debug.Log("[FruitManager] 각 task 처리 시작");
            foreach (var task in list.tasks)
            {
                Debug.Log($"[FruitManager] task {task.id} 처리 시작");
                yield return StartCoroutine(HandleOneTask(task));
            }
            Debug.Log("[FruitManager] 모든 task 처리 완료");
        }
    }

    private IEnumerator HandleOneTask(AcceptedTaskData task)
    {
        Debug.Log($"[FruitManager] HandleOneTask 시작: taskId={task.id}");
        
        // 먼저, 백엔드에 해당 taskId 로 fruit 리스트가 있는지 물어봅니다.
        using (var www = UnityWebRequest.Get($"{fruitUrl}/{task.id}"))
        {
            Debug.Log($"[FruitManager] Fruit GET 요청: {fruitUrl}/{task.id}");
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Fruit GET 실패 for task {task.id}: {www.error}");
                Debug.LogError($"응답 코드: {www.responseCode}");
                yield break;
            }

            // JSON → FruitListData 파싱 (root 필드 이름이 "fruits" 여야 합니다)
            var json = www.downloadHandler.text;
            Debug.Log($"[FruitManager] Fruit GET 응답 JSON: {json}");
            
            var list = JsonUtility.FromJson<FruitListData>(json);
            var fruits = list?.fruits ?? new List<FruitData>();
            Debug.Log($"[FruitManager] 기존 fruits 개수: {fruits.Count}");

            if (fruits.Count == 0)
            {
                Debug.Log($"[FruitManager] 새로운 Fruit 생성 시작: taskId={task.id}");
                
                // (1) spawnPoint 가 있으면 그 위치, 없으면 매니저 위치
                Vector3 spawnPos = spawnPoint != null
                    ? GetBasketSpawnPosition(spawnedFruits.Count)
                    : transform.position;
                
                Debug.Log($"[FruitManager] 스폰 위치: {spawnPos}");
                Debug.Log($"[FruitManager] fruitPrefab: {(fruitPrefab != null ? "NULL이 아님" : "NULL!")}");

                if (fruitPrefab == null)
                {
                    Debug.LogError("[FruitManager] fruitPrefab이 NULL입니다! Inspector에서 설정해주세요.");
                    yield break;
                }

                // (2) 월드 좌표, 완전한 회전(0,0,0)으로 인스턴스화
                GameObject obj = Instantiate(
                    fruitPrefab,
                    spawnPos,
                    Quaternion.identity   // world-space 회전을 0으로 고정
                );
                
                Debug.Log($"[FruitManager] Fruit 인스턴스 생성 완료: {obj.name}");

                // (3) 필요하면 월드 위치 그대로 자식으로 붙입니다.
                //     부모 회전을 물려받지 않으려면 worldPositionStays: true 를 사용
                obj.transform.SetParent(transform, worldPositionStays: true);

                Debug.Log("[FruitManager] after Instantiation rot = " + obj.transform.rotation.eulerAngles);

                // (4) 다시 한번 확실히 회전 리셋
                obj.transform.rotation = Quaternion.identity;

                Debug.Log("[FruitManager] after zeroing rot = " + obj.transform.rotation.eulerAngles);

                // (5) FruitInfoUI 컴포넌트를 꺼져 있거나 없으면 붙이고
                var infoUI = obj.GetComponent<FruitInfoUI>()
                             ?? obj.AddComponent<FruitInfoUI>();
                
                Debug.Log($"[FruitManager] FruitInfoUI 컴포넌트: {(infoUI != null ? "성공" : "NULL!")}");

                // (6) JSON 데이터로 초기화
                infoUI.Initialize(task);

                // (7) 로컬 리스트에도 기록
                spawnedFruits.Add(new FruitData
                {
                    acceptedTaskId = task.id,
                    posX = spawnPos.x,
                    posY = spawnPos.y,
                    posZ = spawnPos.z,
                    extraData = "{}"
                });
                
                Debug.Log($"[FruitManager] Fruit 생성 완료: taskId={task.id}, 위치={spawnPos}");
            }
            else
            {
                Debug.Log($"[FruitManager] 기존 fruit이 있음. 저장된 위치에 생성: taskId={task.id}");
                
                // 기존 과일이 있으면 저장된 위치 정보를 사용해서 생성
                foreach (var fruit in fruits)
                {
                    // position 정보가 있으면 해당 위치, 없으면 spawnPoint
                    Vector3 spawnPos;
                    if (fruit.position != null)
                        spawnPos = new Vector3(fruit.position.x, fruit.position.y, fruit.position.z);
                    else
                        spawnPos = spawnPoint != null ? GetBasketSpawnPosition(spawnedFruits.Count) : transform.position;

                    if (fruitPrefab == null)
                    {
                        Debug.LogError("[FruitManager] fruitPrefab이 NULL입니다! Inspector에서 설정해주세요.");
                        yield break;
                    }

                    GameObject obj = Instantiate(
                        fruitPrefab,
                        spawnPos,
                        Quaternion.identity
                    );
                    Debug.Log($"[FruitManager] 기존 위치에 Fruit 인스턴스 생성 완료: {obj.name}");

                    obj.transform.SetParent(transform, worldPositionStays: true);
                    obj.transform.rotation = Quaternion.identity;

                    var infoUI = obj.GetComponent<FruitInfoUI>()
                                 ?? obj.AddComponent<FruitInfoUI>();
                    
                    Debug.Log($"[FruitManager] FruitInfoUI 컴포넌트: {(infoUI != null ? "성공" : "NULL!")}");

                    infoUI.Initialize(task);

                    // emotion 값 전달
                    if (!string.IsNullOrEmpty(fruit.emotion))
                    {
                        var emojiCtrl = obj.GetComponentInChildren<EmojiController>();
                        if (emojiCtrl != null)
                            emojiCtrl.SetEmotion(fruit.emotion); // 아래에 SetEmotion 구현 필요
                    }

                    spawnedFruits.Add(new FruitData
                    {
                        acceptedTaskId = task.id,
                        posX = spawnPos.x,
                        posY = spawnPos.y,
                        posZ = spawnPos.z,
                        extraData = "{}",
                        emotion = fruit.emotion,
                        position = fruit.position
                    });
                    
                    Debug.Log($"[FruitManager] 기존 위치에 Fruit 생성 완료: taskId={task.id}, 위치={spawnPos}");
                }
            }
        }
    }

    private IEnumerator CreateFruit(long taskId, Vector3 pos)
    {
        var dto = new FruitData
        {
            acceptedTaskId = taskId,
            posX = pos.x,
            posY = pos.y,
            posZ = pos.z,
            extraData = "{}"  // JSON 형태로 보내려면 문자열로
        };
        string json = JsonUtility.ToJson(dto);

        using (var req = new UnityWebRequest(fruitUrl, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogError($"Fruit POST 실패: {req.error}");
        }
    }
        public void OnSaveButtonPressed()
    {
        StartCoroutine(SaveAllSpawnedFruits());
    }

    private IEnumerator SaveAllSpawnedFruits()
    {
        foreach (var fruit in spawnedFruits)
        {
            yield return StartCoroutine(CreateFruit(
                fruit.acceptedTaskId, new Vector3(fruit.posX, fruit.posY, fruit.posZ)));
        }

        Debug.Log("✅ 모든 fruit 저장 완료");
        spawnedFruits.Clear(); // 선택
    }

    // 층마다 랜덤 쌓임 위치 생성 함수
    private Vector3 GetBasketSpawnPosition(int fruitIndex)
    {
        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;
        int layer = fruitIndex / fruitsPerLayer;
        float x = basePos.x + UnityEngine.Random.Range(-spawnRange.x, spawnRange.x);
        float z = basePos.z + UnityEngine.Random.Range(-spawnRange.z, spawnRange.z);
        float y = basePos.y + (layer * fruitHeight) + UnityEngine.Random.Range(-0.1f, 0.1f);
        return new Vector3(x, y, z);
    }
}


