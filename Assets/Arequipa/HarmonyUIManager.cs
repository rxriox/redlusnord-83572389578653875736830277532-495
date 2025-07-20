using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(AudioSource))]
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

    void Start()
    {
        if (timerPanel != null)
        {
            timerPanel.SetActive(false);
            timerPanelImage = timerPanel.GetComponent<Image>();
            if (timerText != null)
            {
                timerPulseEffect = timerText.GetComponent<UIPulseEffect>();
            }
        }
        audioSource = GetComponent<AudioSource>();
    }

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

    [Header("Temporizador")]
    public GameObject timerPanel;
    public TextMeshProUGUI timerText;

    [Tooltip("El color del panel cuando el tiempo está entre 10 y 4 segundos.")]
    public Color warningColor = Color.black;
    [Tooltip("El color del panel cuando el tiempo está entre 3 y 0 segundos.")]
    public Color dangerColor = Color.red;

    [Header("Sonidos del Temporizador")]
    [Tooltip("Sonido para los segundos 5, 4 y 3.")]
    public AudioClip normalTickSound;
    [Tooltip("Sonido para el segundo 2.")]
    public AudioClip finalTickSound2;
    [Tooltip("Sonido para el segundo 1.")]
    public AudioClip finalTickSound1;
    [Tooltip("Sonido para el segundo 0 (final del combate).")]
    public AudioClip finalTickSound0;

    private Image timerPanelImage;
    private UIPulseEffect timerPulseEffect;
    private int lastSecondDisplayed = -1;
    private AudioSource audioSource;


    void Update()
    {
        if (GameManager.Instance == null || timerText == null || timerPanel == null) return;

        if (GameManager.Instance.CurrentState == GameManager.GameState.Combat)
        {
            float timeLeft = GameManager.BATTLE_TIME_LIMIT - GameManager.Instance.battleTimer;

            if (timeLeft <= 10.99f)
            {
                timerPanel.SetActive(true);

                int currentSecond = Mathf.CeilToInt(timeLeft);
                currentSecond = Mathf.Clamp(currentSecond, 0, 10);

                if (currentSecond != lastSecondDisplayed)
                {
                    lastSecondDisplayed = currentSecond;
                    timerText.text = currentSecond.ToString();

                    if (timerPanelImage != null)
                    {
                        timerPanelImage.color = (currentSecond <= 3) ? dangerColor : warningColor;
                    }

                    if (timerPulseEffect != null)
                    {
                        timerPulseEffect.PlayPulse();
                    }

                    // ===== MODIFICACIÓN #4: Lógica para reproducir el sonido correcto =====
                    PlayTimerSound(currentSecond);
                }
            }
            else
            {
                timerPanel.SetActive(false);
                lastSecondDisplayed = -1;
            }
        }
        else
        {
            timerPanel.SetActive(false);
            lastSecondDisplayed = -1;
        }
    }
    
    private void PlayTimerSound(int second)
    {
        AudioClip clipToPlay = null;

        switch (second)
        {
            case 5:
            case 4:
            case 3:
                clipToPlay = normalTickSound;
                break;
            case 2:
                clipToPlay = finalTickSound2;
                break;
            case 1:
                clipToPlay = finalTickSound1;
                break;
            case 0:
                clipToPlay = finalTickSound0;
                break;
        }

        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }
}