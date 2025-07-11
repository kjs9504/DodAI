using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// JSON ↔ C# 매핑용
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

    private List<FruitData> spawnedFruits = new List<FruitData>();

    void Start()
    {
        StartCoroutine(InitFruits());
    }

    private IEnumerator InitFruits()
    {
        using (var www = UnityWebRequest.Get(acceptsUrl))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"AcceptedTasks GET 실패: {www.error}");
                yield break;
            }

            string rawJson = www.downloadHandler.text;
            Debug.Log($"[FruitManager] AcceptedTasks Raw JSON:\n{rawJson}");

            // ② JSON → 객체 파싱
            var list = JsonUtility.FromJson<AcceptedListData>(rawJson);

            // ③ 파싱된 각 항목을 로그에 출력
            if (list.tasks != null && list.tasks.Count > 0)
            {
                foreach (var task in list.tasks)
                {
                    Debug.Log($"[FruitManager] AcceptedTask → " +
                              $"id={task.id}, todo={task.todo}, date={task.date}, time={task.time}, userId={task.userId}");
                }
            }
            else
            {
                Debug.LogWarning("[FruitManager] AcceptedTasks 목록이 비어 있습니다.");
            }

            // ④ 기존 로직: 각 task 처리
            foreach (var task in list.tasks)
                yield return StartCoroutine(HandleOneTask(task));
        }
    }

    private IEnumerator HandleOneTask(AcceptedTaskData task)
    {
        // 먼저, 백엔드에 해당 taskId 로 fruit 리스트가 있는지 물어봅니다.
        using (var www = UnityWebRequest.Get($"{fruitUrl}/{task.id}"))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Fruit GET 실패 for task {task.id}: {www.error}");
                yield break;
            }

            // JSON → FruitListData 파싱 (root 필드 이름이 "fruits" 여야 합니다)
            var json = www.downloadHandler.text;
            var list = JsonUtility.FromJson<FruitListData>(json);
            var fruits = list?.fruits ?? new List<FruitData>();

            if (fruits.Count == 0)
            {
                // (1) spawnPoint 가 있으면 그 위치, 없으면 매니저 위치
                Vector3 spawnPos = spawnPoint != null
                    ? spawnPoint.position
                    : transform.position;

                // (2) 월드 좌표, 완전한 회전(0,0,0)으로 인스턴스화
                GameObject obj = Instantiate(
                    fruitPrefab,
                    spawnPos,
                    Quaternion.identity   // world-space 회전을 0으로 고정
                );

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
            }
            else
            {
                Debug.Log($"[FruitManager] 이미 존재하는 fruit이 있어 생성 안 함: taskId={task.id}");
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
}


