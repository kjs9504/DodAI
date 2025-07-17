using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MultiPokeUIToMeshController : MonoBehaviour
{
    [System.Serializable]
    public class Item
    {
        [Header("■ 클릭할 UI Image (버튼)")]
        public Image uiButton;          // 클릭 입력 받을 UI

        [Header("■ 변경 대상 3D 오브젝트")]
        public GameObject targetObject;  // 이 오브젝트의 MeshFilter/MeshRenderer 변경

        [Header("■ 눌렸을 때 적용할 Mesh/Material")]
        public Mesh pressedMesh;
        public Material pressedMaterial;

        // 런타임에 저장되는 원래 상태
        [HideInInspector] public Mesh normalMesh;
        [HideInInspector] public Material normalMaterial;

        // 런타임 캐시
        [HideInInspector] public MeshFilter mf;
        [HideInInspector] public MeshRenderer mr;
    }

    [Tooltip("Public에 할당한 UI 이미지(5개)마다 1개씩 Item을 추가하세요.")]
    public List<Item> items = new List<Item>(5);

    void Awake()
    {
        foreach (var it in items)
        {
            // 1) UI Image 할당 체크
            if (it.uiButton == null)
            {
                Debug.LogWarning($"MultiPokeUIToMeshController: uiButton이 할당되지 않음 (Item 인덱스 {items.IndexOf(it)})");
                continue;
            }

            // 2) targetObject 없으면 Self(버튼 오브젝트) 사용
            if (it.targetObject == null)
                it.targetObject = it.uiButton.gameObject;

            // 3) MeshFilter / MeshRenderer 탐색 (자식 포함)
            if (!it.targetObject.TryGetComponent<MeshFilter>(out it.mf))
                it.mf = it.targetObject.GetComponentInChildren<MeshFilter>();
            if (!it.targetObject.TryGetComponent<MeshRenderer>(out it.mr))
                it.mr = it.targetObject.GetComponentInChildren<MeshRenderer>();

            if (it.mf == null || it.mr == null)
            {
                Debug.LogError($"[{name}] Item[{items.IndexOf(it)}]: '{it.targetObject.name}'에 MeshFilter/MeshRenderer가 없습니다.");
                continue;
            }

            // 4) 원래 상태 저장
            it.normalMesh = it.mf.mesh;
            it.normalMaterial = it.mr.material;

            // 5) UI Image 에 RaycastTarget 켜기
            it.uiButton.raycastTarget = true;

            // 6) EventTrigger 추가
            var trig = it.uiButton.gameObject.AddComponent<EventTrigger>();

            // PointerDown → OnPressed
            var downEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };
            downEntry.callback.AddListener((data) => OnPressed(it));
            trig.triggers.Add(downEntry);

            // PointerUp → OnReleased
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






