using UnityEngine;

public class HighlightFollower : MonoBehaviour
{
    public Transform targetToFollow;

    // Usamos LateUpdate para asegurarnos de que el movimiento del objetivo ya ha sido procesado
    // en el frame actual. Esto evita parpadeos o tirones.
    void LateUpdate()
    {
        if (targetToFollow != null)
        {
            // Sigue la posición del objetivo
            transform.position = targetToFollow.position;
        }
        else
        {
            // Si el objetivo se destruye por alguna razón, este highlight se autodestruye.
            Destroy(gameObject);
        }
    }
}