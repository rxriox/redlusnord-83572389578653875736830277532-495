using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Localization;
using TMPro;

public class PlacementUIManager : MonoBehaviour
{
    public static PlacementUIManager Instance { get; private set; }

    [Header("Configuración de Pestañas")]
    public Button allyBenchButton;
    public Button enemyBenchButton;
    public Button ArtifactsBenchButton;
    public Color activeTabColor = Color.black;
    public Color inactiveTabColor = Color.white;

    [Header("Bancas de Unidades")]
    [Tooltip("La lista única de prefabs de unidades que se usará para ambos equipos.")]
    public GameObject[] unitIconPrefabs;
    
    [Header("Banca Aliada")]
    public GameObject allyBenchScrollView;
    public Transform allyBenchContent;
    public CanvasGroup allyBenchCanvasGroup;

    [Header("Banca Enemiga")]
    public GameObject enemyBenchScrollView;
    public Transform enemyBenchContent;
    public CanvasGroup enemyBenchCanvasGroup;

    [Header("Banca de Artefactos")]
    public GameObject artifactsBenchScrollView;
    public Transform artifactsBenchContent;
    public GameObject[] artifactIconPrefabs;
    public CanvasGroup artifactsBenchCanvasGroup;

    [Header("Animación")]
    public float fadeDuration = 0.1f;

    private Coroutine benchFadeCoroutine;
    private enum ActiveBench { Allies, Enemies, Artifacts }
    private ActiveBench lastActiveBench;
    public int CurrentPlacementTeamID { get; private set; }

    [Header("Buscador")]
    public TMP_InputField searchInputField;
    public Button searchClearButton;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        StartCoroutine(PopulateBench(allyBenchContent, unitIconPrefabs));
        StartCoroutine(PopulateBench(enemyBenchContent, unitIconPrefabs));
        StartCoroutine(PopulateBench(artifactsBenchContent, artifactIconPrefabs));
        ShowAllyBench();

        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(OnSearchTextChanged);
        }
        if (searchClearButton != null)
        {
            // El botón debe estar oculto al inicio.
            searchClearButton.gameObject.SetActive(false);
            searchClearButton.onClick.AddListener(ClearSearchField);
        }
    }


    IEnumerator PopulateBench(Transform content, GameObject[] iconPrefabs)
    {
        if (content == null) yield break;
        foreach (Transform child in content) Destroy(child.gameObject);

        var itemsToLoad = new List<(GameObject prefab, LocalizedString localizedString, AsyncOperationHandle<string> handle)>();
        foreach (var prefab in iconPrefabs)
        {
            if (prefab == null) continue;
            
            LocalizedString locString = null;
            var unitIcon = prefab.GetComponent<UnitIconController>();
            if (unitIcon != null && unitIcon.characterData != null)
            {
                locString = unitIcon.characterData.unitName;
            }
            else
            {
                var artifactIcon = prefab.GetComponent<ArtifactIconController>();
                if (artifactIcon != null && artifactIcon.artifactData != null)
                {
                    locString = artifactIcon.artifactData.artifactName;
                }
            }
            
            if (locString != null && !locString.IsEmpty)
            {
                var handle = locString.GetLocalizedStringAsync();
                itemsToLoad.Add((prefab, locString, handle));
            }
        }

        yield return new WaitUntil(() => itemsToLoad.All(item => item.handle.IsDone));
        var itemsToSort = new List<(GameObject prefab, string translatedName, int category)>();
        foreach (var item in itemsToLoad)
        {
            if (item.handle.Status == AsyncOperationStatus.Succeeded)
            {
                int categoryValue = 0; // Por defecto para las unidades
                var artifactIcon = item.prefab.GetComponent<ArtifactIconController>();
                if (artifactIcon != null && artifactIcon.artifactData != null)
                {
                    categoryValue = (int)artifactIcon.artifactData.category;
                }
                itemsToSort.Add((item.prefab, item.handle.Result, categoryValue));
            }
        }
        
        var sortedItems = itemsToSort
            .OrderBy(item => item.category)
            .ThenBy(item => item.translatedName, System.StringComparer.CurrentCulture)
            .ToList();

        foreach (var item in sortedItems)
        {
            Instantiate(item.prefab, content);
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

        ClearAndFilter();
    }

    public void ShowEnemyBench()
    {
        CurrentPlacementTeamID = 1;
        lastActiveBench = ActiveBench.Enemies;
        SetBenchVisibility(enemyBenchCanvasGroup, allyBenchCanvasGroup, artifactsBenchCanvasGroup);

        if (allyBenchButton != null) allyBenchButton.GetComponent<Image>().color = inactiveTabColor;
        if (enemyBenchButton != null) enemyBenchButton.GetComponent<Image>().color = activeTabColor;
        if (ArtifactsBenchButton != null) ArtifactsBenchButton.GetComponent<Image>().color = inactiveTabColor;

        ClearAndFilter();
    }

    public void ShowArtifactsBench()
    {
        lastActiveBench = ActiveBench.Artifacts;
        SetBenchVisibility(artifactsBenchCanvasGroup, allyBenchCanvasGroup, enemyBenchCanvasGroup);
        if (allyBenchButton != null) allyBenchButton.GetComponent<Image>().color = inactiveTabColor;
        if (enemyBenchButton != null) enemyBenchButton.GetComponent<Image>().color = inactiveTabColor;
        if (ArtifactsBenchButton != null) ArtifactsBenchButton.GetComponent<Image>().color = activeTabColor;

        ClearAndFilter();
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
            case ActiveBench.Allies: ShowAllyBench(); break;
            case ActiveBench.Enemies: ShowEnemyBench(); break;
            case ActiveBench.Artifacts: ShowArtifactsBench(); break;
            default: ShowAllyBench(); break;
        }
    }
    
    private void ClearAndFilter()
    {
        if (searchInputField != null)
        {
            searchInputField.text = ""; // Limpia el texto del buscador
        }
        FilterActiveBench(""); // Muestra todos los íconos
    }

    private void OnSearchTextChanged(string text)
    {
        FilterActiveBench(text);
        
        if (searchClearButton != null)
        {
            searchClearButton.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
    }

    private void ClearSearchField()
    {
        if (searchInputField != null)
        {
            searchInputField.text = "";
        }
    }

    private void FilterActiveBench(string searchText)
    {
        Transform activeContent = null;
        switch (lastActiveBench)
        {
            case ActiveBench.Allies: activeContent = allyBenchContent; break;
            case ActiveBench.Enemies: activeContent = enemyBenchContent; break;
            case ActiveBench.Artifacts: activeContent = artifactsBenchContent; break;
        }

        if (activeContent == null) return;

        string lowerSearchText = searchText.ToLower();

        foreach (Transform child in activeContent)
        {
            bool found = false;
            UnitIconController unitIcon = child.GetComponent<UnitIconController>();
            if (unitIcon != null)
            {
                found = unitIcon.cachedLocalizedName.ToLower().Contains(lowerSearchText);
            }
            else
            {
                ArtifactIconController artifactIcon = child.GetComponent<ArtifactIconController>();
                if (artifactIcon != null)
                {
                    found = artifactIcon.cachedLocalizedName.ToLower().Contains(lowerSearchText);
                }
            }
            child.gameObject.SetActive(found);
        }
    }
}