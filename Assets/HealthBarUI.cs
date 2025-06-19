using UnityEngine;
using UnityEngine.UI; // Necesario para usar componentes UI como Slider e Image

/// <summary>
/// Controla la visualización de la barra de salud de una unidad.
/// Se encarga de actualizar el valor del Slider y el color del relleno.
/// También asegura que la barra de salud siempre mire a la cámara principal,
/// manteniéndose en un plano vertical para una apariencia 2D.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Tooltip("Referencia al componente Slider que representa la barra de salud.")]
    [SerializeField] private Slider healthSlider;
    [Tooltip("Referencia al componente Image que es el 'relleno' del Slider.")]
    [SerializeField] private Image fillImage;

    private Camera mainCamera; // Cachea la cámara principal para rendimiento.

    void Awake()
    {
        // Busca y cachea la cámara principal una vez al inicio.
        // Es crucial que tu cámara principal en la escena tenga el tag "MainCamera".
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("HealthBarUI: No se encontró una cámara principal. Asegúrate de que tu cámara tenga el tag 'MainCamera'.");
        }
    }

    void LateUpdate()
    {
        // Asegura que la barra de salud siempre mire a la cámara,
        // pero solo rota en el eje Y global para mantenerse 'plana' y no inclinada.
        if (mainCamera != null)
        {
            // Calcula la dirección del frente de la cámara, pero solo en el plano horizontal (XZ).
            // Esto asegura que la barra de salud no se incline hacia arriba o abajo con la cámara.
            Vector3 cameraForwardXZ = mainCamera.transform.forward;
            cameraForwardXZ.y = 0; // Elimina el componente Y para que solo sea horizontal.
            cameraForwardXZ.Normalize(); // Normaliza la dirección.

            // Si la dirección no es cero (para evitar errores de LookRotation en Vector3.zero),
            // hace que la barra de salud mire en esa dirección horizontal.
            // Vector3.up asegura que el "arriba" de la barra de salud siempre esté alineado con el "arriba" del mundo.
            if (cameraForwardXZ != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(cameraForwardXZ, Vector3.up);
            }
        }
    }

    /// <summary>
    /// Actualiza el porcentaje de relleno de la barra de salud en el Slider.
    /// </summary>
    /// <param name="currentHealth">Salud actual de la unidad.</param>
    /// <param name="maxHealth">Salud máxima de la unidad.</param>
    public void SetHealthPercentage(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth; // Establece el valor máximo del Slider.
            healthSlider.value = currentHealth; // Establece el valor actual del Slider.
        }
    }

    /// <summary>
    /// Establece el color del relleno de la barra de salud (verde para aliados, rojo para enemigos).
    /// </summary>
    /// <param name="color">El color deseado (ej. Color.green, Color.red).</param>
    public void SetColor(Color color)
    {
        if (fillImage != null)
        {
            fillImage.color = color; // Asigna el color al componente Image del relleno.
        }
    }
}
