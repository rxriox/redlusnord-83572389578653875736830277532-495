using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TabGroupManager : MonoBehaviour
{
    public static TabGroupManager Instance { get; private set; } // Añadimos un Singleton
    [HideInInspector]
    public Artifact draggedArtifact { get; private set; } // Para saber qué artefacto se arrastra
    private Tab lastSelectedTab; // Para recordar qué pestaña estaba abierta
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    [System.Serializable]
    public class Tab
    {
        [Tooltip("El botón que activa esta pestaña.")]
        public Button tabButton;
        [Tooltip("El panel que se mostrará al hacer clic en el botón.")]
        public GameObject panelToShow;
        [Tooltip("El sprite que tendrá el botón cuando esta pestaña esté ACTIVA.")]
        public Sprite activeStateSprite;
        [Tooltip("El sprite que tendrá el botón cuando esta pestaña esté INACTIVA.")]
        public Sprite inactiveStateSprite;
    }

    [Header("Configuración de Pestañas")]
    [Tooltip("Añade aquí todas las pestañas que quieres que este manager controle.")]
    public List<Tab> tabs;

    [Header("Pestaña por Defecto")]
    [Tooltip("El índice de la pestaña que se mostrará al iniciar (0 es la primera de la lista).")]
    public int defaultTabIndex = 0;

    private Tab selectedTab;

    void Start()
    {
        foreach (Tab tab in tabs)
        {
            tab.tabButton.onClick.AddListener(() => OnTabSelected(tab));
        }

        if (tabs.Count > defaultTabIndex)
        {
            OnTabSelected(tabs[defaultTabIndex]);
        }
    }
    void OnTabSelected(Tab tab)
    {
        selectedTab = tab;
        ResetTabStates();
    }

    void ResetTabStates()
    {
        foreach (Tab tab in tabs)
        {
            bool isActive = (tab == selectedTab);

            if (tab.panelToShow != null)
            {
                tab.panelToShow.SetActive(isActive);
            }
            Image buttonImage = tab.tabButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = isActive ? tab.activeStateSprite : tab.inactiveStateSprite;
            }
        }
    }
    public void OnArtifactDragStart(Artifact artifact)
    {
        // Guardamos la pestaña que estaba activa para poder volver a ella después.
        lastSelectedTab = selectedTab;
        draggedArtifact = artifact;

        // Opcional: Forzamos que se muestre una pestaña específica como zona de drop.
        // Por ejemplo, la primera de la lista (el panel de armonías).
        if (tabs.Count > 0)
        {
            OnTabSelected(tabs[0]);
        }
    }
    public void OnArtifactDragEnd()
    {
        // Restauramos la pestaña que estaba activa antes del arrastre.
        if (lastSelectedTab != null)
        {
            OnTabSelected(lastSelectedTab);
        }

        // Limpiamos el estado.
        draggedArtifact = null;
        lastSelectedTab = null;
    }
}