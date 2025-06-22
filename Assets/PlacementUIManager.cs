using UnityEngine;
using UnityEngine.UI;

public class PlacementUIManager : MonoBehaviour
{
    // --- Singleton ---
    public static PlacementUIManager Instance { get; private set; }

    [Header("Configuración de Pestañas")]
    public Button allyBenchButton;
    public Button enemyBenchButton;
    public Color activeTabColor = Color.black;
    public Color inactiveTabColor = Color.white;

    [Header("Banca Aliada")]
    public GameObject allyBenchScrollView;
    public Transform allyBenchContent;
    [Tooltip("Arrastra aquí tus PREFABS de iconos de personajes aliados ya configurados.")]
    public GameObject[] allyIconPrefabs; // CORRECCIÓN: Ahora es una lista de prefabs de iconos.

    [Header("Banca Enemiga")]
    public GameObject enemyBenchScrollView;
    public Transform enemyBenchContent;
    [Tooltip("Arrastra aquí tus PREFABS de iconos de personajes enemigos ya configurados.")]
    public GameObject[] enemyIconPrefabs; // CORRECCIÓN: Ahora es una lista de prefabs de iconos.
    
    public int CurrentPlacementTeamID { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        // Llenamos las bancas usando los prefabs de iconos que asignemos en el inspector
        PopulateBench(allyBenchContent, allyIconPrefabs);
        PopulateBench(enemyBenchContent, enemyIconPrefabs);

        ShowAllyBench();
    }

    /// <summary>
    /// Instancia los prefabs de iconos pre-configurados en la banca correspondiente.
    /// </summary>
    void PopulateBench(Transform content, GameObject[] iconPrefabs)
    {
        if (content == null) return;
        
        foreach (Transform child in content) Destroy(child.gameObject);

        // Creamos un icono por cada prefab en la lista
        foreach (GameObject iconPrefab in iconPrefabs)
        {
            if (iconPrefab != null)
            {
                Instantiate(iconPrefab, content);
            }
        }
    }
    
    public void ShowAllyBench()
    {
        CurrentPlacementTeamID = 0;
        allyBenchScrollView.SetActive(true);
        enemyBenchScrollView.SetActive(false);
        if (allyBenchButton != null) allyBenchButton.GetComponent<Image>().color = activeTabColor;
        if (enemyBenchButton != null) enemyBenchButton.GetComponent<Image>().color = inactiveTabColor;
    }

    public void ShowEnemyBench()
    {
        CurrentPlacementTeamID = 1;
        allyBenchScrollView.SetActive(false);
        enemyBenchScrollView.SetActive(true);
        if (allyBenchButton != null) allyBenchButton.GetComponent<Image>().color = inactiveTabColor;
        if (enemyBenchButton != null) enemyBenchButton.GetComponent<Image>().color = activeTabColor;
    }
}