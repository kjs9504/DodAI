using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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
    [Header("랜덤 스폰 범위 (±값, 예: 2,1,2)")]
    public Vector3 spawnRange = new Vector3(2, 1, 2);
    [Header("한 층에 들어갈 과일 개수")]
    public int fruitsPerLayer = 5;
    [Header("과일 높이(쌓임 간격)")]
    public float fruitHeight = 1.0f;

    private List<FruitData> spawnedFruits = new List<FruitData>();
    private List<GameObject> currentFruitObjects = new List<GameObject>();
    private WeeksData currentWeeksData;
    
    // 주차별 과일 관리를 위한 딕셔너리
    private Dictionary<string, List<GameObject>> weekFruitObjects = new Dictionary<string, List<GameObject>>();
    private Dictionary<string, List<FruitData>> weekFruitData = new Dictionary<string, List<FruitData>>();

    void Start()
    {
        Debug.Log("[FruitManager] Start() 호출됨");
        Debug.Log($"[FruitManager] fruitPrefab: {(fruitPrefab != null ? "설정됨" : "NULL!")}");
        Debug.Log($"[FruitManager] spawnPoint: {(spawnPoint != null ? "설정됨" : "NULL!")}");
        Debug.Log($"[FruitManager] acceptsUrl: {acceptsUrl}");
        
        // spawnPoint가 null이면 현재 transform을 기본값으로 설정
        if (spawnPoint == null)
        {
            Debug.LogWarning("[FruitManager] spawnPoint가 null입니다! 현재 transform을 기본값으로 설정합니다.");
            spawnPoint = transform;
        }
        
        // spawnRange가 0이면 기본값 설정
        if (spawnRange == Vector3.zero)
        {
            Debug.LogWarning("[FruitManager] spawnRange가 0입니다! 기본값 (2,1,2)로 설정합니다.");
            spawnRange = new Vector3(2, 1, 2);
        }
        
        StartCoroutine(InitFruits());
    }

    private IEnumerator InitFruits()
    {
        Debug.Log("[FruitManager] InitFruits() 시작");
        
        // 기존 엔드포인트 사용하되 새로운 JSON 구조로 파싱
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

            // 1. 새로운 단일 객체 구조로 파싱 시도
            var newData = JsonUtility.FromJson<NewDataStructure>(rawJson);
            if (newData != null && newData.fruits != null && newData.fruits.Count > 0)
            {
                Debug.Log($"[FruitManager] ✅ 새로운 단일 객체 구조로 파싱 성공: {newData.fruits.Count}개 fruits");
                Debug.Log($"[FruitManager] Week: {newData.weekStartDate} ~ {newData.weekEndDate}");
                
                // JSON에서 null 값을 제대로 처리하기 위해 fruits의 posX, posY, posZ를 수정
                FixNullPositionValuesForNewData(rawJson, newData);
                
                // TreeController가 있으면 TreeController에 데이터 전달
                var treeController = FindObjectOfType<TreeController>();
                if (treeController != null)
                {
                    Debug.Log("[FruitManager] TreeController에 새로운 데이터 구조 전달");
                    // TreeController가 자동으로 fruits를 활성화할 것입니다
                }
                else
                {
                    // TreeController가 없으면 직접 fruits 활성화
                    yield return StartCoroutine(ActivateWeekFruits(newData.fruits));
                }
                yield break;
            }
            
            // 2. 새로운 WeeksData 구조로 파싱 시도
            currentWeeksData = JsonUtility.FromJson<WeeksData>(rawJson);
            
            if (currentWeeksData != null && currentWeeksData.weeks != null && currentWeeksData.weeks.Count > 0)
            {
                Debug.Log($"[FruitManager] ✅ WeeksData 구조로 파싱 성공: {currentWeeksData.weeks.Count}개 weeks");
                
                // JSON에서 null 값을 제대로 처리하기 위해 fruits의 posX, posY, posZ를 수정
                FixNullPositionValues(rawJson, currentWeeksData);
                
                // 주차별로 과일 데이터 저장
                weekFruitData.Clear();
                List<FruitData> allFruits = new List<FruitData>();
                
                Debug.Log($"[FruitManager] 주차별 과일 데이터 저장 시작:");
                foreach (var week in currentWeeksData.weeks)
                {
                    if (week.fruits != null && week.fruits.Count > 0)
                    {
                        string weekKey = GenerateWeekKey(week.weekStart, week.weekEnd);
                        weekFruitData[weekKey] = new List<FruitData>(week.fruits);
                        allFruits.AddRange(week.fruits);
                        
                        Debug.Log($"[FruitManager] 주차 '{weekKey}'에서 {week.fruits.Count}개 과일 저장");
                        Debug.Log($"[FruitManager]   - weekStart: '{week.weekStart}', weekEnd: '{week.weekEnd}'");
                    }
                }
                
                Debug.Log($"[FruitManager] weekFruitData에 저장된 모든 키들:");
                foreach (var key in weekFruitData.Keys)
                {
                    Debug.Log($"[FruitManager]   - '{key}' (과일 {weekFruitData[key].Count}개)");
                }
                
                Debug.Log($"[FruitManager] 총 {allFruits.Count}개 과일 데이터가 저장되었습니다.");
                
                // 모든 과일을 생성하되, 초기에는 가장 최근 주차만 활성화
                Debug.Log($"[FruitManager] 모든 과일을 생성하고 초기에는 가장 최근 주차만 활성화합니다.");
                yield return StartCoroutine(CreateAllFruitsWithInitialActivation());
            }
            else
            {
                // 3. 기존 구조로 시도
                Debug.LogWarning("[FruitManager] ❌ WeeksData 구조 파싱 실패, 기존 구조로 시도");
                
                var list = JsonUtility.FromJson<AcceptedListData>(rawJson);
                
                if (list?.tasks != null && list.tasks.Count > 0)
                {
                    Debug.Log($"[FruitManager] ✅ 기존 구조로 파싱 성공: {list.tasks.Count}개 tasks");
                    foreach (var task in list.tasks)
                    {
                        Debug.Log($"[FruitManager] Task → id={task.id}, todo={task.todo}, date={task.date}, time={task.time}, emotion='{task.emotion}'");
                        yield return StartCoroutine(HandleOneTask(task));
                    }
                }
                else
                {
                    Debug.LogError("[FruitManager] ❌ 모든 JSON 파싱 시도 실패");
                }
            }
        }
    }

    // 새로운 단일 객체 구조에서 null 값을 제대로 처리하는 메서드
    private void FixNullPositionValuesForNewData(string rawJson, NewDataStructure newData)
    {
        Debug.Log("[FruitManager] FixNullPositionValuesForNewData 시작");
        
        if (newData.fruits != null)
        {
            foreach (var fruit in newData.fruits)
            {
                // JSON 문자열에서 해당 fruit의 posX, posY, posZ가 "null"인지 확인
                string fruitJson = GetFruitJsonFromRawJson(rawJson, fruit.id);
                if (!string.IsNullOrEmpty(fruitJson))
                {
                    // posX가 "null"이면 nullable float를 null로 설정
                    if (fruitJson.Contains("\"posX\":null"))
                    {
                        fruit.posX = null;
                        Debug.Log($"[FruitManager] Fruit {fruit.id}의 posX를 null로 설정");
                    }
                    
                    // posY가 "null"이면 nullable float를 null로 설정
                    if (fruitJson.Contains("\"posY\":null"))
                    {
                        fruit.posY = null;
                        Debug.Log($"[FruitManager] Fruit {fruit.id}의 posY를 null로 설정");
                    }
                    
                    // posZ가 "null"이면 nullable float를 null로 설정
                    if (fruitJson.Contains("\"posZ\":null"))
                    {
                        fruit.posZ = null;
                        Debug.Log($"[FruitManager] Fruit {fruit.id}의 posZ를 null로 설정");
                    }
                    
                    Debug.Log($"[FruitManager] Fruit {fruit.id} 최종 위치: posX={fruit.posX}, posY={fruit.posY}, posZ={fruit.posZ}");
                }
            }
        }
    }

    // JSON에서 null 값을 제대로 처리하는 메서드
    private void FixNullPositionValues(string rawJson, WeeksData weeksData)
    {
        Debug.Log("[FruitManager] FixNullPositionValues 시작");
        
        foreach (var week in weeksData.weeks)
        {
            if (week.fruits != null)
            {
                foreach (var fruit in week.fruits)
                {
                    // JSON 문자열에서 해당 fruit의 posX, posY, posZ가 "null"인지 확인
                    string fruitJson = GetFruitJsonFromRawJson(rawJson, fruit.id);
                    if (!string.IsNullOrEmpty(fruitJson))
                    {
                        // posX가 "null"이면 nullable float를 null로 설정
                        if (fruitJson.Contains("\"posX\":null"))
                        {
                            fruit.posX = null;
                            Debug.Log($"[FruitManager] Fruit {fruit.id}의 posX를 null로 설정");
                        }
                        
                        // posY가 "null"이면 nullable float를 null로 설정
                        if (fruitJson.Contains("\"posY\":null"))
                        {
                            fruit.posY = null;
                            Debug.Log($"[FruitManager] Fruit {fruit.id}의 posY를 null로 설정");
                        }
                        
                        // posZ가 "null"이면 nullable float를 null로 설정
                        if (fruitJson.Contains("\"posZ\":null"))
                        {
                            fruit.posZ = null;
                            Debug.Log($"[FruitManager] Fruit {fruit.id}의 posZ를 null로 설정");
                        }
                        
                        Debug.Log($"[FruitManager] Fruit {fruit.id} 최종 위치: posX={fruit.posX}, posY={fruit.posY}, posZ={fruit.posZ}");
                    }
                }
            }
        }
    }
    
    // JSON 문자열에서 특정 fruit의 JSON 부분을 추출
    private string GetFruitJsonFromRawJson(string rawJson, long fruitId)
    {
        try
        {
            // fruit ID를 찾아서 해당 fruit의 JSON 부분을 추출
            string searchPattern = $"\"id\":{fruitId},";
            int startIndex = rawJson.IndexOf(searchPattern);
            if (startIndex != -1)
            {
                // fruit 객체의 시작 부분 찾기
                int braceCount = 0;
                int fruitStart = -1;
                for (int i = startIndex; i >= 0; i--)
                {
                    if (rawJson[i] == '}')
                    {
                        braceCount++;
                    }
                    else if (rawJson[i] == '{')
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            fruitStart = i;
                            break;
                        }
                    }
                }
                
                if (fruitStart != -1)
                {
                    // fruit 객체의 끝 부분 찾기
                    braceCount = 0;
                    int fruitEnd = -1;
                    for (int i = fruitStart; i < rawJson.Length; i++)
                    {
                        if (rawJson[i] == '{')
                        {
                            braceCount++;
                        }
                        else if (rawJson[i] == '}')
                        {
                            braceCount--;
                            if (braceCount == 0)
                            {
                                fruitEnd = i;
                                break;
                            }
                        }
                    }
                    
                    if (fruitEnd != -1)
                    {
                        return rawJson.Substring(fruitStart, fruitEnd - fruitStart + 1);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FruitManager] GetFruitJsonFromRawJson 오류: {e.Message}");
        }
        
        return "";
    }

    // 가장 최근 주 찾기
    private WeekData GetMostRecentWeek()
    {
        if (currentWeeksData?.weeks == null || currentWeeksData.weeks.Count == 0)
            return null;

        WeekData mostRecentWeek = null;
        System.DateTime mostRecentDate = System.DateTime.MinValue;

        foreach (var week in currentWeeksData.weeks)
        {
            if (System.DateTime.TryParse(week.weekEnd, out System.DateTime weekEndDate))
            {
                if (weekEndDate > mostRecentDate)
                {
                    mostRecentDate = weekEndDate;
                    mostRecentWeek = week;
                }
            }
        }

        return mostRecentWeek;
    }

    // 특정 주의 fruits 활성화 (TreeController에서 호출)
    public IEnumerator ActivateWeekFruits(List<FruitData> weekFruits)
    {
        Debug.Log($"[FruitManager] ActivateWeekFruits() 호출됨 - fruits 개수: {weekFruits?.Count ?? 0}");
        
        // 기존 과일 오브젝트들 제거
        ClearCurrentFruits();
        
        if (weekFruits == null || weekFruits.Count == 0)
        {
            Debug.LogWarning("[FruitManager] 활성화할 fruits가 없습니다.");
            yield break;
        }

        // 각 fruit 생성
        foreach (var fruit in weekFruits)
        {
            yield return StartCoroutine(CreateFruitFromData(fruit));
        }
        
        Debug.Log($"[FruitManager] Week fruits 활성화 완료: {weekFruits.Count}개");
    }
    
    // 특정 주차의 과일만 활성화
    public IEnumerator ActivateSpecificWeek(string weekKey)
    {
        Debug.Log($"[FruitManager] ActivateSpecificWeek() 호출됨 - 주차: {weekKey}");
        
        // weekFruitData의 모든 키 출력
        Debug.Log($"[FruitManager] weekFruitData에 저장된 주차 키들:");
        foreach (var key in weekFruitData.Keys)
        {
            Debug.Log($"[FruitManager]   - '{key}' (과일 {weekFruitData[key].Count}개)");
        }
        
        if (!weekFruitData.ContainsKey(weekKey))
        {
            Debug.LogWarning($"[FruitManager] 주차 {weekKey}의 데이터가 없습니다.");
            Debug.LogWarning($"[FruitManager] 사용 가능한 주차: {string.Join(", ", weekFruitData.Keys)}");
            yield break;
        }
        
        // 모든 과일을 비활성화
        foreach (var fruitObj in currentFruitObjects)
        {
            if (fruitObj != null)
            {
                fruitObj.SetActive(false);
            }
        }
        
        // 해당 주차의 과일들만 활성화
        var weekFruits = weekFruitData[weekKey];
        Debug.Log($"[FruitManager] {weekKey} 주차에서 {weekFruits.Count}개 과일 찾기 시작");
        Debug.Log($"[FruitManager] 현재 생성된 과일 오브젝트 수: {currentFruitObjects.Count}");
        
        // 모든 과일을 순회하면서 해당 주차의 과일들만 활성화
        int activatedCount = 0;
        foreach (var fruitObj in currentFruitObjects)
        {
            if (fruitObj == null) continue;
            
            var infoUI = fruitObj.GetComponent<FruitInfoUI>();
            if (infoUI == null) continue;
            
            // 이 과일이 현재 주차에 속하는지 확인
            bool belongsToWeek = false;
            foreach (var fruit in weekFruits)
            {
                if (infoUI.id == fruit.acceptedTaskId)
                {
                    belongsToWeek = true;
                    Debug.Log($"[FruitManager] 과일 매칭 발견: infoUI.id={infoUI.id} == fruit.acceptedTaskId={fruit.acceptedTaskId} (fruit.id={fruit.id})");
                    break;
                }
            }
            
            if (belongsToWeek)
            {
                fruitObj.SetActive(true);
                activatedCount++;
                Debug.Log($"[FruitManager] 과일 활성화: infoUI.id={infoUI.id}");
            }
            else
            {
                fruitObj.SetActive(false);
            }
        }
        
        Debug.Log($"[FruitManager] {weekKey} 주차에서 {activatedCount}개 과일 활성화 완료");
        
        Debug.Log($"[FruitManager] 주차 {weekKey} 과일 활성화 완료: {weekFruits.Count}개");
    }
    
    // 모든 주차의 과일 활성화
    public IEnumerator ActivateAllWeeks()
    {
        Debug.Log("[FruitManager] ActivateAllWeeks() 호출됨");
        
        // 기존 과일 오브젝트들 제거
        ClearCurrentFruits();
        
        // 모든 주차의 과일들을 수집
        List<FruitData> allFruits = new List<FruitData>();
        foreach (var weekData in weekFruitData.Values)
        {
            allFruits.AddRange(weekData);
        }
        
        // 모든 과일 생성
        foreach (var fruit in allFruits)
        {
            yield return StartCoroutine(CreateFruitFromData(fruit));
        }
        
        Debug.Log($"[FruitManager] 모든 주차 과일 활성화 완료: {allFruits.Count}개");
    }
    
    // 특정 주차의 과일 비활성화 (다른 주차 활성화 시 자동으로 처리됨)
    public void DeactivateAllFruits()
    {
        Debug.Log("[FruitManager] DeactivateAllFruits() 호출됨");
        
        // 모든 과일을 비활성화 (제거하지 않음)
        foreach (var fruitObj in currentFruitObjects)
        {
            if (fruitObj != null)
            {
                fruitObj.SetActive(false);
            }
        }
    }
    
    // 사용 가능한 주차 목록 반환
    public List<string> GetAvailableWeeks()
    {
        var availableWeeks = new List<string>(weekFruitData.Keys);
        Debug.Log($"[FruitManager] GetAvailableWeeks() 호출됨 - {availableWeeks.Count}개 주차 반환");
        foreach (var week in availableWeeks)
        {
            Debug.Log($"[FruitManager]   - '{week}'");
        }
        return availableWeeks;
    }
    
    // 특정 주차의 과일 데이터 반환
    public List<FruitData> GetWeekFruits(string weekKey)
    {
        if (weekFruitData.ContainsKey(weekKey))
        {
            return weekFruitData[weekKey];
        }
        return null;
    }
    
    // 모든 과일을 생성하고 초기에는 가장 최근 주차만 활성화
    private IEnumerator CreateAllFruitsWithInitialActivation()
    {
        Debug.Log("[FruitManager] CreateAllFruitsWithInitialActivation() 시작");
        
        // 기존 과일 오브젝트들 제거
        ClearCurrentFruits();
        
        // 모든 과일을 생성하되 비활성화 상태로 시작
        List<FruitData> allFruits = new List<FruitData>();
        foreach (var weekData in weekFruitData.Values)
        {
            allFruits.AddRange(weekData);
        }
        
        // 모든 과일을 생성 (비활성화 상태로)
        foreach (var fruit in allFruits)
        {
            yield return StartCoroutine(CreateFruitFromDataDisabled(fruit));
        }
        
        Debug.Log($"[FruitManager] 모든 과일 {allFruits.Count}개 생성 완료 (비활성화 상태)");
        
        // 가장 최근 주차의 과일만 활성화
        var mostRecentWeek = GetMostRecentWeek();
        if (mostRecentWeek != null)
        {
            string mostRecentWeekKey = GenerateWeekKey(mostRecentWeek.weekStart, mostRecentWeek.weekEnd);
            Debug.Log($"[FruitManager] 가장 최근 주차 '{mostRecentWeekKey}'의 과일만 활성화합니다.");
            
            if (weekFruitData.ContainsKey(mostRecentWeekKey))
            {
                var weekFruits = weekFruitData[mostRecentWeekKey];
                
                // 모든 과일을 순회하면서 해당 주차의 과일들만 활성화
                int activatedCount = 0;
                foreach (var fruitObj in currentFruitObjects)
                {
                    if (fruitObj == null) continue;
                    
                    var infoUI = fruitObj.GetComponent<FruitInfoUI>();
                    if (infoUI == null) continue;
                    
                    // 이 과일이 현재 주차에 속하는지 확인
                    bool belongsToWeek = false;
                    foreach (var fruit in weekFruits)
                    {
                        if (infoUI.id == fruit.acceptedTaskId)
                        {
                            belongsToWeek = true;
                            Debug.Log($"[FruitManager] 초기 과일 매칭 발견: infoUI.id={infoUI.id} == fruit.acceptedTaskId={fruit.acceptedTaskId} (fruit.id={fruit.id})");
                            break;
                        }
                    }
                    
                    if (belongsToWeek)
                    {
                        fruitObj.SetActive(true);
                        activatedCount++;
                        Debug.Log($"[FruitManager] 초기 과일 활성화: infoUI.id={infoUI.id}");
                    }
                    else
                    {
                        fruitObj.SetActive(false);
                    }
                }
                
                Debug.Log($"[FruitManager] 주차 {mostRecentWeekKey}에서 {activatedCount}개 과일 활성화 완료");
            }
        }
        else
        {
            Debug.LogWarning("[FruitManager] 가장 최근 주차를 찾을 수 없습니다.");
        }
    }
    
    // 비활성화 상태로 과일 생성
    private IEnumerator CreateFruitFromDataDisabled(FruitData fruit)
    {
        Vector3 spawnPos;
        
        // position이 있으면 사용 (단, (0,0,0)이면 spawnPoint 사용)
        if (fruit.position != null)
        {
            Vector3 positionValue = new Vector3(fruit.position.x, fruit.position.y, fruit.position.z);
            
            // position이 (0,0,0)이면 spawnPoint 사용
            if (positionValue == Vector3.zero)
            {
                Debug.Log($"[FruitManager] Fruit {fruit.id}의 position이 (0,0,0)이므로 spawnPoint 사용");
                spawnPos = GetBasketSpawnPosition(currentFruitObjects.Count);
            }
            else
            {
                spawnPos = positionValue;
                Debug.Log($"[FruitManager] Fruit {fruit.id}의 position 사용: {spawnPos}");
            }
        }
        // posX, posY, posZ가 모두 null이 아니면 사용 (단, (0,0,0)이면 spawnPoint 사용)
        else if (fruit.posX.HasValue && fruit.posY.HasValue && fruit.posZ.HasValue)
        {
            Vector3 posValue = new Vector3(fruit.posX.Value, fruit.posY.Value, fruit.posZ.Value);
            
            // posX, posY, posZ가 모두 0이면 spawnPoint 사용
            if (posValue == Vector3.zero)
            {
                Debug.Log($"[FruitManager] Fruit {fruit.id}의 posX, posY, posZ가 (0,0,0)이므로 spawnPoint 사용");
                spawnPos = GetBasketSpawnPosition(currentFruitObjects.Count);
            }
            else
            {
                spawnPos = posValue;
                Debug.Log($"[FruitManager] Fruit {fruit.id}의 posX, posY, posZ 사용: {spawnPos}");
            }
        }
        // position과 posX, posY, posZ가 모두 null이면 spawnPoint 사용
        else
        {
            Debug.Log($"[FruitManager] Fruit {fruit.id}의 position과 posX, posY, posZ가 모두 null입니다. spawnPoint 상태 확인:");
            Debug.Log($"[FruitManager]   - position: {(fruit.position != null ? "있음" : "null")}");
            Debug.Log($"[FruitManager]   - posX: {(fruit.posX.HasValue ? fruit.posX.Value.ToString() : "null")}");
            Debug.Log($"[FruitManager]   - posY: {(fruit.posY.HasValue ? fruit.posY.Value.ToString() : "null")}");
            Debug.Log($"[FruitManager]   - posZ: {(fruit.posZ.HasValue ? fruit.posZ.Value.ToString() : "null")}");
            Debug.Log($"[FruitManager]   - spawnPoint: {(spawnPoint != null ? "설정됨" : "NULL!")}");
            Debug.Log($"[FruitManager]   - spawnPoint.position: {(spawnPoint != null ? spawnPoint.position.ToString() : "N/A")}");
            Debug.Log($"[FruitManager]   - spawnRange: {spawnRange}");
            Debug.Log($"[FruitManager]   - currentFruitObjects.Count: {currentFruitObjects.Count}");
            
            // currentFruitObjects.Count를 사용하여 현재 생성된 과일 개수를 기준으로 위치 계산
            spawnPos = GetBasketSpawnPosition(currentFruitObjects.Count);
            Debug.Log($"[FruitManager] Fruit {fruit.id}의 position과 posX, posY, posZ가 모두 null이므로 spawnPoint에서 랜덤 위치 생성: {spawnPos}");
        }

        Debug.Log($"[FruitManager] Fruit 생성 (비활성화): id={fruit.id}, taskId={fruit.acceptedTaskId}, 위치={spawnPos}, emotion='{fruit.emotion}'");

        if (fruitPrefab == null)
        {
            Debug.LogError("[FruitManager] fruitPrefab이 NULL입니다! Inspector에서 설정해주세요.");
            yield break;
        }

        GameObject obj = Instantiate(fruitPrefab, spawnPos, Quaternion.identity);
        obj.transform.SetParent(transform, worldPositionStays: true);
        obj.transform.rotation = Quaternion.identity;
        
        // 비활성화 상태로 생성
        obj.SetActive(false);

        var infoUI = obj.GetComponent<FruitInfoUI>() ?? obj.AddComponent<FruitInfoUI>();
        
        // EmojiController 찾아서 FruitInfoUI에 설정
        var emojiCtrl = obj.GetComponent<EmojiController>();
        if (emojiCtrl == null)
        {
            emojiCtrl = obj.GetComponentInChildren<EmojiController>(false);
        }
        
        if (emojiCtrl != null && infoUI.emojiController == null)
        {
            infoUI.emojiController = emojiCtrl;
        }

        // 임시 AcceptedTaskData 생성 (fruit 정보 기반)
        var tempTask = new AcceptedTaskData
        {
            id = fruit.acceptedTaskId, // fruit.acceptedTaskId 사용 (사용자 요청에 따라)
            todo = fruit.todo, // fruit의 실제 todo 사용
            date = fruit.date, // fruit의 실제 date 사용
            time = "", // time은 fruit에 없으므로 빈 문자열
            acceptedAt = fruit.createdAt,
            emotion = fruit.emotion
        };

        Debug.Log($"[FruitManager] tempTask 생성: id={tempTask.id} (fruit.acceptedTaskId={fruit.acceptedTaskId}), todo={tempTask.todo}, emotion={tempTask.emotion}");

        infoUI.Initialize(tempTask);
        if (infoUI.emojiController != null)
            infoUI.emojiController.SetCurrentFruitInfoUI(infoUI);
        
        // emotion 값 처리
        string emotionToUse = !string.IsNullOrEmpty(fruit.emotion) ? fruit.emotion : "";
        
        // 'none' 값을 빈 문자열로 처리
        if (emotionToUse == "none" || emotionToUse == "NONE")
        {
            emotionToUse = "";
            Debug.Log($"[FruitManager] Fruit {fruit.id}의 'none' 값을 빈 문자열로 변환");
        }
        
        infoUI.currentEmotion = emotionToUse;
        
        // emotion 값이 있으면 EmojiController에 설정
        if (!string.IsNullOrEmpty(emotionToUse))
        {
            StartCoroutine(SetEmotionAfterAwake(obj, emotionToUse));
        }

        currentFruitObjects.Add(obj);
        
        Debug.Log($"[FruitManager] Fruit 생성 완료 (비활성화): id={fruit.id}, taskId={fruit.acceptedTaskId}, 위치={spawnPos}, emotion={emotionToUse}");
    }

    // FruitData로부터 과일 생성
    private IEnumerator CreateFruitFromData(FruitData fruit)
    {
        Vector3 spawnPos;
        
        // position이 있으면 사용 (단, (0,0,0)이면 spawnPoint 사용)
        if (fruit.position != null)
        {
            Vector3 positionValue = new Vector3(fruit.position.x, fruit.position.y, fruit.position.z);
            
            // position이 (0,0,0)이면 spawnPoint 사용
            if (positionValue == Vector3.zero)
            {
                Debug.Log($"[FruitManager] Fruit {fruit.id}의 position이 (0,0,0)이므로 spawnPoint 사용");
                spawnPos = GetBasketSpawnPosition(currentFruitObjects.Count);
            }
            else
            {
                spawnPos = positionValue;
                Debug.Log($"[FruitManager] Fruit {fruit.id}의 position 사용: {spawnPos}");
            }
        }
        // posX, posY, posZ가 모두 null이 아니면 사용 (단, (0,0,0)이면 spawnPoint 사용)
        else if (fruit.posX.HasValue && fruit.posY.HasValue && fruit.posZ.HasValue)
        {
            Vector3 posValue = new Vector3(fruit.posX.Value, fruit.posY.Value, fruit.posZ.Value);
            
            // posX, posY, posZ가 모두 0이면 spawnPoint 사용
            if (posValue == Vector3.zero)
            {
                Debug.Log($"[FruitManager] Fruit {fruit.id}의 posX, posY, posZ가 (0,0,0)이므로 spawnPoint 사용");
                spawnPos = GetBasketSpawnPosition(currentFruitObjects.Count);
            }
            else
            {
                spawnPos = posValue;
                Debug.Log($"[FruitManager] Fruit {fruit.id}의 posX, posY, posZ 사용: {spawnPos}");
            }
        }
        // position과 posX, posY, posZ가 모두 null이면 spawnPoint 사용
        else
        {
            Debug.Log($"[FruitManager] Fruit {fruit.id}의 position과 posX, posY, posZ가 모두 null입니다. spawnPoint 상태 확인:");
            Debug.Log($"[FruitManager]   - position: {(fruit.position != null ? "있음" : "null")}");
            Debug.Log($"[FruitManager]   - posX: {(fruit.posX.HasValue ? fruit.posX.Value.ToString() : "null")}");
            Debug.Log($"[FruitManager]   - posY: {(fruit.posY.HasValue ? fruit.posY.Value.ToString() : "null")}");
            Debug.Log($"[FruitManager]   - posZ: {(fruit.posZ.HasValue ? fruit.posZ.Value.ToString() : "null")}");
            Debug.Log($"[FruitManager]   - spawnPoint: {(spawnPoint != null ? "설정됨" : "NULL!")}");
            Debug.Log($"[FruitManager]   - spawnPoint.position: {(spawnPoint != null ? spawnPoint.position.ToString() : "N/A")}");
            Debug.Log($"[FruitManager]   - spawnRange: {spawnRange}");
            Debug.Log($"[FruitManager]   - currentFruitObjects.Count: {currentFruitObjects.Count}");
            
            // currentFruitObjects.Count를 사용하여 현재 생성된 과일 개수를 기준으로 위치 계산
            spawnPos = GetBasketSpawnPosition(currentFruitObjects.Count);
            Debug.Log($"[FruitManager] Fruit {fruit.id}의 position과 posX, posY, posZ가 모두 null이므로 spawnPoint에서 랜덤 위치 생성: {spawnPos}");
        }

        Debug.Log($"[FruitManager] Fruit 생성: id={fruit.id}, taskId={fruit.acceptedTaskId}, 위치={spawnPos}, emotion='{fruit.emotion}'");

        if (fruitPrefab == null)
        {
            Debug.LogError("[FruitManager] fruitPrefab이 NULL입니다! Inspector에서 설정해주세요.");
            yield break;
        }

        GameObject obj = Instantiate(fruitPrefab, spawnPos, Quaternion.identity);
        obj.transform.SetParent(transform, worldPositionStays: true);
        obj.transform.rotation = Quaternion.identity;

        var infoUI = obj.GetComponent<FruitInfoUI>() ?? obj.AddComponent<FruitInfoUI>();
        
        // EmojiController 찾아서 FruitInfoUI에 설정
        var emojiCtrl = obj.GetComponent<EmojiController>();
        if (emojiCtrl == null)
        {
            emojiCtrl = obj.GetComponentInChildren<EmojiController>(false);
        }
        
        if (emojiCtrl != null && infoUI.emojiController == null)
        {
            infoUI.emojiController = emojiCtrl;
        }

        // 임시 AcceptedTaskData 생성 (fruit 정보 기반)
        var tempTask = new AcceptedTaskData
        {
            id = fruit.acceptedTaskId, // fruit.acceptedTaskId 사용 (사용자 요청에 따라)
            todo = fruit.todo, // fruit의 실제 todo 사용
            date = fruit.date, // fruit의 실제 date 사용
            time = "", // time은 fruit에 없으므로 빈 문자열
            acceptedAt = fruit.createdAt,
            emotion = fruit.emotion
        };

        Debug.Log($"[FruitManager] tempTask 생성: id={tempTask.id} (fruit.acceptedTaskId={fruit.acceptedTaskId}), todo={tempTask.todo}, emotion={tempTask.emotion}");

        infoUI.Initialize(tempTask);
        if (infoUI.emojiController != null)
            infoUI.emojiController.SetCurrentFruitInfoUI(infoUI);
        
        // emotion 값 처리
        string emotionToUse = !string.IsNullOrEmpty(fruit.emotion) ? fruit.emotion : "";
        
        // 'none' 값을 빈 문자열로 처리
        if (emotionToUse == "none" || emotionToUse == "NONE")
        {
            emotionToUse = "";
            Debug.Log($"[FruitManager] Fruit {fruit.id}의 'none' 값을 빈 문자열로 변환");
        }
        
        infoUI.currentEmotion = emotionToUse;
        
        // emotion 값이 있으면 EmojiController에 설정
        if (!string.IsNullOrEmpty(emotionToUse))
        {
            StartCoroutine(SetEmotionAfterAwake(obj, emotionToUse));
        }

        currentFruitObjects.Add(obj);
        
        Debug.Log($"[FruitManager] Fruit 생성 완료: id={fruit.id}, taskId={fruit.acceptedTaskId}, 위치={spawnPos}, emotion={emotionToUse}");
    }

    // 현재 활성화된 과일들 제거
    private void ClearCurrentFruits()
    {
        Debug.Log($"[FruitManager] 기존 과일 {currentFruitObjects.Count}개 제거");
        
        foreach (var fruitObj in currentFruitObjects)
        {
            if (fruitObj != null)
            {
                DestroyImmediate(fruitObj);
            }
        }
        currentFruitObjects.Clear();
    }

    private IEnumerator HandleOneTask(AcceptedTaskData task)
    {
        // 새로운 과일 생성
        Vector3 spawnPos = spawnPoint != null
            ? GetBasketSpawnPosition(spawnedFruits.Count)
            : transform.position;
        
        Debug.Log($"[FruitManager] 새 과일 스폰 위치: {spawnPos}");

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
        
        Debug.Log($"[FruitManager] 새 Fruit 인스턴스 생성 완료: {obj.name}");

        obj.transform.SetParent(transform, worldPositionStays: true);
        obj.transform.rotation = Quaternion.identity;

        var infoUI = obj.GetComponent<FruitInfoUI>()
                     ?? obj.AddComponent<FruitInfoUI>();
        
        Debug.Log($"[FruitManager] FruitInfoUI 컴포넌트: {(infoUI != null ? "성공" : "NULL!")}");

        // EmojiController 찾아서 FruitInfoUI에 설정
        var emojiCtrl = obj.GetComponent<EmojiController>();
        if (emojiCtrl == null)
        {
            emojiCtrl = obj.GetComponentInChildren<EmojiController>(false);
        }
        
        if (emojiCtrl != null && infoUI.emojiController == null)
        {
            infoUI.emojiController = emojiCtrl;
        }

        infoUI.Initialize(task);
        if (infoUI.emojiController != null)
            infoUI.emojiController.SetCurrentFruitInfoUI(infoUI);
        
        // task에서 emotion 값 사용
        string emotionToUse = !string.IsNullOrEmpty(task.emotion) ? task.emotion : "";
        
        // 'none' 값을 빈 문자열로 처리
        if (emotionToUse == "none" || emotionToUse == "NONE")
        {
            emotionToUse = "";
            Debug.Log($"[FruitManager] Task {task.id}의 'none' 값을 빈 문자열로 변환");
        }
        
        infoUI.currentEmotion = emotionToUse;
        
        // emotion 값이 있으면 EmojiController에 설정
        if (!string.IsNullOrEmpty(emotionToUse))
        {
            StartCoroutine(SetEmotionAfterAwake(obj, emotionToUse));
        }

        spawnedFruits.Add(new FruitData
        {
            acceptedTaskId = task.id,
            posX = spawnPos.x,
            posY = spawnPos.y,
            posZ = spawnPos.z,
            emotion = emotionToUse
        });
        
        Debug.Log($"[FruitManager] 새 Fruit 생성 완료: taskId={task.id}, 위치={spawnPos}, emotion={emotionToUse}");
    }

    public void OnSaveButtonPressed()
    {
        // 새로운 전체 저장 방식 사용
        var fruitSaver = FindObjectOfType<FruitSaver>();
        if (fruitSaver != null)
        {
            fruitSaver.SaveAllFruitsInScene();
        }
        else
        {
            Debug.LogWarning("[FruitManager] FruitSaver를 찾을 수 없습니다. 기존 방식으로 저장합니다.");
            StartCoroutine(SaveAllSpawnedFruits());
        }
    }

    private IEnumerator SaveAllSpawnedFruits()
    {
        foreach (var fruit in spawnedFruits)
        {
            // nullable float를 안전하게 처리
            float x = fruit.posX ?? 0f;
            float y = fruit.posY ?? 0f;
            float z = fruit.posZ ?? 0f;
            
            yield return StartCoroutine(CreateFruit(
                fruit.acceptedTaskId, new Vector3(x, y, z)));
        }

        Debug.Log("✅ 모든 fruit 저장 완료");
        spawnedFruits.Clear();
    }

    private IEnumerator CreateFruit(long taskId, Vector3 pos)
    {
        var dto = new FruitData
        {
            acceptedTaskId = taskId,
            posX = pos.x,
            posY = pos.y,
            posZ = pos.z
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

    // 층마다 랜더 쌓임 위치 생성 함수
    private Vector3 GetBasketSpawnPosition(int fruitIndex)
    {
        // spawnPoint가 null이면 현재 transform 사용 (Start에서 이미 처리했지만 안전장치)
        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;
        
        // basePos가 (0,0,0)이면 기본 위치 설정
        if (basePos == Vector3.zero)
        {
            Debug.LogWarning("[FruitManager] basePos가 (0,0,0)입니다! 기본 위치 (0,1,0)으로 설정합니다.");
            basePos = new Vector3(0, 1, 0);
        }
        
        int layer = fruitIndex / fruitsPerLayer;
        float x = basePos.x + UnityEngine.Random.Range(-spawnRange.x, spawnRange.x);
        float z = basePos.z + UnityEngine.Random.Range(-spawnRange.z, spawnRange.z);
        float y = basePos.y + (layer * fruitHeight) + UnityEngine.Random.Range(-0.1f, 0.1f);
        
        Debug.Log($"[FruitManager] GetBasketSpawnPosition 계산:");
        Debug.Log($"[FruitManager]   - fruitIndex: {fruitIndex}");
        Debug.Log($"[FruitManager]   - basePos: {basePos} (spawnPoint: {(spawnPoint != null ? "사용" : "transform.position 사용")})");
        Debug.Log($"[FruitManager]   - layer: {layer} (fruitsPerLayer: {fruitsPerLayer})");
        Debug.Log($"[FruitManager]   - spawnRange: {spawnRange}");
        Debug.Log($"[FruitManager]   - fruitHeight: {fruitHeight}");
        Debug.Log($"[FruitManager]   - 최종 위치: ({x}, {y}, {z})");
        
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Awake 완료 후 SetEmotion 호출을 위한 코루틴
    /// </summary>
    private IEnumerator SetEmotionAfterAwake(GameObject fruitObj, string emotion)
    {
        // 한 프레임 대기하여 Awake 완료 보장
        yield return null;
        
        // FruitInfoUI에서 참조하는 EmojiController 사용 (가장 확실한 방법)
        var fruitInfoUI = fruitObj.GetComponent<FruitInfoUI>();
        if (fruitInfoUI != null && fruitInfoUI.emojiController != null)
        {
            fruitInfoUI.emojiController.SetEmotion(emotion);
        }
        else
        {
            // 백업 방법: 메인 과일 오브젝트에서만 EmojiController 찾기
            var emojiCtrl = fruitObj.GetComponent<EmojiController>();
            if (emojiCtrl == null)
            {
                // 직접 자식에서만 찾기
                emojiCtrl = fruitObj.GetComponentInChildren<EmojiController>(false); // false = 직접 자식만
            }
            
            if (emojiCtrl != null)
            {
                emojiCtrl.SetEmotion(emotion);
            }
        }
    }

    // 주차 키 생성을 위한 공통 메서드 (TreeController와 공유)
    public static string GenerateWeekKey(string weekStart, string weekEnd)
    {
        // 날짜 형식을 정규화 (시간 부분 제거)
        string normalizedStart = weekStart?.Split('T')[0] ?? weekStart;
        string normalizedEnd = weekEnd?.Split('T')[0] ?? weekEnd;
        
        string weekKey = $"{normalizedStart} ~ {normalizedEnd}";
        Debug.Log($"[FruitManager] 주차 키 생성: '{weekStart}' + '{weekEnd}' → '{weekKey}'");
        return weekKey;
    }
}


