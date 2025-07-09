using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class AudioSender : MonoBehaviour
{
    [Header("전송할 서버 URL")]
    public string serverUrl = "https://8527-211-186-8-83.ngrok-free.app/transcribe/";

    [Header("할 일 리스트 매니저 (Inspector에서 할당)")]
    public TodoListManager todoListManager;

    [Header("할 일 저장용 백엔드 URL")]
    public string backendUrl = "http://localhost:8080/api/tasks/bulk";

    [Header("에러 메시지용 패널")]
    public GameObject errorPanel;               // 에러 메시지 전체 패널
    [Header("에러 메시지용 텍스트")]
    public TextMeshProUGUI errorText;           // 텍스트 컴포넌트

    [Serializable]
    private class TranscribeResponse
    {
        public string result;
        // 필요하면 다른 필드도 추가
    }
    void Awake()
    {
        // 에러 패널이 켜져 있으면 꺼 줍니다.
        if (errorPanel != null)
            errorPanel.SetActive(false);
        else
            Debug.LogWarning("⚠️ errorPanel이 할당되지 않았습니다.");
    }

    public IEnumerator SendWavToServer(string filename)
    {
        string path = Path.Combine(Application.persistentDataPath, filename);
        if (!File.Exists(path))
        {
            Debug.LogError($"⚠️ 파일을 찾을 수 없습니다: {path}");
            yield break;
        }

        byte[] wavData = File.ReadAllBytes(path);
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, filename, "audio/wav");

        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                Debug.Log("🌐 서버 응답(JSON): " + json);

                var resp = JsonUtility.FromJson<TranscribeResponse>(json);
                if (resp.result == "false")
                {
                    // 잘못된 명령일 때만 에러 패널 활성화
                    StartCoroutine(ShowErrorAndHide());
                    yield break;
                }

                if (todoListManager != null)
                {
                    todoListManager.LoadFromJson(json);
                    StartCoroutine(SaveTasksToBackend(json));
                }
                else
                {
                    Debug.LogWarning("⚠️ TodoListManager가 할당되지 않았습니다.");
                }
            }
            else
            {
                Debug.LogError("❌ 전송 실패: " + www.error);
            }
        }
    }

    private IEnumerator ShowErrorAndHide()
    {
        if (errorPanel == null || errorText == null)
        {
            Debug.LogWarning("⚠️ errorPanel 또는 errorText 가 할당되지 않았습니다.");
            yield break;
        }

        // 0) 패널 게임오브젝트 활성화
        errorPanel.SetActive(true);

        // 1) TextMeshPro 가운데 정렬
        errorText.alignment = TextAlignmentOptions.Center;

        // 2) CanvasGroup 준비
        var cg = errorPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = errorPanel.AddComponent<CanvasGroup>();

        // 3) 초기 alpha 세팅
        cg.alpha = 0f;
        errorText.text = "";

        // 4) 페이드 인
        float fadeInDuration = 0.3f;
        for (float t = 0; t < fadeInDuration; t += Time.deltaTime)
        {
            cg.alpha = t / fadeInDuration;
            yield return null;
        }
        cg.alpha = 1f;

        // 5) 타이핑 효과
        string fullMessage = "잘못된 명령입니다.\n다시 한 번 말씀해 주세요.";
        float typingSpeed = 0.05f;
        foreach (char c in fullMessage)
        {
            errorText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        // 6) 유지
        yield return new WaitForSeconds(1f);

        // 7) 페이드 아웃
        float fadeOutDuration = 0.3f;
        for (float t = 0; t < fadeOutDuration; t += Time.deltaTime)
        {
            cg.alpha = 1f - (t / fadeOutDuration);
            yield return null;
        }
        cg.alpha = 0f;

        // 8) 패널 비활성화
        errorText.text = "";
        errorPanel.SetActive(false);
    }


    public IEnumerator SaveTasksToBackend(string tasksJson)
    {
        var request = new UnityWebRequest(backendUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(tasksJson);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log("✅ 백엔드 저장 성공");
        else
            Debug.LogError($"❌ 백엔드 저장 실패 ({request.responseCode}): {request.error}");
    }
}

