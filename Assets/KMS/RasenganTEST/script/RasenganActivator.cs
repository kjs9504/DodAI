using Oculus.Interaction;
using UnityEngine;

public class RasenganActivator : MonoBehaviour
{
    [SerializeField] private SelectorUnityEventWrapper m_NinjaGestureEvent;
    void Start()
    {
        m_NinjaGestureEvent.WhenSelected.AddListener(On)
    }
    void Update()
    {
        
    }
}
