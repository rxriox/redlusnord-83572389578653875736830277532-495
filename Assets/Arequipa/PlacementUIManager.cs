using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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

    [Header("Animación")]
    public float fadeDuration = 0.1f;
    
    private Coroutine benchFadeCoroutine;
    private enum ActiveBench { Allies, Enemies, Artifacts }
    private ActiveBench lastActiveBench;
    public int CurrentPlacementTeamID { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        PopulateBench(allyBenchContent, allyIconPrefabs);
        PopulateBench(enemyBenchContent, enemyIconPrefabs);
        PopulateBench(artifactsBenchContent, artifactIconPrefabs);
        ShowAllyBench();
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
            case ActiveBench.Allies: ShowAllyBench(); break;
            case ActiveBench.Enemies: ShowEnemyBench(); break;
            case ActiveBench.Artifacts: ShowArtifactsBench(); break;
            default: ShowAllyBench(); break;
        }
    }
}