using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class ArtifactManager : MonoBehaviour
{
    public static ArtifactManager Instance { get; private set; }

    [Header("Referencias de UI")]
    public Transform activeArtifactsContainer;
    public GameObject noArtifactsContainer;
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
        ReorderActiveIcons();
    }

    public int GetActiveArtifactCount()
    {
        return activeArtifacts.Count;
    }

    public bool CanPlaceArtifact(Artifact artifact)
    {
        if (activeArtifacts.Count >= GameManager.Instance.maxArtifacts)
        {
            string message = "Haz alcanzado el maximo de artefactos disponibles";

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowPlacementError(message);
            }

            Debug.Log(message + $" ({activeArtifacts.Count}/{GameManager.Instance.maxArtifacts})");

            return false;
        }

        int currentCategoryCount = activeCategoryCounts.ContainsKey(artifact.category) ? activeCategoryCounts[artifact.category] : 0;
        int categoryLimit = GameManager.Instance.GetArtifactLimitForCategory(artifact.category);

        if (currentCategoryCount >= categoryLimit)
        {
            string message = $"Límite de artefactos de categoría '{artifact.category}' alcanzado";
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowPlacementError(message);
            }
            Debug.Log(message + $" ({currentCategoryCount}/{categoryLimit})");
            return false;
        }

        return true;
    }

    public void PlaceArtifact(ArtifactIconController benchIcon)
    {
        if (benchIcon == null || benchIcon.artifactData == null || !CanPlaceArtifact(benchIcon.artifactData)) return;

        GameObject activeIconGO = Instantiate(activeArtifactIconPrefab, activeArtifactsContainer);
        ActiveArtifactIcon activeIconScript = activeIconGO.GetComponent<ActiveArtifactIcon>();

        activeIconScript.Initialize(benchIcon);
        activeArtifacts.Add(activeIconScript);

        activeCategoryCounts.TryGetValue(benchIcon.artifactData.category, out int count);
        activeCategoryCounts[benchIcon.artifactData.category] = count + 1;

        benchIcon.SetAsPlaced();
        GameManager.Instance.UpdateAllCountsUI();
        ReorderActiveIcons();

        UpdateUI();
    }

    public void RemoveArtifact(ActiveArtifactIcon activeIcon)
    {
        if (activeIcon == null || activeIcon.originatingBenchIcon == null) return;

        activeIcon.ClearEquippedStatus();

        Artifact artifactToRemove = activeIcon.originatingBenchIcon.artifactData;

        if (activeCategoryCounts.ContainsKey(artifactToRemove.category))
        {
            activeCategoryCounts[artifactToRemove.category]--;
        }

        activeIcon.originatingBenchIcon.ResetIcon();
        activeArtifacts.Remove(activeIcon);
        Destroy(activeIcon.gameObject);

        GameManager.Instance.UpdateAllCountsUI();
        ReorderActiveIcons();

        UpdateUI();
    }

    public void FindAndClearEquippedIcon(Artifact artifactToFind)
    {
        var activeIcon = activeArtifacts.FirstOrDefault(icon => icon.originatingBenchIcon.artifactData == artifactToFind);
        if (activeIcon != null)
        {
            activeIcon.ClearEquippedStatus();
        }
    }

    public void ValidateActiveArtifacts()
    {
        var keptCategoryCounts = new Dictionary<ArtifactCategory, int>();

        foreach (var activeIcon in activeArtifacts.ToList())
        {
            if (activeIcon == null || activeIcon.originatingBenchIcon == null) continue;

            Artifact artifact = activeIcon.originatingBenchIcon.artifactData;
            int limitForCategory = GameManager.Instance.GetArtifactLimitForCategory(artifact.category);
            int currentKeptCount = keptCategoryCounts.TryGetValue(artifact.category, out int count) ? count : 0;

            if (currentKeptCount >= limitForCategory)
            {
                RemoveArtifact(activeIcon);
            }
            else
            {
                keptCategoryCounts[artifact.category] = currentKeptCount + 1;
            }
        }

        while (activeArtifacts.Count > GameManager.Instance.maxArtifacts)
        {
            if (activeArtifacts.Count > 0)
            {
                RemoveArtifact(activeArtifacts[activeArtifacts.Count - 1]);
            }
        }

        UpdateUI();
        ReorderActiveIcons();
    }

    private void UpdateUI()
    {
        bool hasArtifacts = activeArtifacts.Count > 0;
        
        if (noArtifactsContainer != null)
        {
            noArtifactsContainer.SetActive(!hasArtifacts);
        }
    }

    public void ResetAllArtifactIconsState()
    {
        foreach (var activeIcon in activeArtifacts)
        {
            if (activeIcon != null)
            {
                activeIcon.ClearEquippedStatus();
            }
        }
    }
    private void ReorderActiveIcons()
    {
        if (activeArtifactsContainer == null || activeArtifacts.Count < 2) return;

        var sortedIcons = activeArtifacts.OrderByDescending(icon => icon.IsEquipped).ToList();
        for (int i = 0; i < sortedIcons.Count; i++)
        {
            sortedIcons[i].transform.SetSiblingIndex(i);
        }
    }
}
