// ��ũ��Ʈ: UIButtonHandler.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIButtonHandler : MonoBehaviour
{
    public void LoadVirtualScene()
    {
        Debug.Log("��ư Ŭ����! �� ��ȯ �õ�");
        SceneManager.LoadScene("VirtualScene"); // �� �̸� ��Ȯ�� �Է�
    }
}
