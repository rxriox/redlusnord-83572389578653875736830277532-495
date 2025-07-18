using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class TabGroupManager : MonoBehaviour
{
    private PlayerControl playerControls;
    private bool artifactDropWasSuccessful = false;
    public static TabGroupManager Instance { get; private set; }
    [HideInInspector]
    public Artifact draggedArtifact { get; private set; }
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

        playerControls = new PlayerControl();
    }

    private void OnEnable()
    {
        playerControls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        playerControls.Gameplay.Disable();
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
    [Tooltip("La imagen de la UI que seguirá al puntero al arrastrar un artefacto.")]
    public Image artifactDragCursor;
    [Tooltip("El índice de la pestaña/panel que se mostrará como zona para soltar artefactos (0=primero, 1=segundo, etc.).")]
    public int artifactDropTargetTabIndex = 1;

    [Tooltip("Arrastra aquí el objeto que tiene el ArtifactManager.")]
    public ArtifactManager artifactManager;
    [Tooltip("El índice en la lista 'Tabs' que corresponde a la pestaña de artefactos (0 es el primero, 1 el segundo, etc.).")]
    public int artifactTabIndex = 1;

    void Start()
    {
        if (artifactManager != null)
        {
            artifactManager.SetArtifactPanelActive(false);
        }

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
        for (int i = 0; i < tabs.Count; i++)
        {
            Tab tab = tabs[i];
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

            if (i == artifactTabIndex && artifactManager != null)
            {
                artifactManager.SetArtifactPanelActive(isActive);
            }
        }
    }

    public void OnArtifactDragStart(ArtifactIconController icon)
    {
        artifactDropWasSuccessful = false;
        lastSelectedTab = selectedTab;
        draggedArtifact = icon.artifactData;

        if (artifactDragCursor != null && icon.artifactData != null)
        {
            artifactDragCursor.gameObject.SetActive(true);
            artifactDragCursor.sprite = icon.artifactData.icon;
        }

        if (tabs.Count > artifactDropTargetTabIndex && artifactDropTargetTabIndex >= 0)
        {
            OnTabSelected(tabs[artifactDropTargetTabIndex]);
        }
        else
        {
            Debug.LogWarning("El 'Artifact Drop Target Tab Index' no es válido.");
        }
    }

    public void OnArtifactDragEnd()
    {
        if (artifactDragCursor != null)
        {
            artifactDragCursor.gameObject.SetActive(false);
        }

        if (!artifactDropWasSuccessful && lastSelectedTab != null)
        {
            OnTabSelected(lastSelectedTab);
        }

        draggedArtifact = null;
        lastSelectedTab = null;
        artifactDropWasSuccessful = false;
    }

    public void SetArtifactDropSuccessful()
    {
        artifactDropWasSuccessful = true;
    }

    private void Update()
    {
        if (draggedArtifact != null && artifactDragCursor != null)
        {
            artifactDragCursor.transform.position = playerControls.Gameplay.PointerPosition.ReadValue<Vector2>();
        }
    }
}
