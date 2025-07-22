using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPerspectiveManager : MonoBehaviour
{
    public static UIPerspectiveManager Instance { get; private set; }
    public int GetCurrentTeamPerspective() => harmonyUIManager.teamIdToShow;

    [Header("Referencias de UI")]
    [Tooltip("El botón que activa la perspectiva del jugador.")]
    public Button playerPerspectiveButton;
    [Tooltip("El botón que activa la perspectiva del enemigo.")]
    public Button enemyPerspectiveButton;

    [Header("Contador Total")]
    [Tooltip("Texto para mostrar el contador total de unidades en la perspectiva actual.")]
    public TextMeshProUGUI totalUnitCountText;

    [Header("Contadores de Rareza")]
    [Tooltip("Texto para mostrar el contador de unidades Fabulosas.")]
    
    public TextMeshProUGUI fabulosaCountText;
    [Tooltip("Texto para mostrar el contador de unidades Magníficas.")]
    public TextMeshProUGUI magnificaCountText;
    [Tooltip("Texto para mostrar el contador de unidades Supremas.")]
    public TextMeshProUGUI supremaCountText;

    [Header("Contadores de Artefactos")]
    public TextMeshProUGUI activeArtifactsCountText;

    [Header("Referencias del Sistema")]
    [Tooltip("Arrastra aquí el objeto que contiene el HarmonyUIManager.")]
    public HarmonyUIManager harmonyUIManager;

    [Tooltip("Arrastra aquí el objeto que contiene el ArtifactManager.")]
    public ArtifactManager artifactManager;

    [Header("Apariencia de Botones")]
    public Color activeButtonColor = Color.white;
    public Color inactiveButtonColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private void OnEnable()
    {
        GameManager.OnUnitCountChanged += HandleUnitCountChange;
    }

    private void OnDisable()
    {
        GameManager.OnUnitCountChanged -= HandleUnitCountChange;
    }

    void Start()
    {
        if (playerPerspectiveButton == null || enemyPerspectiveButton == null || harmonyUIManager == null)
        {
            Debug.LogError("Asegúrate de asignar todos los botones y el HarmonyUIManager en el inspector de UIPerspectiveManager.");
            return;
        }

        playerPerspectiveButton.onClick.AddListener(ShowPlayerPerspective);
        enemyPerspectiveButton.onClick.AddListener(ShowEnemyPerspective);

        ShowPlayerPerspective();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void ShowPlayerPerspective()
    {
        if (harmonyUIManager != null)
        {
            harmonyUIManager.SetTeamPerspective(0);
            if (artifactManager != null) artifactManager.SetPerspective(0);
            UpdateButtonAppearance(true);
            UpdateUnitCountsUI(0);
            UpdateArtifactBenchVisuals(0);
        }
    }

    public void ShowEnemyPerspective()
    {
        if (harmonyUIManager != null)
        {
            harmonyUIManager.SetTeamPerspective(1);
            if (artifactManager != null) artifactManager.SetPerspective(1);
            UpdateButtonAppearance(false);
            UpdateUnitCountsUI(1);
            UpdateArtifactBenchVisuals(1);
        }
    }

    private void UpdateArtifactBenchVisuals(int teamID)
    {
        // Busca todos los íconos de artefactos en el banquillo y actualiza su sprite
        var artifactIcons = FindObjectsByType<ArtifactIconController>(FindObjectsSortMode.None);
        foreach (var icon in artifactIcons)
        {
            icon.UpdateIconVisual(teamID);
        }
    }

    private void UpdateButtonAppearance(bool isPlayerPerspective)
    {
        Image playerBtnImage = playerPerspectiveButton.GetComponent<Image>();
        Image enemyBtnImage = enemyPerspectiveButton.GetComponent<Image>();

        if (playerBtnImage != null)
        {
            playerBtnImage.color = isPlayerPerspective ? activeButtonColor : inactiveButtonColor;
        }

        if (enemyBtnImage != null)
        {
            enemyBtnImage.color = !isPlayerPerspective ? activeButtonColor : inactiveButtonColor;
        }
    }

    private void HandleUnitCountChange()
    {
        if (harmonyUIManager != null)
        {
            UpdateUnitCountsUI(harmonyUIManager.teamIdToShow);
        }
    }

    private void UpdateUnitCountsUI(int teamID)
    {
        if (GameManager.Instance == null) return;

        // Contador total
        if (totalUnitCountText != null)
        {
            int currentTotal = GameManager.Instance.GetUnitCountForTeam(teamID);
            int totalLimit = GameManager.Instance.maxUnitsPerTeam;
            totalUnitCountText.text = $"{currentTotal} / {totalLimit}";
        }

        // Fabulosa
        if (fabulosaCountText != null)
        {
            int currentCount = GameManager.Instance.GetCategoryCountForTeam(UnitStats.UnitCategory.Fabulosa, teamID);
            int limit = GameManager.Instance.GetCategoryLimit(UnitStats.UnitCategory.Fabulosa);
            fabulosaCountText.text = $"{currentCount} / {limit}";
        }

        // Magnifica
        if (magnificaCountText != null)
        {
            int currentCount = GameManager.Instance.GetCategoryCountForTeam(UnitStats.UnitCategory.Magnifica, teamID);
            int limit = GameManager.Instance.GetCategoryLimit(UnitStats.UnitCategory.Magnifica);
            magnificaCountText.text = $"{currentCount} / {limit}";
        }

        // Suprema
        if (supremaCountText != null)
        {
            int currentCount = GameManager.Instance.GetCategoryCountForTeam(UnitStats.UnitCategory.Suprema, teamID);
            int limit = GameManager.Instance.GetCategoryLimit(UnitStats.UnitCategory.Suprema);
            supremaCountText.text = $"{currentCount} / {limit}";
        }

        //Artefactos
        if (activeArtifactsCountText != null && ArtifactManager.Instance != null)
        {
            int currentArtifacts = ArtifactManager.Instance.GetActiveArtifactCountForTeam(teamID);
            int maxArtifactsLimit = GameManager.Instance.maxArtifacts;
            activeArtifactsCountText.text = $"{currentArtifacts} / {maxArtifactsLimit}";
        }
    }
}
