using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class HarmonyUIManager : MonoBehaviour
{
    [Header("Referencias de UI")]
    public Transform activeHarmoniesContainer;
    public Transform inactiveHarmoniesContainer;
    public GameObject harmonyIconPrefab;

    [Header("Configuración")]
    public int teamIdToShow = 0;
    private Dictionary<HarmonyType, GameObject> spawnedIcons = new Dictionary<HarmonyType, GameObject>();
    [Header("UI Mensajes")]
    [Tooltip("El objeto de texto que se muestra cuando no hay unidades.")]
    public GameObject noUnitsContainer;

    private void OnEnable()
    {
        GameManager.OnHarmoniesUpdated += UpdateDisplay;
    }

    private void OnDisable()
    {
        GameManager.OnHarmoniesUpdated -= UpdateDisplay;
    }

    public void SetTeamPerspective(int teamId)
    {
        teamIdToShow = teamId;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (GameManager.Instance == null) return;

        int totalPlayerUnits = GameManager.Instance.GetUnitCountForTeam(teamIdToShow);
        bool hasUnits = totalPlayerUnits > 0;
        activeHarmoniesContainer.gameObject.SetActive(hasUnits);
        inactiveHarmoniesContainer.gameObject.SetActive(hasUnits);

        if (noUnitsContainer != null)
        {
            noUnitsContainer.SetActive(!hasUnits);
        }

        if (!hasUnits)
        {
            foreach (var icon in spawnedIcons.Values)
            {
                icon.SetActive(false);
            }
            return;
        }

        Dictionary<HarmonyType, int> harmonyCounts = GameManager.Instance.GetHarmonyCountsForTeam(teamIdToShow);

        foreach (var icon in spawnedIcons.Values)
        {
            icon.SetActive(false);
        }

        foreach (var harmonyInfo in harmonyCounts)
        {
            HarmonyType type = harmonyInfo.Key;
            int count = harmonyInfo.Value;
            bool isHarmonyActive = GameManager.Instance.IsHarmonyActiveForTeam(type, teamIdToShow);
            GameObject iconGO;

            if (spawnedIcons.ContainsKey(type))
            {
                iconGO = spawnedIcons[type];
            }
            else
            {
                iconGO = Instantiate(harmonyIconPrefab);
                spawnedIcons[type] = iconGO;
            }

            iconGO.SetActive(true);

            Image iconImage = iconGO.GetComponentInChildren<Image>();
            Text iconText = iconGO.GetComponentInChildren<Text>();

            if (iconImage != null)
            {
                if (isHarmonyActive)
                {
                    iconImage.sprite = type.activeIcon;
                }
                else // La armonía está inactiva
                {
                    // Comprueba si hay un ícono específico para el número actual de unidades.
                    // El índice es 'count - 1' (1 unidad -> índice 0, 2 unidades -> índice 1, etc.)
                    if (type.inactiveIcons != null && count > 0 && count <= type.inactiveIcons.Count)
                    {
                        iconImage.sprite = type.inactiveIcons[count - 1];
                    }
                    else
                    {
                        // Si no hay un ícono específico, puedes decidir qué mostrar.
                        // Por ejemplo, el último de la lista o simplemente desactivarlo.
                        // Aquí usamos el último disponible como respaldo.
                        if(type.inactiveIcons != null && type.inactiveIcons.Count > 0)
                        {
                            iconImage.sprite = type.inactiveIcons[type.inactiveIcons.Count - 1];
                        }
                    }
                }
            }

            if (iconText != null)
            {
                iconText.text = count.ToString();
                iconText.color = isHarmonyActive ? Color.cyan : Color.white;
            }

            iconGO.transform.SetParent(
                isHarmonyActive ? activeHarmoniesContainer : inactiveHarmoniesContainer,
                false
            );
        }
    }
}