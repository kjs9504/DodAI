using UnityEngine;
using System.Text;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI; // UI 버튼을 위한 네임스페이스 추가

public class FruitDestroy : MonoBehaviour
{
    public string deleteUrl = "http://localhost:8080/api/tasks/accepted";
    
    // Inspector에서 직접 설정할 수 있는 ID (FruitInfo가 없을 때 사용)
    [Header("ID 설정")]
    public long manualTaskId = 0;
    public bool useManualId = false;
    
    // 버튼 컴포넌트 참조
    private Button button;

    private void Start()
    {
        // 버튼 컴포넌트 찾기
        button = GetComponent<Button>();
        
        // 버튼이 있다면 클릭 이벤트 등록
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
            Debug.Log("[FruitDestroy] 버튼 클릭 이벤트가 등록되었습니다.");
        }
        else
        {
            Debug.LogWarning("[FruitDestroy] Button 컴포넌트를 찾을 수 없습니다!");
        }
    }

    // 버튼 클릭 시 호출되는 메서드
    public void OnButtonClick()
    {
        Debug.Log($"[FruitDestroy] 버튼이 클릭되었습니다: {gameObject.name}");

        // FruitInfoUI 컴포넌트 검색 과정 로깅 (여러 방법으로 시도)
        var fruitInfoUI = GetComponent<FruitInfoUI>();
        Debug.Log($"[FruitDestroy] 같은 오브젝트에서 FruitInfoUI 검색 결과: {(fruitInfoUI != null ? "찾음" : "찾을 수 없음")}");
        
        if (fruitInfoUI == null)
        {
            // 부모에서 찾기
            fruitInfoUI = GetComponentInParent<FruitInfoUI>();
            Debug.Log($"[FruitDestroy] 부모에서 FruitInfoUI 검색 결과: {(fruitInfoUI != null ? "찾음" : "찾을 수 없음")}");
        }
        
        if (fruitInfoUI == null)
        {
            // 자식에서 찾기
            fruitInfoUI = GetComponentInChildren<FruitInfoUI>();
            Debug.Log($"[FruitDestroy] 자식에서 FruitInfoUI 검색 결과: {(fruitInfoUI != null ? "찾음" : "찾을 수 없음")}");
        }
        
        if (fruitInfoUI == null)
        {
            // 씬 전체에서 찾기
            fruitInfoUI = FindObjectOfType<FruitInfoUI>();
            Debug.Log($"[FruitDestroy] 씬 전체에서 FruitInfoUI 검색 결과: {(fruitInfoUI != null ? "찾음" : "찾을 수 없음")}");
        }
        
        if (fruitInfoUI != null)
        {
            Debug.Log($"[FruitDestroy] FruitInfoUI에서 ID 값: {fruitInfoUI.id}");
            Debug.Log($"[FruitDestroy] Starting DELETE for id={fruitInfoUI.id}");
            StartCoroutine(DeleteAcceptedTask(fruitInfoUI.id));
        }
        else if (useManualId && manualTaskId > 0)
        {
            Debug.Log($"[FruitDestroy] 수동 설정된 ID 사용: {manualTaskId}");
            Debug.Log($"[FruitDestroy] Starting DELETE for manualTaskId={manualTaskId}");
            StartCoroutine(DeleteAcceptedTask(manualTaskId));
        }
        else
        {
            Debug.LogWarning("[FruitDestroy] FruitInfoUI 컴포넌트를 찾을 수 없습니다!");
            Debug.LogWarning("[FruitDestroy] 수동 ID도 설정되지 않았습니다!");
            Debug.LogWarning($"[FruitDestroy] 현재 게임오브젝트의 컴포넌트들:");
            var components = GetComponents<Component>();
            foreach (var comp in components)
            {
                Debug.LogWarning($"  - {comp.GetType().Name}");
            }
            
            // FruitInfoUI가 없어도 오브젝트는 파괴
            Debug.LogWarning("[FruitDestroy] FruitInfoUI가 없으므로 오브젝트만 파괴합니다.");
            Destroy(gameObject);
        }
    }

    private IEnumerator DeleteAcceptedTask(long acceptedTaskId)
    {
        // 백엔드가 기대하는 TodoDTO 형태로 데이터 구성
        var reqObj = new DeleteRequest
        {
            tasks = new List<TodoDTO> { 
                new TodoDTO { 
                    id = acceptedTaskId,
                    todo = "", // 빈 문자열로 설정 (삭제 시에는 필요 없음)
                    date = "", // 빈 문자열로 설정 (삭제 시에는 필요 없음)
                    time = ""  // 빈 문자열로 설정 (삭제 시에는 필요 없음)
                } 
            }
        };
        string json = JsonUtility.ToJson(reqObj);
        Debug.Log($"[FruitDestroy] DELETE 요청 JSON: {json}");

        using var req = new UnityWebRequest(deleteUrl, "DELETE");
        byte[] body = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" };
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[FruitDestroy] DELETE 성공: {req.responseCode}");
        }
        else
        {
            Debug.LogError($"[FruitDestroy] DELETE 실패: {req.error} ({req.responseCode})");
        }

        // HTTP 요청 완료 후 오브젝트 파괴
        Debug.Log("[FruitDestroy] HTTP 요청 완료, 오브젝트 파괴");
        Destroy(gameObject);
    }

    // JSON 직렬화용 클래스 - 백엔드 TodoDTO 구조에 맞춤
    [System.Serializable]
    class DeleteRequest { public List<TodoDTO> tasks; }
    [System.Serializable]
    class TodoDTO 
    { 
        public long id;
        public string todo;
        public string date;
        public string time;
    }
}
