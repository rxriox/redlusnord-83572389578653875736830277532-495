using UnityEngine;

/// <summary>
/// Gestiona la interfaz de usuario para la colocación de unidades,
/// permitiendo alternar entre el panel de aliados y el de enemigos.
/// </summary>
public class PlacementUIManager : MonoBehaviour
{
    [Header("Paneles de Contenido")]
    [Tooltip("El objeto padre que contiene los íconos de los aliados.")]
    [SerializeField] private GameObject allyContentPanel;
    
    [Tooltip("El objeto padre que contiene los íconos de los enemigos.")]
    [SerializeField] private GameObject enemyContentPanel;

    void Start()
    {
        // Por defecto, al empezar, mostramos el panel de aliados.
        ShowAllyPanel();
    }

    /// <summary>
    /// Activa el panel de aliados y desactiva el de enemigos.
    /// </summary>
    public void ShowAllyPanel()
    {
        allyContentPanel.SetActive(true);
        enemyContentPanel.SetActive(false);
    }

    /// <summary>
    /// Activa el panel de enemigos y desactiva el de aliados.
    /// </summary>
    public void ShowEnemyPanel()
    {
        allyContentPanel.SetActive(false);
        enemyContentPanel.SetActive(true);
    }
}