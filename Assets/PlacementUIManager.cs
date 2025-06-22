using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Necesario para Coroutines

public class PlacementUIManager : MonoBehaviour
{
    public static PlacementUIManager Instance { get; private set; }

    [Header("Configuración de Pestañas")]
    public Button allyBenchButton;
    public Button enemyBenchButton;
    public Color activeTabColor = Color.black;
    public Color inactiveTabColor = Color.white;

    [Header("Banca Aliada")]
    public GameObject allyBenchScrollView;
    public Transform allyBenchContent;
    public GameObject[] allyIconPrefabs;
    public CanvasGroup allyBenchCanvasGroup; // <-- NUEVO CAMPO

    [Header("Banca Enemiga")]
    public GameObject enemyBenchScrollView;
    public Transform enemyBenchContent;
    public GameObject[] enemyIconPrefabs;
    public CanvasGroup enemyBenchCanvasGroup; // <-- NUEVO CAMPO

    [Header("Configuración de Iconos")]
    public GameObject unitIconPrefab;
    
    [Header("Animación")]
    [Tooltip("La duración en segundos del desvanecimiento (fade).")]
    public float fadeDuration = 0.2f;

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

    // --- MÉTODOS DE CONTROL DE PESTAÑAS (Ahora usan CanvasGroup) ---

    public void ShowAllyBench()
    {
        CurrentPlacementTeamID = 0;
        SetBenchVisibility(allyBenchCanvasGroup, enemyBenchCanvasGroup);
        if (allyBenchButton != null) allyBenchButton.GetComponent<Image>().color = activeTabColor;
        if (enemyBenchButton != null) enemyBenchButton.GetComponent<Image>().color = inactiveTabColor;
    }

    public void ShowEnemyBench()
    {
        CurrentPlacementTeamID = 1;
        SetBenchVisibility(enemyBenchCanvasGroup, allyBenchCanvasGroup);
        if (allyBenchButton != null) allyBenchButton.GetComponent<Image>().color = inactiveTabColor;
        if (enemyBenchButton != null) enemyBenchButton.GetComponent<Image>().color = activeTabColor;
    }
    
    // Método auxiliar para no repetir código
    private void SetBenchVisibility(CanvasGroup toShow, CanvasGroup toHide)
    {
        if (toShow != null)
        {
            toShow.alpha = 1;
            toShow.interactable = true;
            toShow.blocksRaycasts = true;
        }
        if (toHide != null)
        {
            toHide.alpha = 0;
            toHide.interactable = false;
            toHide.blocksRaycasts = false;
        }
    }

    // --- NUEVOS MÉTODOS PARA ANIMACIÓN ---

    public void HideBenchesForDrag()
    {
        if (allyBenchCanvasGroup != null) StartCoroutine(FadeCanvasGroup(allyBenchCanvasGroup, allyBenchCanvasGroup.alpha, 0f));
        if (enemyBenchCanvasGroup != null) StartCoroutine(FadeCanvasGroup(enemyBenchCanvasGroup, enemyBenchCanvasGroup.alpha, 0f));
    }

    public void ShowBenchesAfterDrag()
    {
        // Vuelve a mostrar la pestaña que estaba activa antes de arrastrar.
        if (CurrentPlacementTeamID == 0)
        {
            SetBenchVisibility(allyBenchCanvasGroup, enemyBenchCanvasGroup);
        }
        else
        {
            SetBenchVisibility(enemyBenchCanvasGroup, allyBenchCanvasGroup);
        }
    }
    
    // Corutina que hace la magia de la animación de fade
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end)
    {
        float counter = 0f;
        
        // Desactivamos la interacción al empezar a ocultar
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

        // Activamos la interacción al terminar de mostrar
        if (end == 1)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }
}