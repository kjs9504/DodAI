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
}

