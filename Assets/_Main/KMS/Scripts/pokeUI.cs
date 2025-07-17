using UnityEngine;

public class ToggleUIPanel : MonoBehaviour
{
    public GameObject uiPanel;

    public void ToggleUI()
    {
        uiPanel.SetActive(!uiPanel.activeSelf);
    }
}
