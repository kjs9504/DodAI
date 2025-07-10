using UnityEngine;

public class FruitDestroy : MonoBehaviour
{
    [Header("�ε��� ��� �±�")]
    public string targetTag = "Target";

    // Fruit �� Collider�� IsTrigger = true�� ���
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            // �ı� ���� ����Ʈ�� ���� �ְ� ������ ���⼭ ó��
            Destroy(gameObject);
        }
    }
}
