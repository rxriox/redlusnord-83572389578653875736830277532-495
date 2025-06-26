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

        // Pedimos un icono del pool en lugar de instanciarlo.
        GameObject activeIconGO = ObjectPooler.Instance.SpawnFromPool("ActiveArtifactIcon", activeArtifactsContainer.position, Quaternion.identity);
        
        if (activeIconGO != null)
        {
            // Lo hacemos hijo del contenedor, y el Layout Group se encargará de su posición.
            activeIconGO.transform.SetParent(activeArtifactsContainer);
            activeIconGO.transform.localScale = Vector3.one; // Nos aseguramos de que la escala es correcta.

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
        
        // CORRECCIÓN: Devolvemos el icono al pool en lugar de destruirlo.
        ObjectPooler.Instance.ReturnToPool("ActiveArtifactIcon", activeIcon.gameObject);

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