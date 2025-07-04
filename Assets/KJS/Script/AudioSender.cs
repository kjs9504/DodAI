using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class AudioSender : MonoBehaviour
{
    [Header("전송할 서버 URL")]
    public string serverUrl = "https://8527-211-186-8-83.ngrok-free.app/transcribe/";

    [Header("할 일 리스트 매니저 (Inspector에서 할당)")]
    public TodoListManager todoListManager;

    [Header("할 일 저장용 백엔드 URL")]
    public string backendUrl = "http://localhost:5432/api/tasks/bulk";


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

                if (todoListManager != null)
                {
                    todoListManager.LoadFromJson(json);
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

