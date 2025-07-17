using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MultiPokeUIToMeshController : MonoBehaviour
{
    [System.Serializable]
    public class Item
    {
        [Header("�� Ŭ���� UI Image (��ư)")]
        public Image uiButton;          // Ŭ�� �Է� ���� UI

        [Header("�� ���� ��� 3D ������Ʈ")]
        public GameObject targetObject;  // �� ������Ʈ�� MeshFilter/MeshRenderer ����

        [Header("�� ������ �� ������ Mesh/Material")]
        public Mesh pressedMesh;
        public Material pressedMaterial;

        // ��Ÿ�ӿ� ����Ǵ� ���� ����
        [HideInInspector] public Mesh normalMesh;
        [HideInInspector] public Material normalMaterial;

        // ��Ÿ�� ĳ��
        [HideInInspector] public MeshFilter mf;
        [HideInInspector] public MeshRenderer mr;
    }

    [Tooltip("Public�� �Ҵ��� UI �̹���(5��)���� 1���� Item�� �߰��ϼ���.")]
    public List<Item> items = new List<Item>(5);

    void Awake()
    {
        foreach (var it in items)
        {
            // 1) UI Image �Ҵ� üũ
            if (it.uiButton == null)
            {
                Debug.LogWarning($"MultiPokeUIToMeshController: uiButton�� �Ҵ���� ���� (Item �ε��� {items.IndexOf(it)})");
                continue;
            }

            // 2) targetObject ������ Self(��ư ������Ʈ) ���
            if (it.targetObject == null)
                it.targetObject = it.uiButton.gameObject;

            // 3) MeshFilter / MeshRenderer Ž�� (�ڽ� ����)
            if (!it.targetObject.TryGetComponent<MeshFilter>(out it.mf))
                it.mf = it.targetObject.GetComponentInChildren<MeshFilter>();
            if (!it.targetObject.TryGetComponent<MeshRenderer>(out it.mr))
                it.mr = it.targetObject.GetComponentInChildren<MeshRenderer>();

            if (it.mf == null || it.mr == null)
            {
                Debug.LogError($"[{name}] Item[{items.IndexOf(it)}]: '{it.targetObject.name}'�� MeshFilter/MeshRenderer�� �����ϴ�.");
                continue;
            }

            // 4) ���� ���� ����
            it.normalMesh = it.mf.mesh;
            it.normalMaterial = it.mr.material;

            // 5) UI Image �� RaycastTarget �ѱ�
            it.uiButton.raycastTarget = true;

            // 6) EventTrigger �߰�
            var trig = it.uiButton.gameObject.AddComponent<EventTrigger>();

            // PointerDown �� OnPressed
            var downEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };
            downEntry.callback.AddListener((data) => OnPressed(it));
            trig.triggers.Add(downEntry);

            // PointerUp �� OnReleased
            var upEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerUp
            };
            trig.triggers.Add(upEntry);
        }
    }

    private void OnPressed(Item it)
    {
        if (it.mf == null || it.mr == null) return;
        if (it.pressedMesh != null) it.mf.mesh = it.pressedMesh;
        if (it.pressedMaterial != null) it.mr.material = it.pressedMaterial;

        it.targetObject.transform.localScale = new Vector3(2f, 2f, 2f);
    }
}






