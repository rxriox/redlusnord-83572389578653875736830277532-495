using UnityEngine;
using UnityEngine.UI; // Necesario para usar componentes UI como Button, Scrollbar, Image

/// <summary>
/// Gestiona la interfaz de usuario para la colocación de unidades,
/// permitiendo alternar entre el panel de aliados y el de enemigos.
/// Ahora también actualiza el color de los botones y la visibilidad de los Scrollbars.
/// </summary>
public class PlacementUIManager : MonoBehaviour
{
    [Header("ScrollViews de Contenido")] // Cambiado a ScrollViews para mayor claridad
    [Tooltip("El componente ScrollRect del ScrollView de aliados.")]
    [SerializeField] private ScrollRect allyScrollView; // Referencia al ScrollRect completo del aliado
    [Tooltip("El componente ScrollRect del ScrollView de enemigos.")]
    [SerializeField] private ScrollRect enemyScrollView; // Referencia al ScrollRect completo del enemigo

    [Header("Botones de Selección")]
    [Tooltip("El botón para mostrar el panel de aliados.")]
    [SerializeField] private Button allyButton;
    [Tooltip("El botón para mostrar el panel de enemigos.")]
    [SerializeField] private Button enemyButton;

    [Header("Colores de Botón")]
    [Tooltip("Color del botón cuando su panel está activo (ej. negro).")]
    [SerializeField] private Color activeColor = Color.black;
    [Tooltip("Color del botón cuando su panel está inactivo (ej. blanco).")]
    [SerializeField] private Color inactiveColor = Color.white;

    void Start()
    {
        // Asegúrate de que los botones llamen a los métodos correctos
        if (allyButton != null) allyButton.onClick.AddListener(ShowAllyPanel);
        if (enemyButton != null) enemyButton.onClick.AddListener(ShowEnemyPanel);

        // Por defecto, al empezar, mostramos el panel de aliados.
        ShowAllyPanel();
    }

    /// <summary>
    /// Activa el ScrollView de aliados, desactiva el de enemigos, y actualiza la UI de los botones y Scrollbars.
    /// </summary>
    public void ShowAllyPanel()
    {
        // Activa y desactiva los ScrollViews completos.
        if (allyScrollView != null) allyScrollView.gameObject.SetActive(true);
        if (enemyScrollView != null) enemyScrollView.gameObject.SetActive(false);

        // Actualizar colores de botón (asumiendo que el botón tiene un componente Image)
        if (allyButton != null) allyButton.GetComponent<Image>().color = activeColor;
        if (enemyButton != null) enemyButton.GetComponent<Image>().color = inactiveColor;

        // ELIMINADO: mainScrollRect.verticalNormalizedPosition = 1f; // Ya no se resetea al principio
    }

    /// <summary>
    /// Activa el ScrollView de enemigos, desactiva el de aliados, y actualiza la UI de los botones y Scrollbars.
    /// </summary>
    public void ShowEnemyPanel()
    {
        // Activa y desactiva los ScrollViews completos.
        if (allyScrollView != null) allyScrollView.gameObject.SetActive(false);
        if (enemyScrollView != null) enemyScrollView.gameObject.SetActive(true);

        // Actualizar colores de botón
        if (allyButton != null) allyButton.GetComponent<Image>().color = inactiveColor;
        if (enemyButton != null) enemyButton.GetComponent<Image>().color = activeColor;

        // ELIMINADO: mainScrollRect.verticalNormalizedPosition = 1f; // Ya no se resetea al principio
    }
}
