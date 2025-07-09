using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// JSON �� C# ���ο�
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
    // �鿣�忡�� { "tasks":[ �� ] } ���·� �����´ٰ� ����
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
    // �鿣�忡�� { "fruits":[ �� ] } ���·� �����´ٰ� ����
    public List<FruitData> fruits;
}

public class FruitManager : MonoBehaviour
{
    [Header("������ �� �� ����Ʈ URL")]
    public string acceptsUrl = "http://localhost:8080/api/tasks/accepted";
    [Header("Fruit API URL")]
    public string fruitUrl = "http://localhost:8080/api/fruits";

    [Header("������ Prefab")]
    public GameObject fruitPrefab;
    [Header("Instantiate �� �θ� Transform")]
    public Transform parentTransform;

    // ���� ���� ����
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
                Debug.LogError($"AcceptedTasks GET ����: {www.error}");
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
                Debug.LogError($"Fruit GET ���� for task {task.id}: {www.error}");
                yield break;
            }
            var fruits = JsonUtility.FromJson<FruitListData>(www.downloadHandler.text).fruits;

            if (fruits.Count == 0)
            {
                // ���� ����
                Vector3 pos = new Vector3(
                    UnityEngine.Random.Range(minSpawn.x, maxSpawn.x),
                    UnityEngine.Random.Range(minSpawn.y, maxSpawn.y),
                    UnityEngine.Random.Range(minSpawn.z, maxSpawn.z)
                );
                Instantiate(fruitPrefab, pos, Quaternion.identity, parentTransform)
                    .transform.localScale = Vector3.one;

                // DB�� ����
                yield return StartCoroutine(CreateFruit(task.id, pos));
            }
            else
            {
                // ���� ��ġ �״�� ����
                foreach (var f in fruits)
                {
                    Vector3 pos = new Vector3(f.posX, f.posY, f.posZ);
                    Instantiate(fruitPrefab, pos, Quaternion.identity, parentTransform)
                        .transform.localScale = Vector3.one;
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
            extraData = "{}"  // JSON ���·� �������� ���ڿ���
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
                Debug.LogError($"Fruit POST ����: {req.error}");
        }
    }
}


