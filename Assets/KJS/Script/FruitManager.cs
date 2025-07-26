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
    private WeekData currentWeekData;

    void Start()
    {
        Debug.Log("[FruitManager] Start() 호출됨");
        Debug.Log($"[FruitManager] fruitPrefab: {(fruitPrefab != null ? "설정됨" : "NULL!")}");
        Debug.Log($"[FruitManager] spawnPoint: {(spawnPoint != null ? "설정됨" : "NULL!")}");
        Debug.Log($"[FruitManager] acceptsUrl: {acceptsUrl}");
        
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

            // 기존 구조를 먼저 시도 (실제로 받는 구조)
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
                // 기존 구조로 파싱 실패 시 새로운 WeekData 구조로 시도
                Debug.LogWarning("[FruitManager] ❌ 기존 구조 파싱 실패, 새로운 WeekData 구조로 시도");
                currentWeekData = JsonUtility.FromJson<WeekData>(rawJson);
                
                if (currentWeekData != null && currentWeekData.tasks != null)
                {
                    Debug.Log($"[FruitManager] ✅ 새로운 WeekData 구조로 파싱 성공:");
                    Debug.Log($"[FruitManager] 주간 시작: {currentWeekData.weekStartDate}");
                    Debug.Log($"[FruitManager] 주간 종료: {currentWeekData.weekEndDate}");
                    Debug.Log($"[FruitManager] Tasks 개수: {currentWeekData.tasks.Count}");
                    Debug.Log($"[FruitManager] Fruits 개수: {currentWeekData.fruits?.Count ?? 0}");

                    // Tasks 정보 출력
                    foreach (var task in currentWeekData.tasks)
                    {
                        Debug.Log($"[FruitManager] Task → id={task.id}, todo={task.todo}, date={task.date}, time={task.time}, emotion='{task.emotion}'");
                    }

                    // Fruits 정보 출력
                    if (currentWeekData.fruits != null && currentWeekData.fruits.Count > 0)
                    {
                        foreach (var fruit in currentWeekData.fruits)
                        {
                            Debug.Log($"[FruitManager] Fruit → id={fruit.id}, taskId={fruit.acceptedTaskId}, emotion='{fruit.emotion}'");
                        }
                    }

                    // 각 task 처리
                    foreach (var task in currentWeekData.tasks)
                    {
                        yield return StartCoroutine(HandleOneTask(task));
                    }
                }
                else
                {
                    Debug.LogError("[FruitManager] ❌ 모든 JSON 파싱 시도 실패");
                    Debug.LogError($"[FruitManager] JSON 길이: {rawJson.Length}");
                    Debug.LogError($"[FruitManager] JSON 시작 부분: {rawJson.Substring(0, Math.Min(100, rawJson.Length))}");
                }
            }
        }
    }

    private IEnumerator HandleOneTask(AcceptedTaskData task)
    {
        // 현재 주간 데이터에서 해당 taskId의 fruit 찾기
        FruitData existingFruit = null;
        if (currentWeekData?.fruits != null)
        {
            existingFruit = currentWeekData.fruits.Find(f => f.acceptedTaskId == task.id);
        }

        if (existingFruit == null)
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
            
            // task에서 emotion 값 사용 (fruits에 없으면 task의 emotion 사용)
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
        else
        {
            // 기존 과일이 있으면 저장된 위치 정보를 사용해서 생성
            Vector3 spawnPos;
            if (existingFruit.position != null)
                spawnPos = new Vector3(existingFruit.position.x, existingFruit.position.y, existingFruit.position.z);
            else
                spawnPos = spawnPoint != null ? GetBasketSpawnPosition(spawnedFruits.Count) : transform.position;

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
            Debug.Log($"[FruitManager] 기존 위치에 Fruit 인스턴스 생성 완료: {obj.name}");

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
            
            // fruits의 emotion 우선, 없으면 task의 emotion 사용
            string emotionToUse = !string.IsNullOrEmpty(existingFruit.emotion) ? existingFruit.emotion : 
                                 (!string.IsNullOrEmpty(task.emotion) ? task.emotion : "");
            
            // 'none' 값을 빈 문자열로 처리
            if (emotionToUse == "none" || emotionToUse == "NONE")
            {
                emotionToUse = "";
                Debug.Log($"[FruitManager] Task {task.id}의 'none' 값을 빈 문자열로 변환");
            }
            
            infoUI.currentEmotion = emotionToUse;

            // emotion 값 전달 (다음 프레임에서 실행하여 Awake 완료 보장)
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
                emotion = emotionToUse,
                position = existingFruit.position
            });
            
            Debug.Log($"[FruitManager] 기존 위치에 Fruit 생성 완료: taskId={task.id}, 위치={spawnPos}, emotion={emotionToUse}");
        }
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
            yield return StartCoroutine(CreateFruit(
                fruit.acceptedTaskId, new Vector3(fruit.posX, fruit.posY, fruit.posZ)));
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
        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;
        int layer = fruitIndex / fruitsPerLayer;
        float x = basePos.x + UnityEngine.Random.Range(-spawnRange.x, spawnRange.x);
        float z = basePos.z + UnityEngine.Random.Range(-spawnRange.z, spawnRange.z);
        float y = basePos.y + (layer * fruitHeight) + UnityEngine.Random.Range(-0.1f, 0.1f);
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
}


