using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // Necesario para List<>

public class TabGroupManager : MonoBehaviour
{
    // Esta clase nos ayudará a organizar cada pestaña en el Inspector.
    [System.Serializable]
    public class Tab
    {
        [Tooltip("El botón que activa esta pestaña.")]
        public Button tabButton;
        [Tooltip("El panel que se mostrará al hacer clic en el botón.")]
        public GameObject panelToShow;
        
        // --- LÍNEAS AÑADIDAS ---
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
        // Añadimos un 'listener' a cada botón para que llame a nuestra función
        // cuando se le haga clic. Esto lo hacemos por código para no tener que
        // configurar cada botón a mano en el Inspector.
        foreach (Tab tab in tabs)
        {
            // Le decimos al botón que cuando se le haga clic, llame a la función OnTabSelected
            // pasándose a sí mismo como referencia.
            tab.tabButton.onClick.AddListener(() => OnTabSelected(tab));
        }

        // Mostramos la pestaña por defecto al iniciar el juego.
        if (tabs.Count > defaultTabIndex)
        {
            OnTabSelected(tabs[defaultTabIndex]);
        }
    }

    /// <summary>
    /// Esta función se ejecuta cuando se hace clic en cualquier botón de las pestañas.
    /// </summary>
    /// <param name="tab">La pestaña que ha sido seleccionada.</param>
    void OnTabSelected(Tab tab)
    {
        selectedTab = tab;
        ResetTabStates();
    }

    void ResetTabStates()
    {
        // Recorremos todas las pestañas para actualizar su estado.
        foreach (Tab tab in tabs)
        {
            bool isActive = (tab == selectedTab);

            if (tab.panelToShow != null)
            {
                tab.panelToShow.SetActive(isActive);
            }

            // CORRECCIÓN: Ahora usamos los sprites específicos de cada 'tab'.
            Image buttonImage = tab.tabButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                // Si la pestaña está activa, usa su 'activeStateSprite', si no, su 'inactiveStateSprite'.
                buttonImage.sprite = isActive ? tab.activeStateSprite : tab.inactiveStateSprite;
            }
        }
    }
}