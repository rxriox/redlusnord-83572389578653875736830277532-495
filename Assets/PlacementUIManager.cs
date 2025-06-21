using UnityEngine;
using UnityEngine.UI;

public class PlacementUIManager : MonoBehaviour
{
    [Header("ScrollViews de Contenido")]
    [Tooltip("El componente ScrollRect del ScrollView de aliados.")]
    [SerializeField] private ScrollRect allyScrollView;
    [Tooltip("El componente ScrollRect del ScrollView de enemigos.")]
    [SerializeField] private ScrollRect enemyScrollView;

    [Header("Botones de Selección")]
    [Tooltip("El botón para mostrar el panel de aliados.")]
    [SerializeField] private Button allyButton;
    [Tooltip("El botón para mostrar el panel de enemigos.")]
    [SerializeField] private Button enemyButton;

    [Header("Colores de Botón")]
    [Tooltip("Color del botón cuando su panel está activo (ej. negro).")]
    [SerializeField] private Color activeColor = Color.black;
    [Tooltip("Color del botón cuando su panel está inactivo (ej. blanco).")]
    [SerializeField] private Color inactiveColor = Color.white;

    void Start()
    {
        if (allyButton != null) allyButton.onClick.AddListener(ShowAllyPanel);
        if (enemyButton != null) enemyButton.onClick.AddListener(ShowEnemyPanel);
        ShowAllyPanel();
    }
    public void ShowAllyPanel()
    {
        if (allyScrollView != null) allyScrollView.gameObject.SetActive(true);
        if (enemyScrollView != null) enemyScrollView.gameObject.SetActive(false);
        if (allyButton != null) allyButton.GetComponent<Image>().color = activeColor;
        if (enemyButton != null) enemyButton.GetComponent<Image>().color = inactiveColor;
    }
    public void ShowEnemyPanel()
    {
        if (allyScrollView != null) allyScrollView.gameObject.SetActive(false);
        if (enemyScrollView != null) enemyScrollView.gameObject.SetActive(true);
        if (allyButton != null) allyButton.GetComponent<Image>().color = inactiveColor;
        if (enemyButton != null) enemyButton.GetComponent<Image>().color = activeColor;
    }
}
