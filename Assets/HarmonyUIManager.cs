using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
    public GameObject noUnitsMessageObject;

    private void OnEnable()
    {
        GameManager.OnHarmoniesUpdated += UpdateDisplay;
    }

    private void OnDisable()
    {
        GameManager.OnHarmoniesUpdated -= UpdateDisplay;
    }

    void UpdateDisplay()
    {
        if (GameManager.Instance == null) return;

        // --- INICIO DE LA CORRECCIÓN ---
        // Solo actualizamos la visualización si estamos en la fase de colocación.
        if (GameManager.Instance.CurrentState != GameManager.GameState.Placement)
        {
            // Si estamos en combate o en la pantalla de resultados, no hacemos nada.
            // El panel mantendrá el estado que tenía justo antes de empezar la batalla.
            return; 
        }
        // --- FIN DE LA CORRECCIÓN ---

        // El resto de la función se ejecuta con normalidad solo durante la fase de colocación.
        int totalPlayerUnits = GameManager.Instance.GetUnitCountForTeam(teamIdToShow);

        if (totalPlayerUnits == 0)
        {
            if (noUnitsMessageObject != null) noUnitsMessageObject.SetActive(true);
            activeHarmoniesContainer.gameObject.SetActive(false);
            inactiveHarmoniesContainer.gameObject.SetActive(false);
            return;
        }
        
        if (noUnitsMessageObject != null) noUnitsMessageObject.SetActive(false);
        activeHarmoniesContainer.gameObject.SetActive(true);
        inactiveHarmoniesContainer.gameObject.SetActive(true);

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
                iconImage.sprite = isHarmonyActive ? type.activeIcon : type.inactiveIcon;
            
            if (iconText != null)
            {
                iconText.text = count.ToString();
                iconText.color = isHarmonyActive ? Color.cyan : Color.white;
            }
            
            iconGO.transform.SetParent(isHarmonyActive ? activeHarmoniesContainer : inactiveHarmoniesContainer, false);
        }
    }
}