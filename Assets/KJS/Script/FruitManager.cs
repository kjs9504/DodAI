using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

public class FruitManager : MonoBehaviour
{
    [Header("수락된 할 일 리스트 URL")]
    public string acceptsUrl = "http://192.168.0.58:8080/api/tasks/accepted";
    [Header("Fruit API URL")]
    public string fruitUrl = "http://192.168.0.58:8080/api/fruits";

    [Header("생성할 Prefab")]
    public GameObject fruitPrefab;
    [Header("고정 스폰 위치 (Transform)")]
    public Transform spawnPoint;

    [Header("직사각형 배치 설정")]
    public float rectangleScale = 2.0f;    // 가로 스케일 (X축)
    public float verticalScale = 0.8f;     // 세로 스케일 (Z축) - 간격 줄임
    public float fruitHeight = 1.0f;       // 층당 높이 간격
    
    // 고정 배치 개수 (세로 2개, 가로 4개)
    private const int FRUITS_PER_ROW = 2;      // 세로 줄 수 (Z축)
    private const int FRUITS_PER_COLUMN = 4;   // 가로 줄 수 (X축)

    private List<FruitData> spawnedFruits = new List<FruitData>();
    private List<GameObject> currentFruitObjects = new List<GameObject>();
    private WeeksData currentWeeksData;

    // 주차별 과일 관리
    private Dictionary<string, List<FruitData>> weekFruitData = new Dictionary<string, List<FruitData>>();
    private int totalFruitCounter = 0;     // Grid 배치용 인덱스

