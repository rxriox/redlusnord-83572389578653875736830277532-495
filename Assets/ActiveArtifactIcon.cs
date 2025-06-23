using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Este script va en el prefab del icono que aparece en el panel de "artefactos activos".
public class ActiveArtifactIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Guardamos una referencia al icono original de la banca para poder reiniciarlo.
    [HideInInspector] public ArtifactIconController originatingBenchIcon;
    
    private Image iconImage;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 startPosition;

    void Awake()
    {
        iconImage = GetComponent<Image>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Configura este icono activo con la información del icono de la banca.
    /// </summary>
    public void Initialize(ArtifactIconController benchIcon)
    {
        this.originatingBenchIcon = benchIcon;
        if (iconImage != null && benchIcon.artifactData != null)
        {
            iconImage.sprite = benchIcon.artifactData.icon;
        }
    }

    // Lógica de arrastre
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Le decimos al PlayerController que muestre la papelera.
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.ShowTrashZone();
        }

        if (PlacementUIManager.Instance != null)
        {
            PlacementUIManager.Instance.HideBenchesForDrag();
        }
        canvasGroup.blocksRaycasts = false;
        originalParent = transform.parent;
        startPosition = transform.position;
        transform.SetParent(transform.root);
    }


    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Le decimos al PlayerController que oculte la papelera.
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.HideTrashZone();
        }
        
        if (PlacementUIManager.Instance != null)
        {
            PlacementUIManager.Instance.ShowBenchesAfterDrag();
        }
        
        canvasGroup.blocksRaycasts = true;
        transform.SetParent(originalParent);
        transform.position = startPosition;
    }
}