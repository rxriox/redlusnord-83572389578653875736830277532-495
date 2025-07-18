using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class ArtifactManager : MonoBehaviour
{
    public static ArtifactManager Instance { get; private set; }

    [Header("Referencias de UI por Equipo")]
    [Tooltip("El objeto padre que contendrá los iconos de artefactos del jugador.")]
    public Transform playerArtifactsContainer;
    [Tooltip("El objeto padre que contendrá los iconos de artefactos del enemigo.")]
    public Transform enemyArtifactsContainer;
    [Tooltip("El mensaje que aparece cuando el jugador no tiene artefactos.")]
    public GameObject playerNoArtifactsMessage;
    [Tooltip("El mensaje que aparece cuando el enemigo no tiene artefactos.")]
    public GameObject enemyNoArtifactsMessage;

    public GameObject activeArtifactIconPrefab;

    private Dictionary<int, List<ActiveArtifactIcon>> teamActiveArtifacts = new Dictionary<int, List<ActiveArtifactIcon>>();
    private Dictionary<int, Dictionary<ArtifactCategory, int>> teamCategoryCounts = new Dictionary<int, Dictionary<ArtifactCategory, int>>();

    private int currentPerspectiveTeamID = 0;
    private bool isArtifactTabActive = false; // NUEVA VARIABLE

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else
        {
            Instance = this;
            teamActiveArtifacts[0] = new List<ActiveArtifactIcon>();
            teamActiveArtifacts[1] = new List<ActiveArtifactIcon>();
            teamCategoryCounts[0] = new Dictionary<ArtifactCategory, int>();
            teamCategoryCounts[1] = new Dictionary<ArtifactCategory, int>();
        }
    }

    void Start()
    {
        SetPerspective(0);
        // Asegúrate de que los contenedores estén ocultos al inicio, si la pestaña no está activa
        if (playerArtifactsContainer != null) playerArtifactsContainer.gameObject.SetActive(false);
        if (enemyArtifactsContainer != null) enemyArtifactsContainer.gameObject.SetActive(false);
        if (playerNoArtifactsMessage != null) playerNoArtifactsMessage.SetActive(false);
        if (enemyNoArtifactsMessage != null) enemyNoArtifactsMessage.SetActive(false);
    }

    public void SetPerspective(int teamID)
    {
        currentPerspectiveTeamID = teamID;
        // Solo activa los contenedores si la pestaña de artefactos está activa
        if (isArtifactTabActive)
        {
            if (playerArtifactsContainer != null) playerArtifactsContainer.gameObject.SetActive(teamID == 0);
            if (enemyArtifactsContainer != null) enemyArtifactsContainer.gameObject.SetActive(teamID == 1);
        }
        else
        {
            if (playerArtifactsContainer != null) playerArtifactsContainer.gameObject.SetActive(false);
            if (enemyArtifactsContainer != null) enemyArtifactsContainer.gameObject.SetActive(false);
        }

        UpdateUI();
    }

    public int GetActiveArtifactCountForTeam(int teamID)
    {
        return teamActiveArtifacts.ContainsKey(teamID) ? teamActiveArtifacts[teamID].Count : 0;
    }

    public bool CanPlaceArtifact(Artifact artifactData, int teamID)
    {
        if (teamActiveArtifacts[teamID].Count >= GameManager.Instance.maxArtifacts)
        {
            GameManager.Instance.ShowPlacementError("Haz alcanzado el máximo de artefactos disponibles");
            return false;
        }

        teamCategoryCounts[teamID].TryGetValue(artifactData.category, out int currentCategoryCount);
        int categoryLimit = GameManager.Instance.GetArtifactLimitForCategory(artifactData.category);

        if (currentCategoryCount >= categoryLimit)
        {
            GameManager.Instance.ShowPlacementError($"Límite de artefactos de categoría '{artifactData.category}' alcanzado");
            return false;
        }

        return true;
    }

    public void PlaceArtifact(ArtifactIconController benchIcon)
    {
        int teamID = currentPerspectiveTeamID;

        if (benchIcon == null || benchIcon.artifactData == null || !CanPlaceArtifact(benchIcon.artifactData, teamID))
            return;

        Transform container = (teamID == 0) ? playerArtifactsContainer : enemyArtifactsContainer;

        GameObject activeIconGO = Instantiate(activeArtifactIconPrefab, container);
        ActiveArtifactIcon activeIconScript = activeIconGO.GetComponent<ActiveArtifactIcon>();

        activeIconScript.Initialize(benchIcon, teamID);
        teamActiveArtifacts[teamID].Add(activeIconScript);

        teamCategoryCounts[teamID].TryGetValue(benchIcon.artifactData.category, out int count);
        teamCategoryCounts[teamID][benchIcon.artifactData.category] = count + 1;

        benchIcon.SetAsPlaced();
        UpdateUI();
        GameManager.Instance.UpdateAllCountsUI();
    }

    public void RemoveArtifact(ActiveArtifactIcon activeIcon)
    {
        if (activeIcon == null || activeIcon.originatingBenchIcon == null) return;

        int teamID = activeIcon.teamID;

        activeIcon.ClearEquippedStatus();

        Artifact artifactToRemove = activeIcon.originatingBenchIcon.artifactData;
        if (teamCategoryCounts.ContainsKey(teamID) && teamCategoryCounts[teamID].ContainsKey(artifactToRemove.category))
        {
            teamCategoryCounts[teamID][artifactToRemove.category]--;
        }

        teamActiveArtifacts[teamID].Remove(activeIcon);
        activeIcon.originatingBenchIcon.ResetIcon();
        Destroy(activeIcon.gameObject);

        UpdateUI();
        GameManager.Instance.UpdateAllCountsUI();
    }

    private void UpdateUI()
    {
        bool isCurrentContainerEmpty = teamActiveArtifacts[currentPerspectiveTeamID].Count == 0;

        if (currentPerspectiveTeamID == 0)
        {
            if (playerNoArtifactsMessage != null)
                playerNoArtifactsMessage.SetActive(isArtifactTabActive && isCurrentContainerEmpty);
            if (enemyNoArtifactsMessage != null)
                enemyNoArtifactsMessage.SetActive(false);
        }
        else
        {
            if (enemyNoArtifactsMessage != null)
                enemyNoArtifactsMessage.SetActive(isArtifactTabActive && isCurrentContainerEmpty);
            if (playerNoArtifactsMessage != null)
                playerNoArtifactsMessage.SetActive(false);
        }
    }

    public void ValidateActiveArtifacts()
    {
        foreach (var teamID in teamActiveArtifacts.Keys.ToList())
        {
            var keptCategoryCounts = new Dictionary<ArtifactCategory, int>();
            var artifactList = teamActiveArtifacts[teamID].ToList();

            foreach (var activeIcon in artifactList)
            {
                if (activeIcon == null || activeIcon.originatingBenchIcon == null) continue;

                Artifact artifact = activeIcon.originatingBenchIcon.artifactData;
                int limit = GameManager.Instance.GetArtifactLimitForCategory(artifact.category);
                int current = keptCategoryCounts.TryGetValue(artifact.category, out int count) ? count : 0;

                if (current >= limit)
                {
                    RemoveArtifact(activeIcon);
                }
                else
                {
                    keptCategoryCounts[artifact.category] = current + 1;
                }
            }

            while (teamActiveArtifacts[teamID].Count > GameManager.Instance.maxArtifacts)
            {
                if (teamActiveArtifacts[teamID].Count > 0)
                {
                    RemoveArtifact(teamActiveArtifacts[teamID].Last());
                }
            }
        }

        UpdateUI();
    }

    public void ResetAllArtifactIconsState()
    {
        foreach (var artifactList in teamActiveArtifacts.Values)
        {
            foreach (var icon in artifactList)
            {
                if (icon != null)
                {
                    icon.ClearEquippedStatus();
                }
            }
        }
    }

    public void FindAndClearEquippedIcon(Artifact artifactToFind)
    {
        foreach (var artifactList in teamActiveArtifacts.Values)
        {
            var activeIcon = artifactList.FirstOrDefault(icon => icon.originatingBenchIcon.artifactData == artifactToFind);
            if (activeIcon != null)
            {
                activeIcon.ClearEquippedStatus();
            }
        }
    }

    public void SetArtifactPanelActive(bool isActive)
    {
        isArtifactTabActive = isActive; // ACTUALIZA LA NUEVA VARIABLE

        if (isActive)
        {
            SetPerspective(currentPerspectiveTeamID);
        }
        else
        {
            if (playerArtifactsContainer != null) playerArtifactsContainer.gameObject.SetActive(false);
            if (enemyArtifactsContainer != null) enemyArtifactsContainer.gameObject.SetActive(false);

            if (playerNoArtifactsMessage != null) playerNoArtifactsMessage.SetActive(false);
            if (enemyNoArtifactsMessage != null) enemyNoArtifactsMessage.SetActive(false);
        }
    }

    public void DetachAllIconsFromUnits()
    {
        foreach (var artifactList in teamActiveArtifacts.Values)
        {
            foreach (var icon in artifactList)
            {
                if (icon != null && icon.IsEquipped)
                {
                    icon.DetachFromUnit();
                }
            }
        }
    }

    public void ReEquipArtifactToUnit(UnitController unit, Artifact artifactToEquip)
    {
        // Primero, equipa el artefacto en la unidad para aplicar sus efectos
        unit.EquipArtifact(artifactToEquip);

        // Luego, encuentra el icono del artefacto activo correspondiente y lo vincula a la nueva unidad
        var activeIcon = teamActiveArtifacts[unit.teamID]
            .FirstOrDefault(icon => icon.originatingBenchIcon.artifactData == artifactToEquip);

        if (activeIcon != null)
        {
            activeIcon.LinkToUnit(unit);
        }
    }

}
