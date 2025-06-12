using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class AudioSender : MonoBehaviour
{
    public IEnumerator SendWavToServer(string filename)
    {
        string path = Path.Combine(Application.persistentDataPath, filename);
        byte[] wavData = File.ReadAllBytes(path);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, filename, "audio/wav");

        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost:8080/speech", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("����: " + www.downloadHandler.text);
            }
            else
            {
                Debug.LogError("���� ����: " + www.error);
            }
        }
    }
}
