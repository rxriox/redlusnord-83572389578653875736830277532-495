using UnityEngine;
using UnityEngine.EventSystems;

public class TrashZoneController : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("Drop detectado en la zona de eliminación.");
    }
}