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
    [Tooltip("El texto que mostrará el contador de unidades aliadas (ej. 5/10).")]
    public TextMeshProUGUI allyUnitCountText;
    [Tooltip("El texto que mostrará el contador de unidades enemigas.")]
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
    [Tooltip("El objeto GameObject del ScrollView de los artefactos.")]
    public GameObject artifactsBenchScrollView;
    [Tooltip("El objeto 'Content' dentro del ScrollView de artefactos.")]
    public Transform artifactsBenchContent;
    [Tooltip("Arrastra aquí tus PREFABS de iconos de artefactos.")]
    public GameObject[] artifactIconPrefabs;
    [Tooltip("El CanvasGroup del ScrollView de los artefactos para la animación.")]
    public CanvasGroup artifactsBenchCanvasGroup;

    [Header("Panel de Detalles de Unidad")]
    public GameObject detailsPanel;
    public TextMeshProUGUI unitNameText;
    public Button closeDetailsButton;
    public GameObject panelBlocker;

    [Header("Animación")]
    [Tooltip("La duración en segundos del desvanecimiento (fade).")]
    public float fadeDuration = 0.2f;
    public enum ActiveBench { Allies, Enemies, Artifacts }

    [Header("Highlight de Selección en Combate")]
    [Tooltip("Arrastra aquí el prefab 'SelectionHighlightFollower_Prefab'")]
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

        if (panelBlocker != null && panelBlocker.GetComponent<Button>() != null)
        {
            panelBlocker.GetComponent<Button>().onClick.AddListener(HideDetailsPanel);
        }
        HideDetailsPanel();
    }

    public void ShowDetailsPanel(UnitStats stats)
    {
        if (stats == null || detailsPanel == null || unitNameText == null) return;

        unitNameText.text = stats.unitName;
        detailsPanel.SetActive(true);
        if (panelBlocker != null) panelBlocker.SetActive(true);
    }

    public void HideDetailsPanel()
    {
        if (detailsPanel != null) detailsPanel.SetActive(false);
        if (panelBlocker != null) panelBlocker.SetActive(false);
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