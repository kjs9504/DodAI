using UnityEngine;

public class FruitDestroy : MonoBehaviour
{
    [Header("부딪힐 대상 태그")]
    public string targetTag = "Target";

    // Fruit 쪽 Collider에 IsTrigger = true인 경우
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            // 파괴 직전 이펙트나 사운드 넣고 싶으면 여기서 처리
            Destroy(gameObject);
        }
    }
}
