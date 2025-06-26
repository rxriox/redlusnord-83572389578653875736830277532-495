using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ArtifactManager : MonoBehaviour
{
    public static ArtifactManager Instance { get; private set; }

    [Header("Configuración")]
    [Tooltip("El número máximo de artefactos que el jugador puede tener activos.")]
    public int maxArtifacts = 8;

    [Header("Referencias de UI")]
    [Tooltip("El panel donde se mostrarán los iconos de los artefactos activos.")]
    public Transform activeArtifactsContainer;
    [Tooltip("El objeto de texto que se muestra cuando no hay artefactos activos.")]
    public GameObject noArtifactsMessageObject;
    [Tooltip("El texto que mostrará el contador de artefactos activos (ej. 2/8).")]
    public TextMeshProUGUI activeArtifactsCountText;
    [Tooltip("El prefab para el icono de un artefacto cuando está activo.")]
    public GameObject activeArtifactIconPrefab;
    private List<ActiveArtifactIcon> activeArtifacts = new List<ActiveArtifactIcon>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    void Start()
    {
        UpdateEmptyMessageVisibility();
        UpdateCountText();
    }
    public bool CanPlaceArtifact()
    {
        bool canPlace = activeArtifacts.Count < maxArtifacts;
        if (!canPlace)
        {
            Debug.Log("Límite de artefactos alcanzado.");
        }
        return canPlace;
    }

    public void PlaceArtifact(ArtifactIconController benchIcon)
    {
        if (!CanPlaceArtifact()) return;

        // CORRECCIÓN: Volvemos a usar Instantiate en lugar de pedirlo al Object Pooler.
        GameObject activeIconGO = Instantiate(activeArtifactIconPrefab, activeArtifactsContainer);
        
        if (activeIconGO != null)
        {
            // El resto de la lógica es la misma
            activeIconGO.transform.localScale = Vector3.one;
            ActiveArtifactIcon activeIconScript = activeIconGO.GetComponent<ActiveArtifactIcon>();
        
            activeIconScript.Initialize(benchIcon);
            activeArtifacts.Add(activeIconScript);
            benchIcon.SetAsPlaced();
            UpdateEmptyMessageVisibility();
            UpdateCountText();
        }
    }

    public void RemoveArtifact(ActiveArtifactIcon activeIcon)
    {
        if (activeIcon == null) return;
        
        if(activeIcon.originatingBenchIcon != null)
            activeIcon.originatingBenchIcon.ResetIcon();
        
        activeArtifacts.Remove(activeIcon);
        
        // CORRECCIÓN: Usamos Destroy en lugar de devolver el objeto al pool.
        Destroy(activeIcon.gameObject);

        UpdateEmptyMessageVisibility();
        UpdateCountText();
    }
    private void UpdateEmptyMessageVisibility()
    {
        if (noArtifactsMessageObject != null)
        {
            noArtifactsMessageObject.SetActive(activeArtifacts.Count == 0);
        }
    }
    private void UpdateCountText()
    {
        if (activeArtifactsCountText != null)
        {
            activeArtifactsCountText.text = $"{activeArtifacts.Count}/{maxArtifacts}";
        }
    }
}