    void Start()
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning("[FruitManager] spawnPoint가 null입니다. 현재 transform 사용");
            spawnPoint = transform;
        }

        // 최소값 보정 (0 이하일 때만)
        if (rectangleScale <= 0) rectangleScale = 2.0f;
        if (verticalScale <= 0) verticalScale = 0.8f;
        if (fruitHeight <= 0) fruitHeight = 1.0f;

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

            // NewDataStructure 시도
            var newData = JsonUtility.FromJson<NewDataStructure>(rawJson);
            if (newData != null && newData.fruits != null && newData.fruits.Count > 0)
            {
                weekFruitData.Clear();
                string weekKey = GenerateWeekKey(newData.weekStartDate, newData.weekEndDate);
                weekFruitData[weekKey] = new List<FruitData>(newData.fruits);

                yield return StartCoroutine(CreateAllFruitsWithInitialActivation());
                yield break;
            }

            // WeeksData 시도
            currentWeeksData = JsonUtility.FromJson<WeeksData>(rawJson);
            if (currentWeeksData != null && currentWeeksData.weeks != null && currentWeeksData.weeks.Count > 0)
            {
                weekFruitData.Clear();
                foreach (var week in currentWeeksData.weeks)
                {
                    string weekKey = GenerateWeekKey(week.weekStart, week.weekEnd);
                    weekFruitData[weekKey] = new List<FruitData>(week.fruits);
                }

                yield return StartCoroutine(CreateAllFruitsWithInitialActivation());
                yield break;
            }

            // 기존 AcceptedListData
            var list = JsonUtility.FromJson<AcceptedListData>(rawJson);
            if (list?.tasks != null)
            {
                foreach (var task in list.tasks)
                    yield return StartCoroutine(HandleOneTask(task));
            }
        }
    }

    // 모든 과일을 생성하고 가장 최근 주차만 활성화
    private IEnumerator CreateAllFruitsWithInitialActivation()
    {
        ClearCurrentFruits();

        List<FruitData> allFruits = weekFruitData.Values.SelectMany(f => f).ToList();
        foreach (var fruit in allFruits)
            yield return StartCoroutine(CreateFruitFromDataDisabled(fruit));

        // 최근 주차 활성화
        if (currentWeeksData != null)
        {
            var recentWeek = currentWeeksData.weeks.OrderByDescending(w => DateTime.Parse(w.weekEnd)).First();
            string key = GenerateWeekKey(recentWeek.weekStart, recentWeek.weekEnd);
            yield return StartCoroutine(ActivateSpecificWeek(key));
        }
    }

    // 주차 활성화
    public IEnumerator ActivateSpecificWeek(string weekKey)
    {
        foreach (var fruitObj in currentFruitObjects)
            if (fruitObj != null) fruitObj.SetActive(false);

        if (!weekFruitData.ContainsKey(weekKey))
            yield break;

        var weekFruits = weekFruitData[weekKey];

        foreach (var fruitObj in currentFruitObjects)
        {
            if (fruitObj == null) continue;
            var infoUI = fruitObj.GetComponent<FruitInfoUI>();
            if (infoUI == null) continue;

            if (weekFruits.Any(f => f.acceptedTaskId == infoUI.id))
                fruitObj.SetActive(true);
        }
        yield return null;
    }

    // 과일 생성 (비활성화 상태)
    private IEnumerator CreateFruitFromDataDisabled(FruitData fruit)
    {
        Vector3 spawnPos = GetSpawnPositionFromFruitData(fruit);

        GameObject obj = Instantiate(fruitPrefab, spawnPos, Quaternion.identity, transform);
        obj.SetActive(false);

        SetupFruitInfo(obj, fruit);
        currentFruitObjects.Add(obj);
        totalFruitCounter++;

        yield return null;
    }

    // 과일 생성 (활성화)
    private IEnumerator CreateFruitFromData(FruitData fruit)
    {
        Vector3 spawnPos = GetSpawnPositionFromFruitData(fruit);

        GameObject obj = Instantiate(fruitPrefab, spawnPos, Quaternion.identity, transform);

        SetupFruitInfo(obj, fruit);
        currentFruitObjects.Add(obj);
        totalFruitCounter++;

        yield return null;
    }

    // JSON에서 좌표 가져오기 + NULL/0 체크 → Grid로
    private Vector3 GetSpawnPositionFromFruitData(FruitData fruit)
    {
        // position 객체 우선
        if (fruit.position != null)
        {
            Vector3 p = new Vector3(fruit.position.x, fruit.position.y, fruit.position.z);
            if (p != Vector3.zero) return p;
        }

        // posX, posY, posZ 체크
        if (fruit.posX.HasValue && fruit.posY.HasValue && fruit.posZ.HasValue)
        {
            Vector3 p = new Vector3(fruit.posX.Value, fruit.posY.Value, fruit.posZ.Value);
            if (p != Vector3.zero) return p;
        }

        // 모두 없거나 0이면 Grid
        return GetBasketSpawnPosition(totalFruitCounter);
    }

    // 사각형 배치 위치 계산
    private Vector3 GetBasketSpawnPosition(int fruitIndex)
    {
        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;

        int fruitsPerLayer = FRUITS_PER_ROW * FRUITS_PER_COLUMN;
        int layer = fruitIndex / fruitsPerLayer;
        int indexInLayer = fruitIndex % fruitsPerLayer;

        int row = indexInLayer / FRUITS_PER_COLUMN;
        int column = indexInLayer % FRUITS_PER_COLUMN;

        // 사각형의 시작점 계산 (왼쪽 아래 모서리)
        float startX = basePos.x - rectangleScale * 0.5f;
        float startZ = basePos.z - verticalScale * 0.5f;
        
        // 각 과일의 간격 계산
        float xSpacing = rectangleScale / (FRUITS_PER_COLUMN - 1);
        float zSpacing = verticalScale / (FRUITS_PER_ROW - 1);
        
        // 현재 과일의 위치 계산
        float xOffset = startX + column * xSpacing;
        float zOffset = startZ + row * zSpacing;

        return new Vector3(
            basePos.x + xOffset,
            basePos.y + layer * fruitHeight,
            basePos.z + zOffset
        );
    }

    // FruitInfo 초기화
    private void SetupFruitInfo(GameObject obj, FruitData fruit)
    {
        var infoUI = obj.GetComponent<FruitInfoUI>() ?? obj.AddComponent<FruitInfoUI>();
        var emojiCtrl = obj.GetComponentInChildren<EmojiController>();
        if (emojiCtrl != null) infoUI.emojiController = emojiCtrl;

        var task = new AcceptedTaskData
        {
            id = fruit.acceptedTaskId,
            todo = fruit.todo,
            date = fruit.date,
            emotion = fruit.emotion
        };

        infoUI.Initialize(task);
        infoUI.currentEmotion = string.IsNullOrEmpty(fruit.emotion) || fruit.emotion.ToLower() == "none" ? "" : fruit.emotion;

        if (!string.IsNullOrEmpty(infoUI.currentEmotion) && emojiCtrl != null)
            emojiCtrl.SetEmotion(infoUI.currentEmotion);
    }

    private void ClearCurrentFruits()
    {
        foreach (var obj in currentFruitObjects)
            if (obj != null) DestroyImmediate(obj);

        currentFruitObjects.Clear();
        totalFruitCounter = 0;
    }

    // 주차 키 생성
    public static string GenerateWeekKey(string weekStart, string weekEnd)
    {
        return $"{weekStart.Split('T')[0]} ~ {weekEnd.Split('T')[0]}";
    }

    // 기존 구조 (AcceptedListData) 지원
    private IEnumerator HandleOneTask(AcceptedTaskData task)
    {
        Vector3 spawnPos = GetBasketSpawnPosition(totalFruitCounter);
        GameObject obj = Instantiate(fruitPrefab, spawnPos, Quaternion.identity, transform);

        SetupFruitInfo(obj, new FruitData
        {
            acceptedTaskId = task.id,
            todo = task.todo,
            date = task.date,
            emotion = task.emotion
        });

        currentFruitObjects.Add(obj);
        totalFruitCounter++;
        yield return null;
    }
}


