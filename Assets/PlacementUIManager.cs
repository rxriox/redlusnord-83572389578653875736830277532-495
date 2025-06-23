using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class PlacementUIManager : MonoBehaviour
{
    public static PlacementUIManager Instance { get; private set; }

    [Header("Configuración de Pestañas")]
    public Button allyBenchButton;
    public Button enemyBenchButton;
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
        UpdateUnitCountDisplay();
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
        // Nos suscribimos al evento del GameManager.
        // Cada vez que se registre/desregistre una unidad, se llamará a UpdateUnitCountDisplay.
        GameManager.OnHarmoniesUpdated += UpdateUnitCountDisplay;
    }

    private void OnDisable()
    {
        // Es importante desuscribirse para evitar errores.
        GameManager.OnHarmoniesUpdated -= UpdateUnitCountDisplay;
    }

    void UpdateUnitCountDisplay()
    {
        if (GameManager.Instance == null) return;

        // Obtenemos el límite máximo de unidades desde el GameManager.
        int maxUnits = GameManager.Instance.maxUnitsPerTeam;

        // Actualizamos el texto del contador de aliados (equipo 0).
        if (allyUnitCountText != null)
        {
            int allyCount = GameManager.Instance.GetUnitCountForTeam(0);
            allyUnitCountText.text = $"{allyCount}/{maxUnits}";
        }

        // Actualizamos el texto del contador de enemigos (equipo 1).
        if (enemyUnitCountText != null)
        {
            int enemyCount = GameManager.Instance.GetUnitCountForTeam(1);
            enemyUnitCountText.text = $"{enemyCount}/{maxUnits}";
        }
    }

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

    public void HideBenchesForDrag()
    {
        if (allyBenchCanvasGroup != null) StartCoroutine(FadeCanvasGroup(allyBenchCanvasGroup, allyBenchCanvasGroup.alpha, 0f));
        if (enemyBenchCanvasGroup != null) StartCoroutine(FadeCanvasGroup(enemyBenchCanvasGroup, enemyBenchCanvasGroup.alpha, 0f));
    }

    public void ShowBenchesAfterDrag()
    {
        if (CurrentPlacementTeamID == 0)
        {
            SetBenchVisibility(allyBenchCanvasGroup, enemyBenchCanvasGroup);
        }
        else
        {
            SetBenchVisibility(enemyBenchCanvasGroup, allyBenchCanvasGroup);
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
}