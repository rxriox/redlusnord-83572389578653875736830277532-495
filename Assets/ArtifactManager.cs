using UnityEngine;
using System.Collections.Generic;

public class ArtifactManager : MonoBehaviour
{
    public static ArtifactManager Instance { get; private set; }

    [Header("Configuración")]
    [Tooltip("El número máximo de artefactos que el jugador puede tener activos.")]
    public int maxArtifacts = 8;
    
    [Header("Referencias de UI")]
    [Tooltip("El panel donde se mostrarán los iconos de los artefactos activos.")]
    public Transform activeArtifactsContainer;
    [Tooltip("El prefab para el icono de un artefacto cuando está activo.")]
    public GameObject activeArtifactIconPrefab;

    // Lista privada para llevar la cuenta de los artefactos activos.
    private List<ActiveArtifactIcon> activeArtifacts = new List<ActiveArtifactIcon>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    /// <summary>
    /// Comprueba si el jugador puede añadir un nuevo artefacto.
    /// </summary>
    public bool CanPlaceArtifact()
    {
        bool canPlace = activeArtifacts.Count < maxArtifacts;
        if (!canPlace)
        {
            Debug.Log("Límite de artefactos alcanzado.");
        }
        return canPlace;
    }

    /// <summary>
    /// Se llama cuando un artefacto se suelta en la zona de artefactos activos.
    /// </summary>
    public void PlaceArtifact(ArtifactIconController benchIcon)
    {
        if (!CanPlaceArtifact()) return;

        // Creamos el nuevo icono en el panel de activos
        GameObject activeIconGO = Instantiate(activeArtifactIconPrefab, activeArtifactsContainer);
        ActiveArtifactIcon activeIconScript = activeIconGO.GetComponent<ActiveArtifactIcon>();
        
        // Lo inicializamos con los datos del icono de la banca
        activeIconScript.Initialize(benchIcon);

        // Actualizamos las listas y estados
        activeArtifacts.Add(activeIconScript);
        benchIcon.SetAsPlaced();
    }

    /// <summary>
    /// Se llama cuando un artefacto activo se suelta en la papelera.
    /// </summary>
    public void RemoveArtifact(ActiveArtifactIcon activeIcon)
    {
        // Le decimos al icono original en la banca que vuelva a estar disponible
        activeIcon.originatingBenchIcon.ResetIcon();
        
        // Eliminamos el artefacto de la lista de activos
        activeArtifacts.Remove(activeIcon);

        // Destruimos el objeto del icono activo
        Destroy(activeIcon.gameObject);
    }
}