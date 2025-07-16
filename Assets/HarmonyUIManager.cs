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
                iconImage.sprite = isHarmonyActive ? type.activeIcon : type.inactiveIcon;

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

    public TextMeshProUGUI timerText;
    void Update()
    {
        if (GameManager.Instance == null || timerText == null) return;

        if (GameManager.Instance.CurrentState == GameManager.GameState.Combat)
        {
            timerText.gameObject.SetActive(true);
            float timeLeft = GameManager.BATTLE_TIME_LIMIT - GameManager.Instance.battleTimer;
            timeLeft = Mathf.Max(timeLeft, 0);
            timerText.text = timeLeft.ToString("F1");
        }
        else
        {
            timerText.gameObject.SetActive(false);
        }
    }
}