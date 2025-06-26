using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TodoListManager : MonoBehaviour
{
    [Header("ScrollRect �� Viewport �� Content")]
    public RectTransform content;       // ScrollRect.Content �� �巡��
    [Header("�� �� ������ Prefab")]
    public GameObject itemPrefab;       // TodoItemPrefab �� �巡��
    [Header("�ʱ� ���� �� �ϵ�")]
    public List<string> initialTasks;   // �ν����Ϳ��� �׸� �߰�

    void Start()
    {
        // Start �� �ʱ� ��� ����
        foreach (var task in initialTasks)
            CreateItem(task);
    }

    // �׸� �ϳ� ����
    public void CreateItem(string text)
    {
        if (content == null || itemPrefab == null) return;

        // Instantiate, �θ�(Content)�� �ڵ� ����
        var go = Instantiate(itemPrefab, content);
        go.transform.localScale = Vector3.one;  // ������ ���� ����

        // �ؽ�Ʈ ����
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
    }
}

