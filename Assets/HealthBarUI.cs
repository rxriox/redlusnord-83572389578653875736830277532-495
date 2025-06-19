using UnityEngine;
using UnityEngine.UI; // Necesario para usar componentes UI como Slider e Image

/// <summary>
/// Controla la visualización de la barra de salud de una unidad.
/// Se encarga de actualizar el valor del Slider y el color del relleno.
/// Asegura que la barra de salud siempre mire a la cámara principal,
/// manteniéndose en un plano vertical y ajustando su profundidad para una apariencia 2D consistente.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Tooltip("Referencia al componente Slider que representa la barra de salud.")]
    [SerializeField] private Slider healthSlider;
    [Tooltip("Referencia al componente Image que es el 'relleno' del Slider.")]
    [SerializeField] private Image fillImage;

    [Header("Ajustes de Visibilidad")]
    [Tooltip("La distancia en Z para desplazar la barra de salud hacia la cámara.")]
    [SerializeField] private float zOffsetTowardsCamera = 0.5f; // Ajusta este valor en el Inspector del prefab de la barra de salud

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
        // manteniéndose plana y ajustando su posición en Z para consistencia visual.
        if (mainCamera != null)
        {
            // Paso 1: Billboard horizontal (como en la versión anterior).
            Vector3 cameraForwardXZ = mainCamera.transform.forward;
            cameraForwardXZ.y = 0; 
            cameraForwardXZ.Normalize(); 

            if (cameraForwardXZ != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(cameraForwardXZ, Vector3.up);
            }

            // Paso 2: Ajustar la posición local en Z para compensar la perspectiva.
            // Esto mueve la barra de salud ligeramente hacia la cámara a lo largo de su propio eje Z local.
            // Dado que es hija de la unidad, esto es un offset relativo a la unidad,
            // pero en la dirección que la propia barra de salud está mirando (hacia la cámara).
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, -zOffsetTowardsCamera);
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
