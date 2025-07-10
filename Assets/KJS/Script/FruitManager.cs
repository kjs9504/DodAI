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
    [Header("Instantiate 할 부모 Transform")]
    public Transform parentTransform;

    private List<FruitData> spawnedFruits = new List<FruitData>();

    // 랜덤 스폰 범위
    public Vector3 minSpawn = new Vector3(-5, 0, -5);
    public Vector3 maxSpawn = new Vector3(5, 0, 5);

    void Start()
    {
        StartCoroutine(InitFruits());
    }

    private IEnumerator InitFruits()
    {
        Debug.Log("Fetching AcceptedTasks from " + acceptsUrl);
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
            {
                yield return StartCoroutine(HandleOneTask(task));
            }
        }
    }

    private IEnumerator HandleOneTask(AcceptedTaskData task)
    {
        string url = $"{fruitUrl}/{task.id}";
        using (var www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Fruit GET 실패 for task {task.id}: {www.error}");
                yield break;
            }

            var fruits = JsonUtility.FromJson<FruitListData>(www.downloadHandler.text).fruits;

            if (fruits.Count == 0)
            {
                Vector3 pos = RandomSpawnPosition();
                Debug.Log($"[Spawn Position] {pos}");

                if (fruitPrefab == null)
                {
                    Debug.LogError("❌ fruitPrefab이 null입니다!");
                    yield break;
                }

                GameObject obj = Instantiate(fruitPrefab, pos, Quaternion.identity);
                if (obj == null)
                {
                    Debug.LogError("❌ Instantiate 실패!");
                    yield break;
                }

                obj.transform.SetParent(parentTransform, worldPositionStays: true);
                Debug.Log($"✅ 생성 완료: {obj.name} 위치: {obj.transform.position}");

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
                Debug.Log($"[INFO] 이미 존재하는 fruit이 있어 생성 안 함: taskId={task.id}");
            }

        }
    }

    private Vector3 RandomSpawnPosition()
    {
        return new Vector3(
            UnityEngine.Random.Range(minSpawn.x, maxSpawn.x),
            UnityEngine.Random.Range(minSpawn.y, maxSpawn.y),
            UnityEngine.Random.Range(minSpawn.z, maxSpawn.z)
        );
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


