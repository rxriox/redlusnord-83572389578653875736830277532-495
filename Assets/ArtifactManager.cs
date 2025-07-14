using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class ArtifactManager : MonoBehaviour
{
    public static ArtifactManager Instance { get; private set; }

    [Header("Referencias de UI")]
    public Transform activeArtifactsContainer;
    public GameObject noArtifactsMessageObject;
    public TextMeshProUGUI activeArtifactsCountText;
    public GameObject activeArtifactIconPrefab;

    private List<ActiveArtifactIcon> activeArtifacts = new List<ActiveArtifactIcon>();
    private Dictionary<ArtifactCategory, int> activeCategoryCounts = new Dictionary<ArtifactCategory, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        UpdateUI();
    }

    public bool CanPlaceArtifact(Artifact artifact)
    {
        // 1. Comprobar el límite total de artefactos
        if (activeArtifacts.Count >= GameManager.Instance.maxArtifacts)
        {
            Debug.Log($"LÍMITE TOTAL DE ARTEFACTOS ALCANZADO: ({activeArtifacts.Count}/{GameManager.Instance.maxArtifacts})");
            return false;
        }

        // 2. Comprobar el límite para la categoría específica del artefacto
        int currentCategoryCount = activeCategoryCounts.ContainsKey(artifact.category) ? activeCategoryCounts[artifact.category] : 0;
        int categoryLimit = GameManager.Instance.GetArtifactLimitForCategory(artifact.category);

        if (currentCategoryCount >= categoryLimit)
        {
            Debug.Log($"LÍMITE DE CATEGORÍA '{artifact.category}' ALCANZADO: ({currentCategoryCount}/{categoryLimit})");
            return false;
        }

        return true;
    }

    public void PlaceArtifact(ArtifactIconController benchIcon)
    {
        if (benchIcon == null || benchIcon.artifactData == null) return;
        
        Artifact artifactToPlace = benchIcon.artifactData;

        if (!CanPlaceArtifact(artifactToPlace)) return;

        GameObject activeIconGO = Instantiate(activeArtifactIconPrefab, activeArtifactsContainer);
        
        if (activeIconGO != null)
        {
            ActiveArtifactIcon activeIconScript = activeIconGO.GetComponent<ActiveArtifactIcon>();
            activeIconScript.Initialize(benchIcon);
            activeArtifacts.Add(activeIconScript);

            // Incrementar el contador para esa categoría
            if (!activeCategoryCounts.ContainsKey(artifactToPlace.category))
            {
                activeCategoryCounts[artifactToPlace.category] = 0;
            }
            activeCategoryCounts[artifactToPlace.category]++;
            
            benchIcon.SetAsPlaced();
            UpdateUI();
        }
    }

    public void RemoveArtifact(ActiveArtifactIcon activeIcon)
    {
        if (activeIcon == null || activeIcon.originatingBenchIcon == null) return;
        
        Artifact artifactToRemove = activeIcon.originatingBenchIcon.artifactData;
        
        // Decrementar el contador para esa categoría
        if (activeCategoryCounts.ContainsKey(artifactToRemove.category))
        {
            activeCategoryCounts[artifactToRemove.category]--;
        }

        activeIcon.originatingBenchIcon.ResetIcon();
        activeArtifacts.Remove(activeIcon);
        Destroy(activeIcon.gameObject);

        UpdateUI();
    }

    public void ValidateActiveArtifacts()
    {
        // Creamos un diccionario temporal para contar cuántos artefactos de cada categoría vamos a conservar.
        Dictionary<ArtifactCategory, int> keptCategoryCounts = new Dictionary<ArtifactCategory, int>();

        // Es crucial iterar sobre una COPIA de la lista (.ToList()) porque vamos a eliminar
        // elementos de la lista original mientras la recorremos, lo que causaría errores.
        foreach (var activeIcon in activeArtifacts.ToList())
        {
            if (activeIcon == null || activeIcon.originatingBenchIcon == null) continue;

            Artifact artifact = activeIcon.originatingBenchIcon.artifactData;
            int limitForCategory = GameManager.Instance.GetArtifactLimitForCategory(artifact.category);

            // Obtenemos el recuento actual de artefactos que hemos decidido conservar para esta categoría.
            int currentKeptCount = keptCategoryCounts.ContainsKey(artifact.category) ? keptCategoryCounts[artifact.category] : 0;

            // Si ya hemos conservado el número máximo permitido para esta categoría, este artefacto debe ser eliminado.
            if (currentKeptCount >= limitForCategory)
            {
                // La función RemoveArtifact se encarga de todo: actualizar contadores, destruir el objeto y resetear el icono de la banca.
                RemoveArtifact(activeIcon);
            }
            else
            {
                // Si aún hay espacio, conservamos este artefacto y aumentamos nuestro contador de "conservados".
                keptCategoryCounts[artifact.category] = currentKeptCount + 1;
            }
        }

        // Una vez validadas las categorías, hacemos una última comprobación del límite total.
        // Esto es útil si la suma de los límites de categoría es mayor que el límite total permitido.
        while (activeArtifacts.Count > GameManager.Instance.maxArtifacts)
        {
            // Si todavía estamos por encima del límite, eliminamos el último artefacto de la lista.
            if (activeArtifacts.Count > 0)
            {
                RemoveArtifact(activeArtifacts[activeArtifacts.Count - 1]);
            }
        }

        // La función RemoveArtifact ya llama a UpdateUI, pero una llamada final asegura que el contador total esté correcto.
        UpdateUI();
    }

    public void FindAndClearEquippedIcon(Artifact artifactToFind)
{
    foreach (var activeIcon in activeArtifacts)
    {
        if (activeIcon.originatingBenchIcon.artifactData == artifactToFind)
        {
            activeIcon.ClearEquippedStatus();
            return;
        }
    }
}

    private void UpdateUI()
    {
        // Actualizar mensaje de "vacío"
        if (noArtifactsMessageObject != null)
        {
            noArtifactsMessageObject.SetActive(activeArtifacts.Count == 0);
        }

        // Actualizar contador de texto
        if (activeArtifactsCountText != null)
        {
            activeArtifactsCountText.text = $"{activeArtifacts.Count}/{GameManager.Instance.maxArtifacts}";
        }
    }
}