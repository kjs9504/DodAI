using UnityEngine;

using UnityEngine.UI;
public class UIController : MonoBehaviour
{
    public GameObject uiGroup;
    private bool isActive = false;

    public void ToggleUI()
    {
        isActive = !isActive;
        uiGroup.SetActive(isActive);
    }
}
