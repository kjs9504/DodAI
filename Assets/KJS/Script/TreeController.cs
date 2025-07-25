using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

[System.Serializable]
public class TreeData
{
    public string date;
    public string emotion;
    public string goodPoints;
    public string solution;
}

[System.Serializable]
public class TreeDataList
{
    public List<TreeData> trees;
}

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
    
    private TreeData currentTreeData;
    private Dictionary<string, GameObject> emotionObjects;
    private List<Button> generatedButtons = new List<Button>();
    private List<TextMeshProUGUI> generatedGoodPointsTexts = new List<TextMeshProUGUI>();
    private List<TreeData> treeDataList = new List<TreeData>();
    
    void Start()
    {
        Debug.Log("TreeController Start() 호출됨");
        Debug.Log($"buttonPrefab: {(buttonPrefab != null ? "할당됨" : "할당되지 않음")}");
        Debug.Log($"buttonParent: {(buttonParent != null ? "할당됨" : "할당되지 않음")}");
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
                // 백엔드 요청 실패 시 더미 데이터 사용
                Debug.Log("더미 데이터로 폴백합니다.");
                GenerateButtonsFromDummyData();
            }
        }
    }
    
    // 백엔드 데이터 처리
    private void ProcessBackendData(string json)
    {
        try
        {
            // 백엔드에서 받은 JSON을 TreeData 리스트로 변환
            treeDataList.Clear();
            
            // JSON 배열 형태로 받은 경우
            if (json.Trim().StartsWith("["))
            {
                // JSON 배열을 개별 객체로 분리하여 처리
                string[] jsonArray = ParseJsonArray(json);
                foreach (string itemJson in jsonArray)
                {
                    TreeData treeData = JsonUtility.FromJson<TreeData>(itemJson);
                    if (treeData != null)
                    {
                        treeDataList.Add(treeData);
                        Debug.Log($"트리 데이터 추가: {treeData.emotion} - {treeData.date}");
                    }
                }
            }
            else
            {
                // 단일 객체인 경우
                TreeData treeData = JsonUtility.FromJson<TreeData>(json);
                if (treeData != null)
                {
                    treeDataList.Add(treeData);
                }
            }
            
            if (treeDataList.Count > 0)
            {
                Debug.Log($"총 {treeDataList.Count}개의 트리 데이터를 받았습니다.");
                GenerateButtonsFromBackendData();
            }
            else
            {
                Debug.LogWarning("백엔드에서 받은 데이터가 없습니다. 더미 데이터를 사용합니다.");
                GenerateButtonsFromDummyData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"백엔드 데이터 처리 오류: {e.Message}");
            GenerateButtonsFromDummyData();
        }
    }
    
    // JSON 배열을 개별 문자열로 분리
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
    
    // 백엔드 데이터로 버튼 생성
    private void GenerateButtonsFromBackendData()
    {
        Debug.Log($"GenerateButtonsFromBackendData() 호출됨 - 백엔드 데이터 개수: {treeDataList.Count}");
        
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
        
        // 백엔드 데이터 개수만큼 버튼 생성
        for (int i = 0; i < treeDataList.Count; i++)
        {
            Debug.Log($"버튼 {i} 생성 중...");
            
            GameObject buttonObj = Instantiate(buttonPrefab, buttonParent);
            Button button = buttonObj.GetComponent<Button>();
            
            if (button != null)
            {
                // 버튼 텍스트 설정
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = treeDataList[i].emotion;
                }
                
                // Date 텍스트 설정
                Transform dateTransform = buttonObj.transform.Find("Date");
                if (dateTransform != null)
                {
                    TextMeshProUGUI dateText = dateTransform.GetComponent<TextMeshProUGUI>();
                    if (dateText != null)
                    {
                        dateText.text = treeDataList[i].date;
                    }
                    else
                    {
                        TextMeshProUGUI childDateText = dateTransform.GetComponentInChildren<TextMeshProUGUI>();
                        if (childDateText != null)
                        {
                            childDateText.text = treeDataList[i].date;
                        }
                    }
                    
                    // GoodPoint 설정
                    Transform goodPointTransform = dateTransform.Find("GoodPoint");
                    if (goodPointTransform != null)
                    {
                        TextMeshProUGUI goodPointText = goodPointTransform.GetComponent<TextMeshProUGUI>();
                        if (goodPointText != null)
                        {
                            goodPointText.text = treeDataList[i].goodPoints;
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
                
                // 버튼 클릭 이벤트 설정
                int index = i;
                button.onClick.AddListener(() => OnBackendDataButtonClick(index));
                
                // 버튼 위치 설정
                RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(0, -i * (rectTransform.rect.height + buttonSpacing));
                }
                
                generatedButtons.Add(button);
            }
        }
        
        Debug.Log($"총 {generatedButtons.Count}개의 버튼이 생성되었습니다.");
    }
    
    // 백엔드 데이터 버튼 클릭 시 호출
    private void OnBackendDataButtonClick(int index)
    {
        Debug.Log($"백엔드 데이터 버튼 {index} 클릭됨");
        
        if (index >= 0 && index < treeDataList.Count)
        {
            currentTreeData = treeDataList[index];
            UpdateUI();
            OnEmotionButtonClick();
            Debug.Log($"백엔드 데이터 버튼 {index} 처리 완료");
        }
        else
        {
            Debug.LogError($"잘못된 버튼 인덱스: {index}");
        }
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
            {"FUN", funObject},
            {"ANGRY", angryObject},
            {"SAD", sadObject},
            {"FRUSTRATION", frustrationObject},
            {"ACHIEVEMENT", achievementObject}
        };
        
        // 모든 오브젝트를 초기에 비활성화
        foreach (var obj in emotionObjects.Values)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
    

    
    // 더미 데이터 개수에 따라 버튼 생성
    void GenerateButtonsFromDummyData()
    {
        Debug.Log($"GenerateButtonsFromDummyData() 호출됨 - 더미 데이터 개수: {dummyJsonData.Length}");
        
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
        
        Debug.Log($"버튼 프리팹 이름: {buttonPrefab.name}");
        Debug.Log($"버튼 부모 이름: {buttonParent.name}");
        Debug.Log("버튼 생성 시작...");
        
        // 기존 생성된 버튼들 제거
        ClearGeneratedButtons();
        
        // 더미 데이터 개수만큼 버튼 생성
        for (int i = 0; i < dummyJsonData.Length; i++)
        {
            Debug.Log($"버튼 {i} 생성 중...");
            
            GameObject buttonObj = Instantiate(buttonPrefab, buttonParent);
            Debug.Log($"버튼 {i} 인스턴스 생성됨: {buttonObj.name}");
            
            Button button = buttonObj.GetComponent<Button>();
            
            if (button != null)
            {
                // 버튼 텍스트 설정 (감정 이름으로 설정)
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    TreeData tempData = JsonUtility.FromJson<TreeData>(dummyJsonData[i]);
                    buttonText.text = tempData.emotion; // 버튼에는 감정 이름 표시
                    Debug.Log($"버튼 {i} 텍스트 설정: {tempData.emotion}");
                }
                else
                {
                    Debug.LogError($"버튼 {i}에 TextMeshProUGUI 컴포넌트를 찾을 수 없습니다!");
                }
                
                // GoodPoint 텍스트 찾기 및 저장 (Date -> GoodPoint 구조)
                Transform dateTransform = buttonObj.transform.Find("Date");
                if (dateTransform != null)
                {
                    Debug.Log($"버튼 {i}에서 Date 오브젝트 찾음: {dateTransform.name}");
                    
                    // Date의 모든 자식 오브젝트 확인
                    Debug.Log($"Date의 자식 오브젝트들:");
                    for (int j = 0; j < dateTransform.childCount; j++)
                    {
                        Transform child = dateTransform.GetChild(j);
                        Debug.Log($"  - {child.name}");
                    }
                    
                                    // Date 텍스트 바로 설정
                Debug.Log($"Date 오브젝트 컴포넌트 확인:");
                Component[] dateComponents = dateTransform.GetComponents<Component>();
                foreach (Component comp in dateComponents)
                {
                    Debug.Log($"  - {comp.GetType().Name}");
                }
                
                TextMeshProUGUI dateText = dateTransform.GetComponent<TextMeshProUGUI>();
                if (dateText != null)
                {
                    TreeData tempData = JsonUtility.FromJson<TreeData>(dummyJsonData[i]);
                    dateText.text = tempData.date;
                    Debug.Log($"버튼 {i}의 Date 텍스트 설정 완료: {tempData.date}");
                }
                else
                {
                    Debug.LogError($"버튼 {i}의 Date에 TextMeshProUGUI 컴포넌트가 없습니다!");
                    // Date의 자식에서 TextMeshProUGUI 찾기 시도
                    TextMeshProUGUI childDateText = dateTransform.GetComponentInChildren<TextMeshProUGUI>();
                    if (childDateText != null)
                    {
                        TreeData tempData = JsonUtility.FromJson<TreeData>(dummyJsonData[i]);
                        childDateText.text = tempData.date;
                        Debug.Log($"버튼 {i}의 Date 자식에서 TextMeshProUGUI 찾음, 텍스트 설정 완료: {tempData.date}");
                    }
                    else
                    {
                        Debug.LogError($"버튼 {i}의 Date와 그 자식들에서 TextMeshProUGUI를 찾을 수 없습니다!");
                    }
                }
                
                // GoodPoint 찾기 및 바로 텍스트 설정
                Transform goodPointTransform = dateTransform.Find("GoodPoint");
                if (goodPointTransform != null)
                {
                    Debug.Log($"버튼 {i}에서 GoodPoint 오브젝트 찾음: {goodPointTransform.name}");
                    TextMeshProUGUI goodPointText = goodPointTransform.GetComponent<TextMeshProUGUI>();
                    if (goodPointText != null)
                    {
                        // 바로 해당 JSON의 goodPoints 텍스트 설정
                        TreeData tempData = JsonUtility.FromJson<TreeData>(dummyJsonData[i]);
                        goodPointText.text = tempData.goodPoints;
                        generatedGoodPointsTexts.Add(goodPointText);
                        Debug.Log($"버튼 {i}의 GoodPoint 텍스트 설정 완료: {tempData.goodPoints.Substring(0, Mathf.Min(30, tempData.goodPoints.Length))}...");
                    }
                    else
                    {
                        Debug.LogError($"버튼 {i}의 GoodPoint에 TextMeshProUGUI 컴포넌트가 없습니다!");
                        generatedGoodPointsTexts.Add(null);
                    }
                }
                else
                {
                    Debug.LogError($"버튼 {i}의 Date에서 GoodPoint를 찾을 수 없습니다!");
                    generatedGoodPointsTexts.Add(null);
                }
                }
                else
                {
                    Debug.LogError($"버튼 {i}에서 Date 오브젝트를 찾을 수 없습니다!");
                    generatedGoodPointsTexts.Add(null);
                }
                
                // 버튼 클릭 이벤트 설정
                int index = i; // 클로저를 위한 변수
                button.onClick.AddListener(() => OnDummyDataButtonClick(index));
                
                // 버튼 위치 설정
                RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(0, -i * (rectTransform.rect.height + buttonSpacing));
                    Debug.Log($"버튼 {i} 위치 설정: {rectTransform.anchoredPosition}");
                }
                
                generatedButtons.Add(button);
                Debug.Log($"버튼 {i} 생성 완료");
            }
            else
            {
                Debug.LogError($"버튼 {i}에 Button 컴포넌트를 찾을 수 없습니다!");
            }
        }
        
        Debug.Log($"총 {generatedButtons.Count}개의 버튼이 생성되었습니다.");
    }
    
    // 생성된 버튼들 제거
    void ClearGeneratedButtons()
    {
        foreach (Button button in generatedButtons)
        {
            if (button != null)
            {
                DestroyImmediate(button.gameObject);
            }
        }
        generatedButtons.Clear();
        generatedGoodPointsTexts.Clear();
    }
    
    // 더미 데이터 버튼 클릭 시 호출
    void OnDummyDataButtonClick(int index)
    {
        Debug.Log($"버튼 {index} 클릭됨");
        
        if (index >= 0 && index < dummyJsonData.Length)
        {
            currentDummyIndex = index;
            SetTreeData(dummyJsonData[index]);
            // UpdateGoodPointText(); // GoodPoint는 버튼 생성 시 이미 설정됨
            OnEmotionButtonClick();
            Debug.Log($"버튼 {index} 데이터 처리 완료");
        }
        else
        {
            Debug.LogError($"잘못된 버튼 인덱스: {index}");
        }
    }
    
    // JSON 데이터를 받아서 처리하는 메서드
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
    
    // 현재 선택된 버튼의 GoodPoint 텍스트 업데이트
    void UpdateGoodPointText()
    {
        if (currentTreeData == null)
        {
            Debug.LogError("currentTreeData가 null입니다!");
            return;
        }
        
        if (currentDummyIndex < 0 || currentDummyIndex >= generatedGoodPointsTexts.Count)
        {
            Debug.LogError($"잘못된 인덱스: {currentDummyIndex}, 리스트 크기: {generatedGoodPointsTexts.Count}");
            return;
        }
            
        TextMeshProUGUI goodPointText = generatedGoodPointsTexts[currentDummyIndex];
        if (goodPointText != null)
        {
            goodPointText.text = currentTreeData.goodPoints;
            Debug.Log($"GoodPoint 텍스트 업데이트: {currentTreeData.goodPoints.Substring(0, Mathf.Min(30, currentTreeData.goodPoints.Length))}...");
        }
        else
        {
            Debug.LogError($"버튼 {currentDummyIndex}의 GoodPoint 텍스트가 null입니다!");
        }
    }
    

    
    // 버튼 클릭 시 호출되는 메서드
    public void OnEmotionButtonClick()
    {
        if (currentTreeData == null) return;
        
        // 모든 감정 오브젝트를 비활성화
        DeactivateAllEmotionObjects();
        
        // 현재 감정에 해당하는 오브젝트 활성화
        string emotion = currentTreeData.emotion.ToUpper();
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
    
    // 더미 JSON 데이터들
    [Header("더미 데이터")]
    [SerializeField] private string[] dummyJsonData = {
        @"{
            ""date"": ""2024-01-15"",
            ""emotion"": ""FUN"",
            ""goodPoints"": ""오늘 정말 재미있었어요! 새로운 것을 배우는 게 즐거웠고, 친구들과 함께하는 시간이 행복했어요. 웃음이 끊이지 않는 하루였습니다."",
            ""solution"": ""이런 즐거운 기분을 유지하기 위해 매일 작은 목표를 세우고, 성취감을 느낄 수 있는 활동을 해보세요. 긍정적인 마인드를 유지하는 것이 중요합니다.""
        }",
        @"{
            ""date"": ""2024-01-16"",
            ""emotion"": ""ANGRY"",
            ""goodPoints"": ""화가 나는 상황에서도 침착하게 대응하려고 노력했어요. 감정을 억누르지 않고 적절히 표현하려고 했습니다."",
            ""solution"": ""화가 날 때는 심호흡을 10번 하고, 잠시 자리를 떠나서 마음을 진정시켜보세요. 감정 일기를 쓰는 것도 도움이 됩니다.""
        }",
        @"{
            ""date"": ""2024-01-17"",
            ""emotion"": ""SAD"",
            ""goodPoints"": ""슬픈 감정을 인정하고 받아들이려고 노력했어요. 감정을 숨기지 않고 표현하려고 했습니다."",
            ""solution"": ""슬픈 감정이 들 때는 좋아하는 음악을 듣거나, 신뢰할 수 있는 사람과 대화해보세요. 충분한 휴식과 자기 돌봄이 필요합니다.""
        }",
        @"{
            ""date"": ""2024-01-18"",
            ""emotion"": ""FRUSTRATION"",
            ""goodPoints"": ""좌절감이 들었지만 포기하지 않고 계속 시도하려고 노력했어요. 문제를 해결하려는 의지가 있었습니다."",
            ""solution"": ""좌절감이 들 때는 문제를 작은 단계로 나누어 하나씩 해결해보세요. 다른 관점에서 문제를 바라보는 것도 도움이 됩니다.""
        }",
        @"{
            ""date"": ""2024-01-19"",
            ""emotion"": ""ACHIEVEMENT"",
            ""goodPoints"": ""목표를 달성했을 때의 성취감이 정말 컸어요! 노력한 만큼 결과가 나와서 뿌듯했습니다."",
            ""solution"": ""성취감을 느낄 때는 자신을 칭찬하고, 다음 목표를 세워보세요. 작은 성공도 축하하고 기록하는 습관을 기르세요.""
        }"
    };
    
    [SerializeField] private int currentDummyIndex = 0;
    
    // 테스트용 메서드 (개발 중에 사용)
    [ContextMenu("테스트 데이터로 실행")]
    public void TestWithSampleData()
    {
        if (dummyJsonData.Length > 0)
        {
            SetTreeData(dummyJsonData[currentDummyIndex]);
            OnEmotionButtonClick();
        }
    }
    
    // 다음 더미 데이터로 테스트
    [ContextMenu("다음 더미 데이터로 테스트")]
    public void TestWithNextDummyData()
    {
        currentDummyIndex = (currentDummyIndex + 1) % dummyJsonData.Length;
        TestWithSampleData();
    }
    
    // 특정 감정의 더미 데이터로 테스트
    public void TestWithSpecificEmotion(string emotion)
    {
        for (int i = 0; i < dummyJsonData.Length; i++)
        {
            TreeData tempData = JsonUtility.FromJson<TreeData>(dummyJsonData[i]);
            if (tempData.emotion.ToUpper() == emotion.ToUpper())
            {
                currentDummyIndex = i;
                TestWithSampleData();
                return;
            }
        }
        Debug.LogWarning($"감정 '{emotion}'에 해당하는 더미 데이터를 찾을 수 없습니다.");
    }
    
    // 버튼 재생성 (더미 데이터가 변경되었을 때)
    [ContextMenu("버튼 재생성")]
    public void RegenerateButtons()
    {
        GenerateButtonsFromDummyData();
    }
    
    // 외부에서 JSON 데이터 배열을 설정하고 버튼 생성
    public void SetDummyDataAndGenerateButtons(string[] newDummyData)
    {
        dummyJsonData = newDummyData;
        GenerateButtonsFromDummyData();
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