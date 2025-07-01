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
    public Button closeDetailsButton;
    public PanelInGameGradient detailsPanelGradient;
    
    // --- AÑADIDO: Referencia al CanvasGroup y variable de control ---
    [Tooltip("Arrastra aquí el componente CanvasGroup del detailsPanel.")]
    public CanvasGroup detailsPanelCanvasGroup;
    private Coroutine panelFadeCoroutine;
    // La propiedad IsDetailsPanelActive ahora comprueba el alpha
    public bool IsDetailsPanelActive => detailsPanelCanvasGroup != null && detailsPanelCanvasGroup.alpha > 0;

    [Header("Configuración de Degradados de Rareza")]
    public Color fabulosaColorTop = new Color(0.1f, 0.2f, 0.6f);
    public Color fabulosaColorBottom = Color.black;
    public Color magnificaColorTop = new Color(0.4f, 0.1f, 0.6f);
    public Color magnificaColorBottom = Color.black;
    public Color supremaColorTop = new Color(0.7f, 0.6f, 0.1f);
    public Color supremaColorBottom = Color.black;

    [Header("Animación")]
    public float fadeDuration = 0.2f; // Puedes usar esta para los bancos
    public float panelFadeDuration = 0.2f; // Duración específica para el panel
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
            // Si no hay CanvasGroup, usamos la lógica antigua como fallback.
            detailsPanel.SetActive(false);
        }
    }

    public void ShowDetailsPanel(UnitStats stats)
    {
        if (stats == null || detailsPanelCanvasGroup == null) return;
        
        // Detenemos cualquier animación anterior para evitar conflictos
        if (panelFadeCoroutine != null)
        {
            StopCoroutine(panelFadeCoroutine);
        }

        // Preparamos el contenido del panel
        unitNameText.text = stats.unitName;
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

        // Iniciamos la animación de fade-in
        panelFadeCoroutine = StartCoroutine(FadeDetailsPanel(true));
    }

    public void HideDetailsPanel()
    {
        
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
            
            // Lógica de limpieza que se ejecuta después de que el panel se ha desvanecido
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
        StopAllCoroutines();
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
        StopAllCoroutines();
        if (allyBenchCanvasGroup != null) StartCoroutine(FadeCanvasGroup(allyBenchCanvasGroup, allyBenchCanvasGroup.alpha, 0f));
        if (enemyBenchCanvasGroup != null) StartCoroutine(FadeCanvasGroup(enemyBenchCanvasGroup, enemyBenchCanvasGroup.alpha, 0f));
        if (artifactsBenchCanvasGroup != null) StartCoroutine(FadeCanvasGroup(artifactsBenchCanvasGroup, artifactsBenchCanvasGroup.alpha, 0f));
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
}