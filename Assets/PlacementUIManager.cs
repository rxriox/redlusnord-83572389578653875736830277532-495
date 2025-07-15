using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class PlacementUIManager : MonoBehaviour
{
    public static PlacementUIManager Instance { get; private set; }

    [Header("Configuración de Pestañas")]
    public Button allyBenchButton;
    public Button enemyBenchButton;
    public Button ArtifactsBenchButton;
    public Color activeTabColor = Color.black;
    public Color inactiveTabColor = Color.white;

    [Header("Contadores de Unidades")]
    public TextMeshProUGUI allyUnitCountText;
    public TextMeshProUGUI enemyUnitCountText;

    [Header("Banca Aliada")]
    public GameObject allyBenchScrollView;
    public Transform allyBenchContent;
    public GameObject[] allyIconPrefabs;
    public CanvasGroup allyBenchCanvasGroup;

    [Header("Banca Enemiga")]
    public GameObject enemyBenchScrollView;
    public Transform enemyBenchContent;
    public GameObject[] enemyIconPrefabs;
    public CanvasGroup enemyBenchCanvasGroup;

    [Header("Banca de Artefactos")]
    public GameObject artifactsBenchScrollView;
    public Transform artifactsBenchContent;
    public GameObject[] artifactIconPrefabs;
    public CanvasGroup artifactsBenchCanvasGroup;

    [Header("Panel de Detalles de Unidad")]
    public GameObject detailsPanel;
    public TextMeshProUGUI unitNameText;
    public Image portraitImage;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI attackDamageText;
    public TextMeshProUGUI attackSpeedText;
    public TextMeshProUGUI moveSpeedText;
    private UnitController selectedUnitForDetails;
    [Tooltip("La imagen en la UI que mostrará el icono del artefacto equipado.")]
    public Image equippedArtifactImage;
    [Tooltip("Lista de las imágenes en la UI destinadas a mostrar los iconos de las armonías.")]
    public List<Image> harmonyIconImages;
    [Tooltip("El sprite que se muestra cuando no hay ningún artefacto equipado.")]
    public Sprite defaultArtifactSprite;

    public Button closeDetailsButton;
    public PanelInGameGradient detailsPanelGradient;

    [Tooltip("Arrastra aquí el componente CanvasGroup del detailsPanel.")]
    public CanvasGroup detailsPanelCanvasGroup;
    private Coroutine panelFadeCoroutine;
    private Coroutine benchFadeCoroutine;
    public bool IsDetailsPanelActive => detailsPanelCanvasGroup != null && detailsPanelCanvasGroup.alpha > 0;

    [Header("Configuración de Degradados de Rareza")]
    public Color fabulosaColorTop = new Color(0.1f, 0.2f, 0.6f);
    public Color fabulosaColorBottom = Color.black;
    public Color magnificaColorTop = new Color(0.4f, 0.1f, 0.6f);
    public Color magnificaColorBottom = Color.black;
    public Color supremaColorTop = new Color(0.7f, 0.6f, 0.1f);
    public Color supremaColorBottom = Color.black;

    [Header("Animación")]
    public float fadeDuration = 0.1f;
    public float panelFadeDuration = 0.1f;
    public enum ActiveBench { Allies, Enemies, Artifacts }

    [Header("Highlight de Selección en Combate")]
    public GameObject selectionHighlightPrefab;
    private GameObject activeSelectionHighlight;

    private ActiveBench lastActiveBench;
    public int CurrentPlacementTeamID { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        PopulateBench(allyBenchContent, allyIconPrefabs);
        PopulateBench(enemyBenchContent, enemyIconPrefabs);
        PopulateBench(artifactsBenchContent, artifactIconPrefabs);
        ShowAllyBench();
        UpdateUnitCountDisplay();
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
        else
        {
            detailsPanel.SetActive(false);
        }
    }

    void Update()
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

            attackDamageText.text = selectedUnitForDetails.CurrentAttackDamage.ToString();
            attackSpeedText.text = selectedUnitForDetails.CurrentAttackSpeed.ToString("F0");
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

    public void ShowDetailsPanel(UnitStats stats)
    {
        if (stats == null || detailsPanelCanvasGroup == null) return;
        selectedUnitForDetails = null;
        if (panelFadeCoroutine != null)
        {
            StopCoroutine(panelFadeCoroutine);
        }

        if (portraitImage != null) portraitImage.sprite = stats.portrait;

        if (equippedArtifactImage != null)
        {
            equippedArtifactImage.sprite = defaultArtifactSprite;
        }

        int currentLevel = GameManager.Instance.GetCurrentLevelForUnit(stats);
        unitNameText.text = stats.unitName;
        levelText.text = $"{currentLevel}";

        int maxHealth = (stats.maxHealthByLevel.Count >= currentLevel && currentLevel > 0) ? stats.maxHealthByLevel[currentLevel - 1] : 0;
        int attackDamage = (stats.attackDamageByLevel.Count >= currentLevel && currentLevel > 0) ? stats.attackDamageByLevel[currentLevel - 1] : 0;

        healthText.text = maxHealth.ToString();
        attackDamageText.text = attackDamage.ToString();
        attackSpeedText.text = stats.attackSpeed.ToString("F2");
        moveSpeedText.text = stats.moveSpeed.ToString("F1");
        if (detailsPanelGradient != null)
        {
            switch (stats.category)
            {
                case UnitStats.UnitCategory.Fabulosa:
                    detailsPanelGradient.m_color1 = fabulosaColorTop;
                    detailsPanelGradient.m_color2 = fabulosaColorBottom;
                    break;
                case UnitStats.UnitCategory.Magnifica:
                    detailsPanelGradient.m_color1 = magnificaColorTop;
                    detailsPanelGradient.m_color2 = magnificaColorBottom;
                    break;
                case UnitStats.UnitCategory.Suprema:
                    detailsPanelGradient.m_color1 = supremaColorTop;
                    detailsPanelGradient.m_color2 = supremaColorBottom;
                    break;
            }
            detailsPanelGradient.Refresh();
        }
        if (harmonyIconImages != null)
        {
            foreach (var iconImage in harmonyIconImages)
            {
                iconImage.gameObject.SetActive(false);
            }

            for (int i = 0; i < stats.naturalHarmonies.Count; i++)
            {
                if (i < harmonyIconImages.Count)
                {
                    harmonyIconImages[i].gameObject.SetActive(true);
                    harmonyIconImages[i].sprite = stats.naturalHarmonies[i].activeIcon;
                }
            }
        }

        panelFadeCoroutine = StartCoroutine(FadeDetailsPanel(true));
    }

    public void HideDetailsPanel()
    {
        selectedUnitForDetails = null;

        if (detailsPanelCanvasGroup == null || detailsPanelCanvasGroup.alpha == 0) return;
        if (PlayerController.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Placement)
        {
            PlayerController.Instance.HideSelectionHighlight();
            PlayerController.Instance.ClearInteractionState();
        }
        if (activeSelectionHighlight != null)
        {
            Destroy(activeSelectionHighlight);
            activeSelectionHighlight = null;
        }

        if (PlayerController.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Placement)
        {
            PlayerController.Instance.HideSelectionHighlight();
            PlayerController.Instance.ClearInteractionState();
        }
        panelFadeCoroutine = StartCoroutine(FadeDetailsPanel(false));
    }

    private IEnumerator FadeDetailsPanel(bool fadeIn)
    {
        float startAlpha = detailsPanelCanvasGroup.alpha;
        float endAlpha = fadeIn ? 1f : 0f;
        float elapsedTime = 0f;

        while (elapsedTime < panelFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            detailsPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / panelFadeDuration);
            yield return null;
        }

        detailsPanelCanvasGroup.alpha = endAlpha;

        if (fadeIn)
        {
            detailsPanelCanvasGroup.interactable = true;
            detailsPanelCanvasGroup.blocksRaycasts = true;
        }
        else
        {
            detailsPanelCanvasGroup.interactable = false;
            detailsPanelCanvasGroup.blocksRaycasts = false;

            if (activeSelectionHighlight != null)
            {
                Destroy(activeSelectionHighlight);
                activeSelectionHighlight = null;
            }

            if (PlayerController.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Placement)
            {
                PlayerController.Instance.HideSelectionHighlight();
                PlayerController.Instance.ClearInteractionState();
            }
        }

        panelFadeCoroutine = null;
    }

    public void ForceDetailsPanelUpdate(UnitController unit)
    {
        if (unit != null && selectedUnitForDetails == unit && IsDetailsPanelActive)
        {
            if (unit.CurrentHealth <= 0)
            {
                healthText.text = $"0 / {unit.MaxHealth}";
            }
            else if (unit.CurrentHealth < unit.MaxHealth)
            {
                healthText.text = $"{Mathf.CeilToInt(unit.CurrentHealth)} / {unit.MaxHealth}";
            }
            else
            {
                healthText.text = unit.MaxHealth.ToString();
            }

            attackDamageText.text = unit.CurrentAttackDamage.ToString();
            attackSpeedText.text = unit.CurrentAttackSpeed.ToString("F0");
            moveSpeedText.text = unit.CurrentMoveSpeed.ToString("F0");
        }
    }

    void PopulateBench(Transform content, GameObject[] iconPrefabs)
    {
        if (content == null) return;
        foreach (Transform child in content) Destroy(child.gameObject);
        foreach (GameObject iconPrefab in iconPrefabs)
        {
            if (iconPrefab != null) Instantiate(iconPrefab, content);
        }
    }

    private void OnEnable()
    {
        GameManager.OnHarmoniesUpdated += UpdateUnitCountDisplay;
    }

    private void OnDisable()
    {
        GameManager.OnHarmoniesUpdated -= UpdateUnitCountDisplay;
    }

    void UpdateUnitCountDisplay()
    {
        if (GameManager.Instance == null) return;
        int maxUnits = GameManager.Instance.maxUnitsPerTeam;
        if (allyUnitCountText != null)
        {
            int allyCount = GameManager.Instance.GetUnitCountForTeam(0);
            allyUnitCountText.text = $"{allyCount}/{maxUnits}";
        }

        if (enemyUnitCountText != null)
        {
            int enemyCount = GameManager.Instance.GetUnitCountForTeam(1);
            enemyUnitCountText.text = $"{enemyCount}/{maxUnits}";
        }
    }

    public void ShowAllyBench()
    {
        CurrentPlacementTeamID = 0;
        lastActiveBench = ActiveBench.Allies;
        SetBenchVisibility(allyBenchCanvasGroup, enemyBenchCanvasGroup, artifactsBenchCanvasGroup);

        if (allyBenchButton != null) allyBenchButton.GetComponent<Image>().color = activeTabColor;
        if (enemyBenchButton != null) enemyBenchButton.GetComponent<Image>().color = inactiveTabColor;
        if (ArtifactsBenchButton != null) ArtifactsBenchButton.GetComponent<Image>().color = inactiveTabColor;
    }

    public void ShowEnemyBench()
    {
        CurrentPlacementTeamID = 1;
        lastActiveBench = ActiveBench.Enemies;
        SetBenchVisibility(enemyBenchCanvasGroup, allyBenchCanvasGroup, artifactsBenchCanvasGroup);

        if (allyBenchButton != null) allyBenchButton.GetComponent<Image>().color = inactiveTabColor;
        if (enemyBenchButton != null) enemyBenchButton.GetComponent<Image>().color = activeTabColor;
        if (ArtifactsBenchButton != null) ArtifactsBenchButton.GetComponent<Image>().color = inactiveTabColor;
    }

    public void ShowArtifactsBench()
    {
        lastActiveBench = ActiveBench.Artifacts;
        SetBenchVisibility(artifactsBenchCanvasGroup, allyBenchCanvasGroup, enemyBenchCanvasGroup);
        if (allyBenchButton != null) allyBenchButton.GetComponent<Image>().color = inactiveTabColor;
        if (enemyBenchButton != null) enemyBenchButton.GetComponent<Image>().color = inactiveTabColor;
        if (ArtifactsBenchButton != null) ArtifactsBenchButton.GetComponent<Image>().color = activeTabColor;
    }
    private void SetBenchVisibility(CanvasGroup toShow, params CanvasGroup[] toHide)
    {
        if (benchFadeCoroutine != null)
        {
            StopCoroutine(benchFadeCoroutine);
        }

        var allBenches = new List<CanvasGroup> { allyBenchCanvasGroup, enemyBenchCanvasGroup, artifactsBenchCanvasGroup };
        foreach (var bench in allBenches)
        {
            if (bench == null) continue;

            bool isActive = (bench == toShow);
            bench.alpha = isActive ? 1f : 0f;
            bench.interactable = isActive;
            bench.blocksRaycasts = isActive;
        }
    }
    public void HideBenchesForDrag()
    {
        if (benchFadeCoroutine != null)
        {
            StopCoroutine(benchFadeCoroutine);
        }
        var allBenches = new List<CanvasGroup> { allyBenchCanvasGroup, enemyBenchCanvasGroup, artifactsBenchCanvasGroup };
        benchFadeCoroutine = StartCoroutine(FadeMultipleBenches(allBenches, false, fadeDuration));
    }

    private IEnumerator FadeMultipleBenches(List<CanvasGroup> groups, bool fadeIn, float duration)
    {
        Dictionary<CanvasGroup, float> startAlphas = new Dictionary<CanvasGroup, float>();
        foreach (var group in groups)
        {
            if (group != null) startAlphas[group] = group.alpha;
        }

        float endAlpha = fadeIn ? 1f : 0f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            foreach (var group in groups)
            {
                if (group != null)
                {
                    group.alpha = Mathf.Lerp(startAlphas[group], endAlpha, t);
                }
            }
            yield return null;
        }

        foreach (var group in groups)
        {
            if (group != null)
            {
                group.alpha = endAlpha;
                group.interactable = fadeIn;
                group.blocksRaycasts = fadeIn;
            }
        }

        benchFadeCoroutine = null;
    }

    public void ShowBenchesAfterDrag()
    {
        switch (lastActiveBench)
        {
            case ActiveBench.Allies:
                ShowAllyBench();
                break;
            case ActiveBench.Enemies:
                ShowEnemyBench();
                break;
            case ActiveBench.Artifacts:
                ShowArtifactsBench();
                break;
            default:
                ShowAllyBench();
                break;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end)
    {
        float counter = 0f;
        if (end == 0)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        while (counter < fadeDuration)
        {
            counter += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, counter / fadeDuration);
            yield return null;
        }

        cg.alpha = end;
        if (end == 1)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }
    public void SelectUnitForDetails(UnitController unit)
    {
        if (activeSelectionHighlight != null)
        {
            Destroy(activeSelectionHighlight);
        }

        ShowDetailsPanel(unit.unitStats);
        this.selectedUnitForDetails = unit;
        if (selectionHighlightPrefab != null)
        {
            activeSelectionHighlight = Instantiate(selectionHighlightPrefab, unit.transform.position, Quaternion.identity);
            HighlightFollower follower = activeSelectionHighlight.GetComponent<HighlightFollower>();
            if (follower != null)
            {
                follower.targetToFollow = unit.transform;
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