using Oculus.Interaction;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// 각 Snap 오브젝트에 붙여서 SnapDataManager와 연동하는 스크립트
/// </summary>
public class SnapInteractableHandler : MonoBehaviour
{
    [Header("Snap 설정")]
    public string snapId;           // 이 Snap의 고유 ID
    public string snapName;         // 이 Snap의 이름
    
    [Header("참조")]
    public SnapDataManager snapDataManager;  // SnapDataManager 참조
    
    private SnapInteractable snapInteractable;
    private bool isRegistered = false;
    
    void Start()
    {
        // SnapInteractable 컴포넌트 찾기
        snapInteractable = GetComponent<SnapInteractable>();
        
        if (snapInteractable == null)
        {
            Debug.LogError($"SnapInteractable 컴포넌트를 찾을 수 없습니다: {gameObject.name}");
            return;
        }
        
        // SnapDataManager 찾기 (씬에 하나만 있어야 함)
        if (snapDataManager == null)
        {
            snapDataManager = FindObjectOfType<SnapDataManager>();
            if (snapDataManager == null)
            {
                Debug.LogError("SnapDataManager를 찾을 수 없습니다. 씬에 SnapDataManager를 추가해주세요.");
                return;
            }
        }
        
        // Snap 등록
        RegisterSnap();
        
        // 이벤트 리스너 등록 부분은 삭제!
    }
     
    /// <summary>
    /// Snap 등록
    /// </summary>
    private void RegisterSnap()
    {
        if (string.IsNullOrEmpty(snapId))
        {
            snapId = $"snap_{gameObject.name}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        }
        
        if (string.IsNullOrEmpty(snapName))
        {
            snapName = gameObject.name;
        }
        
        snapDataManager.RegisterSnap(snapId, snapName, transform.position);
        isRegistered = true;
        
        Debug.Log($"Snap 등록 완료: {snapId} - {snapName}");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // SnapZone에 들어온 오브젝트가 Fruit인지 확인
        var fruitInfoUI = other.GetComponent<FruitInfoUI>();
        if (fruitInfoUI != null)
        {
            Debug.Log($"오브젝트가 Snap에 들어옴: {snapId}");

            var fruitData = new FruitEmotionData
            {
                taskId = fruitInfoUI.id,
                emotion = "기본",
                todo = fruitInfoUI.todo,
                date = fruitInfoUI.date,
                time = fruitInfoUI.time,
                acceptedAt = fruitInfoUI.acceptedAt,
                userId = fruitInfoUI.userId,
                position = fruitInfoUI.transform.position,
                createdAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            snapDataManager.AttachFruitToSnap(snapId, fruitData);
            Debug.Log($"Fruit을 Snap에 연결: {snapId} - {fruitInfoUI.todo}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var fruitInfoUI = other.GetComponent<FruitInfoUI>();
        if (fruitInfoUI != null)
        {
            Debug.Log($"오브젝트가 Snap에서 나감: {snapId}");
            snapDataManager.DetachFruitFromSnap(snapId);
        }
    }
    
    /// <summary>
    /// 수동으로 Fruit 데이터 연결 (감정 선택 후 호출)
    /// </summary>
    public void AttachFruitWithEmotion(FruitEmotionData fruitData)
    {
        if (!isRegistered)
        {
            Debug.LogWarning("Snap이 등록되지 않았습니다.");
            return;
        }
        
        snapDataManager.AttachFruitToSnap(snapId, fruitData);
    }
    
    /// <summary>
    /// 현재 Snap의 데이터 가져오기
    /// </summary>
    public SnapData GetCurrentSnapData()
    {
        return snapDataManager.GetSnapData(snapId);
    }
    
    /// <summary>
    /// Snap ID 설정 (런타임에서 변경 가능)
    /// </summary>
    public void SetSnapId(string newSnapId)
    {
        snapId = newSnapId;
        Debug.Log($"Snap ID 변경: {snapId}");
    }
    
    /// <summary>
    /// Snap 이름 설정 (런타임에서 변경 가능)
    /// </summary>
    public void SetSnapName(string newSnapName)
    {
        snapName = newSnapName;
        Debug.Log($"Snap 이름 변경: {snapName}");
    }
} 