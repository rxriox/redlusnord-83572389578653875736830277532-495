using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PanelUnitDetails : MonoBehaviour
{
    public static PanelUnitDetails Instance { get; private set; }

    [Header("Panel de Detalles de Unidad")]
    public GameObject detailsPanel;
    public GameObject deathEffectOverlay;
    public TextMeshProUGUI unitNameText;
    public Image portraitImage;
    public Image backgroundImage;
    public TextMeshProUGUI levelText;
    public Slider healthBarSlider;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI attackDamageText;
    public TextMeshProUGUI attackSpeedText;
    public TextMeshProUGUI moveSpeedText;
    public List<Image> abilityIconSlots = new List<Image>();
    public List<Image> spellCardIconSlots = new List<Image>();

    [Header("Componentes del Panel")]
    public UnitController selectedUnitForDetails { get; private set; }
    public Image equippedArtifactImage;
    public List<Image> harmonyIconImages;
    public Sprite defaultArtifactSprite;
    public Button closeDetailsButton;
    public PanelInGameGradient detailsPanelGradient;
    public CanvasGroup detailsPanelCanvasGroup;

    [Header("Configuración de Degradados de Rareza")]
    public Color fabulosaColorTop = new Color(0.1f, 0.2f, 0.6f);
    public Color fabulosaColorBottom = Color.black;
    public Color magnificaColorTop = new Color(0.4f, 0.1f, 0.6f);
    public Color magnificaColorBottom = Color.black;
    public Color supremaColorTop = new Color(0.7f, 0.6f, 0.1f);
    public Color supremaColorBottom = Color.black;

    [Header("Animación del Panel")]
    public float panelFadeDuration = 0.1f;
    public float panelSlideOffset = 50f;
    private Vector2 panelOriginalPosition;
    private Coroutine panelFadeCoroutine;

    [Header("Highlight de Selección")]
    public GameObject selectionHighlightPrefab;
    private GameObject activeSelectionHighlight;

    public bool IsDetailsPanelActive => detailsPanelCanvasGroup != null && detailsPanelCanvasGroup.alpha > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        if (closeDetailsButton != null)
        {
            closeDetailsButton.onClick.AddListener(HideDetailsPanel);
        }
        if (detailsPanelCanvasGroup != null)
        {
            detailsPanelCanvasGroup.alpha = 0f;
            detailsPanelCanvasGroup.interactable = false;
            detailsPanelCanvasGroup.blocksRaycasts = false;
        }
        else if (detailsPanel != null)
        {
            detailsPanel.SetActive(false);
        }
        if (detailsPanel != null)
        {
            panelOriginalPosition = detailsPanel.GetComponent<RectTransform>().anchoredPosition;
        }
        if (deathEffectOverlay != null)
        {
            deathEffectOverlay.SetActive(false);
        }
    }

    private void Update()
    {
        if (selectedUnitForDetails != null && IsDetailsPanelActive)
        {
            levelText.text = $"{selectedUnitForDetails.CurrentLevel}";
            if (selectedUnitForDetails.CurrentHealth < selectedUnitForDetails.MaxHealth)
            {
                healthText.text = $"{Mathf.CeilToInt(selectedUnitForDetails.CurrentHealth)} / {selectedUnitForDetails.MaxHealth}";
            }
            else
            {
                healthText.text = selectedUnitForDetails.MaxHealth.ToString();
            }

            if (healthBarSlider != null)
            {
                if (selectedUnitForDetails.MaxHealth > 0)
                {
                    healthBarSlider.value = selectedUnitForDetails.CurrentHealth / selectedUnitForDetails.MaxHealth;
                }
            }

            attackDamageText.text = selectedUnitForDetails.CurrentAttackDamage.ToString();
            attackSpeedText.text = selectedUnitForDetails.CurrentAttackSpeed.ToString("F1");
            moveSpeedText.text = selectedUnitForDetails.CurrentMoveSpeed.ToString("F0");

            if (selectedUnitForDetails.EquippedArtifact != null)
            {
                equippedArtifactImage.sprite = selectedUnitForDetails.EquippedArtifact.icon;
            }
            else
            {
                equippedArtifactImage.sprite = defaultArtifactSprite;
            }
        }
    }

    public void ShowDetailsForBenchUnit(UnitStats stats)
    {
        if (stats == null) return;
        selectedUnitForDetails = null; // Es una unidad del banquillo, no una instancia real
        UpdatePanelContent(stats);
    }

    public void ShowDetailsForPlacedUnit(UnitController unit)
    {
        if (unit == null) return;
        selectedUnitForDetails = unit;
        UpdatePanelContent(unit.unitStats);

        if (unit.CurrentHealth <= 0 && deathEffectOverlay != null)
        {
            deathEffectOverlay.SetActive(true);
        }

        if (activeSelectionHighlight != null) Destroy(activeSelectionHighlight);
        if (selectionHighlightPrefab != null)
        {
            activeSelectionHighlight = Instantiate(selectionHighlightPrefab, unit.transform.position, Quaternion.identity);
            HighlightFollower follower = activeSelectionHighlight.GetComponent<HighlightFollower>();
            if (follower != null) follower.targetToFollow = unit.transform;
        }
    }

    private void UpdatePanelContent(UnitStats stats)
    {
        if (stats == null || detailsPanelCanvasGroup == null) return;
        if (deathEffectOverlay != null) deathEffectOverlay.SetActive(false);
        if (healthBarSlider != null) healthBarSlider.value = 1;

        if (panelFadeCoroutine != null) StopCoroutine(panelFadeCoroutine);

        // Actualizar contenido visual
        if (portraitImage != null) portraitImage.sprite = stats.portrait;
        if (backgroundImage != null) backgroundImage.sprite = stats.backgroundImage;
        if (equippedArtifactImage != null) equippedArtifactImage.sprite = defaultArtifactSprite;

        int currentLevel = GameManager.Instance.GetCurrentLevelForUnit(stats);
        unitNameText.text = "Loading...";
        var nameHandle = stats.unitName.GetLocalizedStringAsync();
        nameHandle.Completed += (handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // Cuando la carga termina, actualiza el texto.
                unitNameText.text = handle.Result;
            }
            else
            {
                unitNameText.text = "Error";
            }
            // Addressables.Release(handle); // Opcional: Descomenta si gestionas la memoria manualmente.
        };
        levelText.text = $"{currentLevel}";

        // Stats
        if (selectedUnitForDetails != null) // Si es una unidad del tablero
        {
            healthText.text = $"{Mathf.CeilToInt(selectedUnitForDetails.CurrentHealth)} / {selectedUnitForDetails.MaxHealth}";
            attackDamageText.text = selectedUnitForDetails.CurrentAttackDamage.ToString();
        }
        else // Si es una unidad del banquillo
        {
            int maxHealth = (stats.maxHealthByLevel.Count >= currentLevel && currentLevel > 0) ? stats.maxHealthByLevel[currentLevel - 1] : 0;
            int attackDamage = (stats.attackDamageByLevel.Count >= currentLevel && currentLevel > 0) ? stats.attackDamageByLevel[currentLevel - 1] : 0;
            healthText.text = maxHealth.ToString();
            attackDamageText.text = attackDamage.ToString();
        }

        attackSpeedText.text = stats.attackSpeed.ToString("F1");
        moveSpeedText.text = stats.moveSpeed.ToString("F1");

        // Habilidades y Hechizos
        for (int i = 0; i < abilityIconSlots.Count; i++)
        {
            abilityIconSlots[i].gameObject.SetActive(i < stats.abilities.Count && stats.abilities[i] != null);
            if (abilityIconSlots[i].gameObject.activeSelf) abilityIconSlots[i].sprite = stats.abilities[i].icon;
        }
        for (int i = 0; i < spellCardIconSlots.Count; i++)
        {
            spellCardIconSlots[i].gameObject.SetActive(i < stats.spellCards.Count && stats.spellCards[i] != null);
            if (spellCardIconSlots[i].gameObject.activeSelf) spellCardIconSlots[i].sprite = stats.spellCards[i].icon;
        }

        // Degradado de rareza
        if (detailsPanelGradient != null)
        {
            switch (stats.category)
            {
                case UnitStats.UnitCategory.Fabulosa: detailsPanelGradient.m_color1 = fabulosaColorTop; detailsPanelGradient.m_color2 = fabulosaColorBottom; break;
                case UnitStats.UnitCategory.Magnifica: detailsPanelGradient.m_color1 = magnificaColorTop; detailsPanelGradient.m_color2 = magnificaColorBottom; break;
                case UnitStats.UnitCategory.Suprema: detailsPanelGradient.m_color1 = supremaColorTop; detailsPanelGradient.m_color2 = supremaColorBottom; break;
            }
            detailsPanelGradient.Refresh();
        }

        // Armonías
        if (harmonyIconImages != null)
        {
            for (int i = 0; i < harmonyIconImages.Count; i++)
            {
                harmonyIconImages[i].gameObject.SetActive(i < stats.naturalHarmonies.Count);
                if (harmonyIconImages[i].gameObject.activeSelf) harmonyIconImages[i].sprite = stats.naturalHarmonies[i].activeIcon;
            }
        }

        panelFadeCoroutine = StartCoroutine(AnimateDetailsPanel(true));
    }

    public void HideDetailsPanel()
    {
        selectedUnitForDetails = null;
        if (detailsPanelCanvasGroup == null || detailsPanelCanvasGroup.alpha == 0) return;

        if (PlayerController.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Placement)
        {
            PlayerController.Instance.ClearInteractionState();
        }

        // ===== MODIFICACIÓN #1: Lógica de destrucción centralizada =====
        // Destruye el highlight y anula la referencia para evitar problemas.
        if (activeSelectionHighlight != null)
        {
            Destroy(activeSelectionHighlight);
            activeSelectionHighlight = null;
        }

        if (deathEffectOverlay != null)
        {
            deathEffectOverlay.SetActive(false);
        }

        panelFadeCoroutine = StartCoroutine(AnimateDetailsPanel(false));
    }

    private IEnumerator AnimateDetailsPanel(bool fadeIn)
    {
        RectTransform panelRect = detailsPanel.GetComponent<RectTransform>();
        float startAlpha = detailsPanelCanvasGroup.alpha;
        float endAlpha = fadeIn ? 1f : 0f;

        Vector2 startPosition = panelRect.anchoredPosition;
        Vector2 endPosition;

        if (fadeIn)
        {
            startPosition = panelOriginalPosition + new Vector2(panelSlideOffset, 0);
            endPosition = panelOriginalPosition;
        }
        else
        {
            endPosition = panelOriginalPosition + new Vector2(panelSlideOffset, 0);
        }

        float elapsedTime = 0f;
        while (elapsedTime < panelFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / panelFadeDuration;

            detailsPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            panelRect.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);

            yield return null;
        }

        detailsPanelCanvasGroup.alpha = endAlpha;
        panelRect.anchoredPosition = endPosition;

        detailsPanelCanvasGroup.interactable = fadeIn;
        detailsPanelCanvasGroup.blocksRaycasts = fadeIn;

        panelFadeCoroutine = null;
    }

    public void ForceDetailsPanelUpdate(UnitController unit)
    {
        if (unit != null && selectedUnitForDetails == unit && IsDetailsPanelActive)
        {
            if (unit.CurrentHealth <= 0)
            {
                healthText.text = $"0 / {unit.MaxHealth}";
                if (deathEffectOverlay != null) deathEffectOverlay.SetActive(true);
                if (healthBarSlider != null) healthBarSlider.value = 0;
            }
            else
            {
                // Este update se maneja en el método Update() principal
            }
        }
    }
    
    public void HideSelectionHighlight()
    {
        if (activeSelectionHighlight != null)
        {
            Destroy(activeSelectionHighlight);
            activeSelectionHighlight = null;
        }
    }
}