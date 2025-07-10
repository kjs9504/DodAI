using UnityEngine;
using System.Text;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class FruitInfo : MonoBehaviour
{
    // 이 과일이 연결된 AcceptedTask의 ID
    public long acceptedTaskId;
}

public class FruitDestroy : MonoBehaviour
{
    public string targetTag = "Target";
    public string deleteUrl = "http://YOUR_SERVER_IP:8080/api/tasks/accepted";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(targetTag)) return;

        Debug.Log($"v: hit {other.name}");

        var info = GetComponent<FruitInfo>();
        if (info != null)
        {
            Debug.Log($"[FruitDestroy] Starting DELETE for acceptedTaskId={info.acceptedTaskId}");
            StartCoroutine(DeleteAcceptedTask(info.acceptedTaskId));
        }
        else
        {
            Debug.LogWarning("[FruitDestroy] FruitInfo 컴포넌트를 찾을 수 없습니다!");
        }

        Destroy(gameObject);
    }

    private IEnumerator DeleteAcceptedTask(long acceptedTaskId)
    {
        var reqObj = new DeleteRequest
        {
            tasks = new List<DeleteItem> { new DeleteItem { id = acceptedTaskId } }
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
            Debug.Log($"[FruitDestroy] DELETE 성공: {req.responseCode}");
        else
            Debug.LogError($"[FruitDestroy] DELETE 실패: {req.error} ({req.responseCode})");
    }

    // JSON 직렬화용 클래스
    [System.Serializable]
    class DeleteRequest { public List<DeleteItem> tasks; }
    [System.Serializable]
    class DeleteItem { public long id; }
}
