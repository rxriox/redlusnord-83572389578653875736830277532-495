using UnityEngine;
using UnityEngine.EventSystems;

// Este script convierte un objeto de la UI en una zona válida para soltar cosas.
public class TrashZoneController : MonoBehaviour, IDropHandler
{
    // No necesitamos escribir nada dentro de OnDrop.
    // El PlayerController se encargará de toda la lógica.
    // Solo necesitamos que el método exista para que la interfaz IDropHandler funcione.
    public void OnDrop(PointerEventData eventData)
    {
        // La magia ocurre en el PlayerController.
        Debug.Log("Drop detectado en la zona de eliminación.");
    }
}