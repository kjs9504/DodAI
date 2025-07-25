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
                
                // 가장 최근 날짜의 데이터를 자동으로 선택
                SelectMostRecentData();
            }
            else
            {
                Debug.LogWarning("백엔드에서 받은 데이터가 없습니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"백엔드 데이터 처리 오류: {e.Message}");
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
                
                // 버튼 클릭 이벤트 설정 (이전 JSON 데이터의 감정 사용)
                int index = i;
                button.onClick.AddListener(() => OnBackendDataButtonClickWithPreviousEmotion(index));
                
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
    
    // 백엔드 데이터 버튼 클릭 시 호출 (이전 JSON 데이터의 감정 사용)
    private void OnBackendDataButtonClickWithPreviousEmotion(int index)
    {
        Debug.Log($"백엔드 데이터 버튼 {index} 클릭됨 (이전 감정 사용)");
        
        if (index >= 0 && index < treeDataList.Count)
        {
            // 현재 버튼의 데이터는 유지하되, 감정만 이전 JSON에서 가져오기
            TreeData currentData = treeDataList[index];
            
            // 이전 JSON 데이터의 감정 찾기
            string previousEmotion = GetPreviousEmotion(index);
            
            // 임시 TreeData 생성 (현재 데이터 + 이전 감정)
            TreeData modifiedData = new TreeData
            {
                date = currentData.date,
                emotion = previousEmotion,
                goodPoints = currentData.goodPoints,
                solution = currentData.solution
            };
            
            currentTreeData = modifiedData;
            UpdateUI();
            OnEmotionButtonClick();
            Debug.Log($"백엔드 데이터 버튼 {index} 처리 완료 (감정: {previousEmotion})");
        }
        else
        {
            Debug.LogError($"잘못된 버튼 인덱스: {index}");
        }
    }
    
    // 이전 JSON 데이터의 감정을 가져오는 메서드
    private string GetPreviousEmotion(int currentIndex)
    {
        if (currentIndex <= 0)
        {
            // 첫 번째 버튼인 경우 마지막 JSON의 감정 사용
            if (treeDataList.Count > 1)
            {
                return treeDataList[treeDataList.Count - 1].emotion;
            }
            else
            {
                // 데이터가 하나뿐인 경우 현재 감정 사용
                return treeDataList[0].emotion;
            }
        }
        else
        {
            // 이전 JSON의 감정 사용
            return treeDataList[currentIndex - 1].emotion;
        }
    }
    
    // 백엔드 데이터 버튼 클릭 시 호출 (기존 방식 - 필요시 사용)
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
    
    /// <summary>
    /// 가장 최근 날짜의 데이터를 자동으로 선택
    /// </summary>
    private void SelectMostRecentData()
    {
        if (treeDataList == null || treeDataList.Count == 0)
        {
            Debug.LogWarning("선택할 데이터가 없습니다.");
            return;
        }
        
        // 가장 최근 날짜 찾기
        TreeData mostRecentData = null;
        System.DateTime mostRecentDate = System.DateTime.MinValue;
        
        foreach (var treeData in treeDataList)
        {
            if (System.DateTime.TryParse(treeData.date, out System.DateTime currentDate))
            {
                if (currentDate > mostRecentDate)
                {
                    mostRecentDate = currentDate;
                    mostRecentData = treeData;
                }
            }
        }
        
        if (mostRecentData != null)
        {
            // 가장 최근 데이터를 현재 데이터로 설정
            currentTreeData = mostRecentData;
            UpdateUI();
            OnEmotionButtonClick();
            
            Debug.Log($"가장 최근 날짜의 데이터가 자동 선택되었습니다: {mostRecentData.date} - {mostRecentData.emotion}");
        }
        else
        {
            Debug.LogWarning("유효한 날짜 데이터를 찾을 수 없습니다.");
        }
    }
    
    void InitializeEmotionObjects()
    {
        emotionObjects = new Dictionary<string, GameObject>
        {
            {"즐거움", funObject},
            {"분노", angryObject},
            {"슬픔", sadObject},
            {"허무감", frustrationObject},
            {"달성감", achievementObject}
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
    }
    

    
    // 버튼 클릭 시 호출되는 메서드
    public void OnEmotionButtonClick()
    {
        if (currentTreeData == null) return;
        
        // 모든 감정 오브젝트를 비활성화
        DeactivateAllEmotionObjects();
        
        // 현재 감정에 해당하는 오브젝트 활성화
        string emotion = currentTreeData.emotion; // ToUpper() 제거하여 한글 감정명 그대로 사용
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