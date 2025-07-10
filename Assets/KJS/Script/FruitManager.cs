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
            var list = JsonUtility.FromJson<AcceptedListData>(www.downloadHandler.text);
            foreach (var task in list.tasks)
                yield return StartCoroutine(HandleOneTask(task));
        }
    }

    private IEnumerator HandleOneTask(AcceptedTaskData task)
    {
        using (var www = UnityWebRequest.Get($"{fruitUrl}/{task.id}"))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Fruit GET 실패 for task {task.id}: {www.error}");
                yield break;
            }

            var fruits = JsonUtility
                .FromJson<FruitListData>(www.downloadHandler.text)
                .fruits;

            if (fruits.Count == 0)
            {
                // spawnPoint가 있으면 그 위치, 없으면 매니저 오브젝트 위치
                Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;

                // transform을 부모로 지정
                GameObject obj = Instantiate(
                    fruitPrefab,
                    pos,
                    Quaternion.identity,
                    this.transform
                );

                var info = obj.AddComponent<FruitInfo>();
                info.acceptedTaskId = task.id;

                spawnedFruits.Add(new FruitData
                {
                    acceptedTaskId = task.id,
                    posX = pos.x,
                    posY = pos.y,
                    posZ = pos.z,
                    extraData = "{}"
                });
            }
            else
            {
                Debug.Log($"이미 존재하는 fruit이 있어 생성 안 함: taskId={task.id}");
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


