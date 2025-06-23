using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TabGroupManager : MonoBehaviour
{
    private bool artifactDropWasSuccessful = false;
    public static TabGroupManager Instance { get; private set; }
    [HideInInspector]
    public Artifact draggedArtifact { get; private set; } // Para saber que artefacto se arrastra
    private Tab lastSelectedTab;
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
    [Header("Arrastre de Artefactos")]
    [Tooltip("El índice de la pestaña/panel que se mostrará como zona para soltar artefactos (0=primero, 1=segundo, etc.).")]
    public int artifactDropTargetTabIndex = 1;

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
        // Reiniciamos la bandera al empezar un nuevo arrastre.
        artifactDropWasSuccessful = false;

        lastSelectedTab = selectedTab;
        draggedArtifact = artifact;

        if (tabs.Count > artifactDropTargetTabIndex && artifactDropTargetTabIndex >= 0)
        {
            OnTabSelected(tabs[artifactDropTargetTabIndex]);
        }
        else
        {
            Debug.LogWarning("El 'Artifact Drop Target Tab Index' no es válido. Revisa la configuración en el TabGroupManager.");
        }
    }
    public void OnArtifactDragEnd()
    {
        // Si el drop NO fue exitoso, volvemos a la pestaña que estaba activa antes.
        if (!artifactDropWasSuccessful && lastSelectedTab != null)
        {
            OnTabSelected(lastSelectedTab);
        }
        // Si FUE exitoso, no hacemos nada y la vista se queda en el panel de artefactos.

        // Limpiamos las variables de estado en cualquier caso.
        draggedArtifact = null;
        lastSelectedTab = null;
        artifactDropWasSuccessful = false; // Reiniciamos la bandera para la próxima vez.
    }
    
    public void SetArtifactDropSuccessful()
    {
        artifactDropWasSuccessful = true;
    }
}