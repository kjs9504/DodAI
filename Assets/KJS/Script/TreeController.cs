using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

public class TreeController : MonoBehaviour
{
    [Header("백엔드 설정")]
    [SerializeField] private string backendUrl = "http://localhost:8080/api/solution/trees";
    
    [Header("감정별 오브젝트")]
    [SerializeField] private GameObject funObject;
    [SerializeField] private GameObject angryObject;
    [SerializeField] private GameObject sadObject;
    [SerializeField] private GameObject frustrationObject;
    [SerializeField] private GameObject achievementObject;
    
    [Header("UI 텍스트")]
    [SerializeField] private TextMeshProUGUI solutionText;
    
    [Header("버튼")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private float buttonSpacing = 10f;
    
    [Header("FruitManager 참조")]
    [SerializeField] private FruitManager fruitManager;
    
    private TreeData currentTreeData;
    private Dictionary<string, GameObject> emotionObjects;
    private List<Button> generatedButtons = new List<Button>();
    private List<TextMeshProUGUI> generatedGoodPointsTexts = new List<TextMeshProUGUI>();
    private List<WeekData> weeksDataList = new List<WeekData>();
    private WeeksData currentWeeksData;
    private int currentSelectedWeekIndex = -1;
    
    void Start()
    {
        Debug.Log("TreeController Start() 호출됨");
        
        // Scene에 TreeController가 몇 개 있는지 확인
        var allTreeControllers = FindObjectsOfType<TreeController>();
        Debug.Log($"[TreeController] Scene에 TreeController {allTreeControllers.Length}개 발견");
        for (int i = 0; i < allTreeControllers.Length; i++)
        {
            Debug.Log($"[TreeController] TreeController {i}: {allTreeControllers[i].gameObject.name}");
        }
        
        Debug.Log($"buttonPrefab: {(buttonPrefab != null ? "할당됨" : "할당되지 않음")}");
        Debug.Log($"buttonParent: {(buttonParent != null ? "할당됨" : "할당되지 않음")}");
        Debug.Log($"fruitManager: {(fruitManager != null ? "할당됨" : "할당되지 않음")}");
        
        InitializeEmotionObjects();
        StartCoroutine(FetchTreeDataFromBackend());
    }
    
    // 백엔드에서 트리 데이터 가져오기
    private IEnumerator FetchTreeDataFromBackend()
    {
        Debug.Log($"🌐 백엔드 요청 시작: {backendUrl}");
        
        using (UnityWebRequest www = UnityWebRequest.Get(backendUrl))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                Debug.Log($"✅ 백엔드 응답 성공: {json}");
                ProcessBackendData(json);
            }
            else
            {
                Debug.LogError($"❌ 백엔드 요청 실패: {www.error} (응답코드: {www.responseCode})");
                // 백엔드 요청 실패 시 빈 상태로 유지
                Debug.Log("백엔드 요청 실패로 데이터를 가져올 수 없습니다.");
            }
        }
    }
    
    // 백엔드 데이터 처리
    private void ProcessBackendData(string json)
    {
        try
        {
            // 1. 새로운 단일 객체 구조로 파싱 시도
            Debug.Log("새로운 단일 객체 구조로 파싱 시도");
            var newData = JsonUtility.FromJson<NewDataStructure>(json);
            
            if (newData != null && newData.trees != null && newData.trees.Count > 0)
            {
                Debug.Log($"새로운 단일 객체 구조로 파싱 성공:");
                Debug.Log($"Week: {newData.weekStartDate} ~ {newData.weekEndDate}");
                Debug.Log($"Trees 개수: {newData.trees.Count}");
                Debug.Log($"Fruits 개수: {newData.fruits?.Count ?? 0}");
                Debug.Log($"Tasks 개수: {newData.tasks?.Count ?? 0}");
                
                // 단일 객체 구조용 버튼 생성
                GenerateButtonsFromNewDataStructure(newData);
                
                // 자동으로 첫 번째 데이터 선택
                SelectFirstTreeData(newData);
                
                // 성공했으므로 다른 파싱 방식 시도하지 않음
                return;
            }
            
            // 2. 새로운 WeeksData 구조로 파싱 시도
            Debug.Log("WeeksData 구조로 파싱 시도");
            currentWeeksData = JsonUtility.FromJson<WeeksData>(json);
            
            if (currentWeeksData != null && currentWeeksData.weeks != null)
            {
                Debug.Log($"WeeksData 구조로 파싱 성공:");
                Debug.Log($"Weeks 개수: {currentWeeksData.weeks.Count}");
                
                weeksDataList.Clear();
                weeksDataList.AddRange(currentWeeksData.weeks);
                
                Debug.Log($"[TreeController] 총 {weeksDataList.Count}개의 weeks 처리 시작:");
                for (int i = 0; i < weeksDataList.Count; i++)
                {
                    var week = weeksDataList[i];
                    Debug.Log($"[TreeController] Week {i}: {week.weekStart} ~ {week.weekEnd}");
                    Debug.Log($"[TreeController]   - Tree: {(week.tree != null ? $"{week.tree.date} - {week.tree.emotion}" : "NULL")}");
                    Debug.Log($"[TreeController]   - Fruits: {week.fruits?.Count ?? 0}개");
                }
                
                GenerateButtonsFromWeeksData();
                
                // 가장 최근 주의 데이터를 자동으로 선택
                SelectMostRecentWeek();
                
                // 성공했으므로 다른 파싱 방식 시도하지 않음
                return;
            }
            else
            {
                // 3. 기존 구조들로 시도
                Debug.LogWarning("기존 구조들로 시도");
                
                // 먼저 JSON이 배열 형태인지 확인
                if (json.Trim().StartsWith("["))
                {
                    Debug.Log("JSON 배열 형태로 받음 - 기존 TreeData 배열 구조로 파싱");
                    
                    // JSON 배열을 개별 객체로 분리하여 처리
                    string[] jsonArray = ParseJsonArray(json);
                    List<TreeData> treeDataList = new List<TreeData>();
                    
                    foreach (string itemJson in jsonArray)
                    {
                        TreeData treeData = JsonUtility.FromJson<TreeData>(itemJson);
                        if (treeData != null)
                        {
                            treeDataList.Add(treeData);
                            Debug.Log($"트리 데이터 추가: {treeData.emotion} - {treeData.date}");
                        }
                    }
                    
                    if (treeDataList.Count > 0)
                    {
                        Debug.Log($"기존 TreeData 배열 구조로 파싱 성공: {treeDataList.Count}개 trees");
                        // 기존 구조용 버튼 생성 (호환성)
                        GenerateButtonsFromTreeDataList(treeDataList);
                        SelectMostRecentTreeData(treeDataList);
                        
                        // 성공했으므로 다른 파싱 방식 시도하지 않음
                        return;
                    }
                    else
                    {
                        Debug.LogError("JSON 배열 파싱 실패");
                    }
                }
                else
                {
                    // 단일 TreeData 객체로 시도
                    Debug.LogWarning("단일 TreeData 객체로 시도");
                    TreeData treeData = JsonUtility.FromJson<TreeData>(json);
                    if (treeData != null)
                    {
                        List<TreeData> treeDataList = new List<TreeData> { treeData };
                        Debug.Log($"단일 TreeData 객체 파싱 성공");
                        GenerateButtonsFromTreeDataList(treeDataList);
                        SelectMostRecentTreeData(treeDataList);
                        
                        // 성공했으므로 다른 파싱 방식 시도하지 않음
                        return;
                    }
                    else
                    {
                        Debug.LogError("모든 JSON 파싱 시도 실패");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"백엔드 데이터 처리 오류: {e.Message}");
        }
    }
    
    // 새로운 단일 객체 구조로 버튼 생성
    private void GenerateButtonsFromNewDataStructure(NewDataStructure newData)
    {
        Debug.Log($"GenerateButtonsFromNewDataStructure() 호출됨 - trees 개수: {newData.trees?.Count ?? 0}");
        
        if (buttonPrefab == null)
        {
            Debug.LogError("버튼 프리팹이 할당되지 않았습니다!");
            return;
        }
        
        if (buttonParent == null)
        {
            Debug.LogError("버튼 부모가 할당되지 않았습니다!");
            return;
        }
        
        // 기존 생성된 버튼들 제거
        ClearGeneratedButtons();
        
        // 각 tree에 대해 버튼 생성
        int buttonIndex = 0;
        
        for (int i = 0; i < newData.trees.Count; i++)
        {
            var tree = newData.trees[i];
            
            Debug.Log($"Tree 버튼 {buttonIndex} 생성 중... (Tree {i}: {tree.emotion} - {tree.date})");
            
            GameObject buttonObj = Instantiate(buttonPrefab, buttonParent);
            Button button = buttonObj.GetComponent<Button>();
            
            if (button != null)
            {
                // 버튼 텍스트 설정 (tree의 emotion 사용)
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = tree.emotion;
                }
                
                // Date 텍스트 설정 (week 범위 표시)
                Transform dateTransform = buttonObj.transform.Find("Date");
                if (dateTransform != null)
                {
                    TextMeshProUGUI dateText = dateTransform.GetComponent<TextMeshProUGUI>();
                    if (dateText != null)
                    {
                        dateText.text = $"{newData.weekStartDate} ~ {newData.weekEndDate}";
                    }
                    else
                    {
                        TextMeshProUGUI childDateText = dateTransform.GetComponentInChildren<TextMeshProUGUI>();
                        if (childDateText != null)
                        {
                            childDateText.text = $"{newData.weekStartDate} ~ {newData.weekEndDate}";
                        }
                    }
                    
                    // GoodPoint 설정 (tree의 goodPoints 사용)
                    Transform goodPointTransform = dateTransform.Find("GoodPoint");
                    if (goodPointTransform != null)
                    {
                        TextMeshProUGUI goodPointText = goodPointTransform.GetComponent<TextMeshProUGUI>();
                        if (goodPointText != null)
                        {
                            goodPointText.text = tree.goodPoints;
                            generatedGoodPointsTexts.Add(goodPointText);
                        }
                        else
                        {
                            generatedGoodPointsTexts.Add(null);
                        }
                    }
                    else
                    {
                        generatedGoodPointsTexts.Add(null);
                    }
                }
                else
                {
                    generatedGoodPointsTexts.Add(null);
                }
                
                // 버튼 클릭 이벤트 설정 (실제 tree 인덱스 사용)
                int treeIndex = i;
                button.onClick.AddListener(() => OnNewDataStructureButtonClick(newData, treeIndex));
                
                // 버튼 위치 설정
                RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(0, -buttonIndex * (rectTransform.rect.height + buttonSpacing));
                }
                
                generatedButtons.Add(button);
                buttonIndex++;
                
                Debug.Log($"버튼 생성 완료! Tree {i} ({tree.emotion} - {tree.date})");
            }
        }
        
        Debug.Log($"총 {generatedButtons.Count}개의 Tree 버튼이 생성되었습니다.");
    }
    
    // 새로운 단일 객체 구조 버튼 클릭 시 호출
    private void OnNewDataStructureButtonClick(NewDataStructure newData, int treeIndex)
    {
        Debug.Log($"NewDataStructure 버튼 {treeIndex} 클릭됨");
        
        if (treeIndex >= 0 && treeIndex < newData.trees.Count)
        {
            var selectedTree = newData.trees[treeIndex];
            
            // Tree 데이터 설정
            currentTreeData = selectedTree;
            UpdateUI();
            OnEmotionButtonClick();
            Debug.Log($"Tree {treeIndex} 활성화: {selectedTree.emotion}");
            
            // Fruits 활성화 (FruitManager에 전달) - 코루틴으로 호출
            if (fruitManager != null && newData.fruits != null)
            {
                StartCoroutine(fruitManager.ActivateWeekFruits(newData.fruits));
                Debug.Log($"Fruits 활성화 시작: {newData.fruits.Count}개");
            }
            else
            {
                Debug.LogWarning($"FruitManager가 없거나 fruits가 null입니다. treeIndex: {treeIndex}");
            }
            
            Debug.Log($"NewDataStructure 버튼 {treeIndex} 처리 완료");
        }
        else
        {
            Debug.LogError($"잘못된 Tree 버튼 인덱스: {treeIndex}");
        }
    }
    
    // 새로운 단일 객체 구조의 첫 번째 데이터 선택
    private void SelectFirstTreeData(NewDataStructure newData)
    {
        if (newData.trees == null || newData.trees.Count == 0)
        {
            Debug.LogWarning("선택할 Tree 데이터가 없습니다.");
            return;
        }
        
        // 첫 번째 tree를 자동으로 선택
        OnNewDataStructureButtonClick(newData, 0);
        Debug.Log($"첫 번째 Tree가 자동 선택되었습니다: {newData.trees[0].emotion} - {newData.trees[0].date}");
    }

    // 새로운 WeeksData 구조로 버튼 생성
    private void GenerateButtonsFromWeeksData()
    {
        Debug.Log($"GenerateButtonsFromWeeksData() 호출됨 - weeks 개수: {weeksDataList.Count}");
        
        if (buttonPrefab == null)
        {
            Debug.LogError("버튼 프리팹이 할당되지 않았습니다!");
            return;
        }
        
        if (buttonParent == null)
        {
            Debug.LogError("버튼 부모가 할당되지 않았습니다!");
            return;
        }
        
        // 기존 생성된 버튼들 제거
        ClearGeneratedButtons();
        
        // tree가 있는 모든 week에 대해 버튼 생성
        int buttonIndex = 0;
        
        for (int i = 0; i < weeksDataList.Count; i++)
        {
            var week = weeksDataList[i];
            
            Debug.Log($"[TreeController] Week {i} 검사: tree = {(week.tree != null ? "있음" : "NULL")}");
            
            // tree가 null이면 버튼 생성하지 않음
            if (week.tree == null)
            {
                Debug.Log($"Week {i} ({week.weekStart} ~ {week.weekEnd}) - tree가 null이므로 버튼 생성하지 않음");
                continue;
            }
            
            Debug.Log($"Week 버튼 {buttonIndex} 생성 중... (Week {i}: {week.weekStart} ~ {week.weekEnd})");
            
            GameObject buttonObj = Instantiate(buttonPrefab, buttonParent);
            Button button = buttonObj.GetComponent<Button>();
            
            if (button != null)
            {
                // 버튼 텍스트 설정 (tree의 emotion 사용)
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null && week.tree != null)
                {
                    buttonText.text = week.tree.emotion;
                }
                
                // Date 텍스트 설정 (week 범위 표시)
                Transform dateTransform = buttonObj.transform.Find("Date");
                if (dateTransform != null)
                {
                    TextMeshProUGUI dateText = dateTransform.GetComponent<TextMeshProUGUI>();
                    if (dateText != null)
                    {
                        dateText.text = $"{week.weekStart} ~ {week.weekEnd}";
                    }
                    else
                    {
                        TextMeshProUGUI childDateText = dateTransform.GetComponentInChildren<TextMeshProUGUI>();
                        if (childDateText != null)
                        {
                            childDateText.text = $"{week.weekStart} ~ {week.weekEnd}";
                        }
                    }
                    
                    // GoodPoint 설정 (tree의 goodPoints 사용)
                    Transform goodPointTransform = dateTransform.Find("GoodPoint");
                    if (goodPointTransform != null && week.tree != null)
                    {
                        TextMeshProUGUI goodPointText = goodPointTransform.GetComponent<TextMeshProUGUI>();
                        if (goodPointText != null)
                        {
                            goodPointText.text = week.tree.goodPoints;
                            generatedGoodPointsTexts.Add(goodPointText);
                        }
                        else
                        {
                            generatedGoodPointsTexts.Add(null);
                        }
                    }
                    else
                    {
                        generatedGoodPointsTexts.Add(null);
                    }
                }
                else
                {
                    generatedGoodPointsTexts.Add(null);
                }
                
                // 버튼 클릭 이벤트 설정 (실제 week 인덱스 사용)
                int weekIndex = i;
                button.onClick.AddListener(() => OnWeekButtonClick(weekIndex));
                
                // 버튼 위치 설정
                RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(0, -buttonIndex * (rectTransform.rect.height + buttonSpacing));
                }
                
                generatedButtons.Add(button);
                buttonIndex++;
                
                Debug.Log($"버튼 생성 완료! Week {i} ({week.weekStart} ~ {week.weekEnd})");
            }
        }
        
        Debug.Log($"총 {generatedButtons.Count}개의 Week 버튼이 생성되었습니다.");
    }
    
    // Week 버튼 클릭 시 호출
    private void OnWeekButtonClick(int weekIndex)
    {
        Debug.Log($"Week 버튼 {weekIndex} 클릭됨");
        
        if (weekIndex >= 0 && weekIndex < weeksDataList.Count)
        {
            currentSelectedWeekIndex = weekIndex;
            var selectedWeek = weeksDataList[weekIndex];
            
            // Tree 데이터 설정
            if (selectedWeek.tree != null)
            {
                currentTreeData = selectedWeek.tree;
                UpdateUI();
                OnEmotionButtonClick();
                Debug.Log($"Week {weekIndex}의 Tree 활성화: {selectedWeek.tree.emotion}");
            }
            
            // Fruits 활성화 (FruitManager에 전달) - 코루틴으로 호출
            if (fruitManager != null && selectedWeek.fruits != null)
            {
                StartCoroutine(fruitManager.ActivateWeekFruits(selectedWeek.fruits));
                Debug.Log($"Week {weekIndex}의 Fruits 활성화 시작: {selectedWeek.fruits.Count}개");
            }
            else
            {
                Debug.LogWarning($"FruitManager가 없거나 fruits가 null입니다. weekIndex: {weekIndex}");
            }
            
            Debug.Log($"Week 버튼 {weekIndex} 처리 완료");
        }
        else
        {
            Debug.LogError($"잘못된 Week 버튼 인덱스: {weekIndex}");
        }
    }
    
    // 기존 TreeData 배열 구조용 버튼 생성 (호환성)
    private void GenerateButtonsFromTreeDataList(List<TreeData> treeDataList)
    {
        Debug.Log($"GenerateButtonsFromTreeDataList() 호출됨 - TreeData 개수: {treeDataList.Count}");
        
        if (buttonPrefab == null || buttonParent == null)
        {
            Debug.LogError("버튼 프리팹 또는 부모가 할당되지 않았습니다!");
            return;
        }
        
        ClearGeneratedButtons();
        
        for (int i = 0; i < treeDataList.Count; i++)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, buttonParent);
            Button button = buttonObj.GetComponent<Button>();
            
            if (button != null)
            {
                var treeData = treeDataList[i];
                
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = treeData.emotion;
                }
                
                Transform dateTransform = buttonObj.transform.Find("Date");
                if (dateTransform != null)
                {
                    TextMeshProUGUI dateText = dateTransform.GetComponent<TextMeshProUGUI>();
                    if (dateText != null)
                    {
                        dateText.text = treeData.date;
                    }
                    
                    Transform goodPointTransform = dateTransform.Find("GoodPoint");
                    if (goodPointTransform != null)
                    {
                        TextMeshProUGUI goodPointText = goodPointTransform.GetComponent<TextMeshProUGUI>();
                        if (goodPointText != null)
                        {
                            goodPointText.text = treeData.goodPoints;
                            generatedGoodPointsTexts.Add(goodPointText);
                        }
                    }
                }
                
                int index = i;
                button.onClick.AddListener(() => OnTreeDataButtonClick(treeDataList, index));
                
                RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(0, -i * (rectTransform.rect.height + buttonSpacing));
                }
                
                generatedButtons.Add(button);
            }
        }
    }
    
    // 기존 TreeData 버튼 클릭 (호환성)
    private void OnTreeDataButtonClick(List<TreeData> treeDataList, int index)
    {
        if (index >= 0 && index < treeDataList.Count)
        {
            currentTreeData = treeDataList[index];
            UpdateUI();
            OnEmotionButtonClick();
            Debug.Log($"기존 TreeData 버튼 {index} 처리 완료");
        }
    }
    
    // 가장 최근 주 선택
    private void SelectMostRecentWeek()
    {
        if (weeksDataList == null || weeksDataList.Count == 0)
        {
            Debug.LogWarning("선택할 Week 데이터가 없습니다.");
            return;
        }
        
        // 가장 최근 주 찾기 (weekEnd 기준)
        WeekData mostRecentWeek = null;
        System.DateTime mostRecentDate = System.DateTime.MinValue;
        int mostRecentIndex = -1;
        
        for (int i = 0; i < weeksDataList.Count; i++)
        {
            var week = weeksDataList[i];
            if (System.DateTime.TryParse(week.weekEnd, out System.DateTime weekEndDate))
            {
                if (weekEndDate > mostRecentDate)
                {
                    mostRecentDate = weekEndDate;
                    mostRecentWeek = week;
                    mostRecentIndex = i;
                }
            }
        }
        
        if (mostRecentWeek != null && mostRecentIndex >= 0)
        {
            // 가장 최근 주를 자동으로 선택
            OnWeekButtonClick(mostRecentIndex);
            Debug.Log($"가장 최근 주가 자동 선택되었습니다: {mostRecentWeek.weekStart} ~ {mostRecentWeek.weekEnd}");
        }
        else
        {
            Debug.LogWarning("유효한 Week 데이터를 찾을 수 없습니다.");
        }
    }
    
    // 기존 TreeData용 최근 선택 (호환성)
    private void SelectMostRecentTreeData(List<TreeData> treeDataList)
    {
        if (treeDataList == null || treeDataList.Count == 0)
        {
            Debug.LogWarning("선택할 TreeData가 없습니다.");
            return;
        }
        
        TreeData mostRecentData = null;
        System.DateTime mostRecentDate = System.DateTime.MinValue;
        int mostRecentIndex = -1;
        
        for (int i = 0; i < treeDataList.Count; i++)
        {
            var treeData = treeDataList[i];
            if (System.DateTime.TryParse(treeData.date, out System.DateTime currentDate))
            {
                if (currentDate > mostRecentDate)
                {
                    mostRecentDate = currentDate;
                    mostRecentData = treeData;
                    mostRecentIndex = i;
                }
            }
        }
        
        if (mostRecentData != null && mostRecentIndex >= 0)
        {
            OnTreeDataButtonClick(treeDataList, mostRecentIndex);
            Debug.Log($"가장 최근 TreeData가 자동 선택되었습니다: {mostRecentData.date} - {mostRecentData.emotion}");
        }
    }
    
    // JSON 배열을 개별 문자열로 분리 (기존 구조용)
    private string[] ParseJsonArray(string jsonArray)
    {
        List<string> items = new List<string>();
        int braceCount = 0;
        int startIndex = -1;
        
        for (int i = 0; i < jsonArray.Length; i++)
        {
            char c = jsonArray[i];
            
            if (c == '{')
            {
                if (braceCount == 0)
                {
                    startIndex = i;
                }
                braceCount++;
            }
            else if (c == '}')
            {
                braceCount--;
                if (braceCount == 0 && startIndex != -1)
                {
                    string item = jsonArray.Substring(startIndex, i - startIndex + 1);
                    items.Add(item);
                    startIndex = -1;
                }
            }
        }
        
        return items.ToArray();
    }
    
    // 백엔드 URL 설정 메서드
    public void SetBackendUrl(string url)
    {
        backendUrl = url;
        Debug.Log($"백엔드 URL이 변경되었습니다: {backendUrl}");
    }
    
    // 백엔드에서 데이터 다시 가져오기
    [ContextMenu("백엔드에서 데이터 새로고침")]
    public void RefreshFromBackend()
    {
        StartCoroutine(FetchTreeDataFromBackend());
    }
    
    void InitializeEmotionObjects()
    {
        emotionObjects = new Dictionary<string, GameObject>
        {
            {"즐거움", funObject},
            {"분노", angryObject},
            {"슬픔", sadObject},
            {"허무감", frustrationObject},
            {"달성감", achievementObject},
            {"happy", funObject}, // 영어 감정 추가
            {"proud", achievementObject},
            {"angry", angryObject},
            {"sad", sadObject},
            {"frustration", frustrationObject}
        };
        
        // 모든 오브젝트를 초기에 비활성화
        foreach (var obj in emotionObjects.Values)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
    
    // 생성된 버튼들 제거
    void ClearGeneratedButtons()
    {
        Debug.Log($"[TreeController] 기존 버튼 {generatedButtons.Count}개 제거 시작");
        
        foreach (Button button in generatedButtons)
        {
            if (button != null)
            {
                Debug.Log($"[TreeController] 버튼 제거: {button.name}");
                DestroyImmediate(button.gameObject);
            }
        }
        generatedButtons.Clear();
        generatedGoodPointsTexts.Clear();
        
        Debug.Log("[TreeController] 기존 버튼 제거 완료");
    }
    
    // JSON 데이터를 받아서 처리하는 메서드 (기존 호환성용)
    public void SetTreeData(string jsonData)
    {
        try
        {
            currentTreeData = JsonUtility.FromJson<TreeData>(jsonData);
            UpdateUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON 파싱 오류: {e.Message}");
        }
    }
    
    // TreeData 객체로 직접 설정하는 메서드
    public void SetTreeData(TreeData data)
    {
        currentTreeData = data;
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (currentTreeData == null) return;
        
        // 텍스트 업데이트
        if (solutionText != null)
        {
            solutionText.text = currentTreeData.solution;
        }
    }
    
    // 버튼 클릭 시 호출되는 메서드
    public void OnEmotionButtonClick()
    {
        if (currentTreeData == null) return;
        
        // 모든 감정 오브젝트를 비활성화
        DeactivateAllEmotionObjects();
        
        // 현재 감정에 해당하는 오브젝트 활성화
        string emotion = currentTreeData.emotion;
        if (emotionObjects.ContainsKey(emotion))
        {
            GameObject targetObject = emotionObjects[emotion];
            if (targetObject != null)
            {
                targetObject.SetActive(true);
                Debug.Log($"감정 오브젝트 활성화: {emotion}");
            }
            else
            {
                Debug.LogWarning($"감정 '{emotion}'에 해당하는 오브젝트가 할당되지 않았습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"알 수 없는 감정: {emotion}");
        }
    }
    
    void DeactivateAllEmotionObjects()
    {
        foreach (var obj in emotionObjects.Values)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
    
    // 현재 활성화된 감정 오브젝트 확인
    public string GetCurrentActiveEmotion()
    {
        foreach (var kvp in emotionObjects)
        {
            if (kvp.Value != null && kvp.Value.activeInHierarchy)
            {
                return kvp.Key;
            }
        }
        return "NONE";
    }
} 