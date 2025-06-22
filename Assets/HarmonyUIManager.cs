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

        Dictionary<HarmonyType, int> harmonyCounts = GameManager.Instance.GetHarmonyCountsForTeam(teamIdToShow);

        // Ocultamos todos los iconos existentes para "limpiar" el panel antes de redibujar.
        // Esto es más eficiente que Destruir y Crear cada vez.
        foreach (var icon in spawnedIcons.Values)
        {
            icon.SetActive(false);
        }

        foreach (var harmonyInfo in harmonyCounts)
        {
            HarmonyType type = harmonyInfo.Key;
            int count = harmonyInfo.Value;
            
            // --- INICIO DE LA CORRECCIÓN ---
            // Ahora le preguntamos al GameManager si la armonía está activa para nuestro equipo.
            bool isHarmonyActive = GameManager.Instance.IsHarmonyActiveForTeam(type, teamIdToShow);
            // --- FIN DE LA CORRECCIÓN ---

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
            
            // Lógica para configurar el icono (se mantiene igual)
            Image iconImage = iconGO.GetComponentInChildren<Image>();
            Text iconText = iconGO.GetComponentInChildren<Text>();

            if (iconImage != null)
                iconImage.sprite = isHarmonyActive ? type.activeIcon : type.inactiveIcon;
            
            if (iconText != null)
            {
                // Mostramos el conteo actual. Podríamos añadir el requerido para el siguiente tier.
                iconText.text = count.ToString();
                iconText.color = isHarmonyActive ? Color.cyan : Color.white; // Feedback extra
            }
            
            iconGO.transform.SetParent(isHarmonyActive ? activeHarmoniesContainer : inactiveHarmoniesContainer, false);
        }
    }
}