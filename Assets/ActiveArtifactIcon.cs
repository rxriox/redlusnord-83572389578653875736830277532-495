using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActiveArtifactIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Referencias Internas")]
    public Image artifactImage;
    public GameObject unitIconOverlay;
    
    [Tooltip("La imagen donde se mostrará el icono de la unidad equipada.")]
    public Image unitIconImage;

    [HideInInspector]
    public ArtifactIconController originatingBenchIcon;

    private Transform originalParent;
    private UnitController equippedUnit;
    
    private Image iconImage; // restaurado
    private CanvasGroup canvasGroup; // restaurado
    private Vector3 startPosition;

    void Awake()
    {
        iconImage = GetComponent<Image>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Initialize(ArtifactIconController benchIcon)
    {
        this.originatingBenchIcon = benchIcon;

        // Usamos tanto artifactImage como iconImage para retrocompatibilidad visual
        if (artifactImage != null && benchIcon.artifactData != null)
        {
            artifactImage.sprite = benchIcon.artifactData.icon;
        }
        if (iconImage != null && benchIcon.artifactData != null)
        {
            iconImage.sprite = benchIcon.artifactData.icon;
        }

        if (unitIconOverlay != null)
        {
            unitIconOverlay.SetActive(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (originatingBenchIcon == null) return;

        originalParent = transform.parent;
        startPosition = transform.position;

        transform.SetParent(transform.root);
        transform.SetAsLastSibling();

        // Bloqueamos raycast
        canvasGroup.blocksRaycasts = false;

        // Restaurado
        PlayerController.Instance?.ShowTrashZone();
        PlacementUIManager.Instance?.HideBenchesForDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Restaurado
        PlayerController.Instance?.HideTrashZone();
        PlacementUIManager.Instance?.ShowBenchesAfterDrag();

        // Restauramos raycasts
        canvasGroup.blocksRaycasts = true;

        // Si se suelta en la papelera
        if (eventData.pointerEnter != null && eventData.pointerEnter.GetComponent<TrashZoneController>() != null)
        {
            ArtifactManager.Instance.RemoveArtifact(this);
            return;
        }

        // Si se suelta sobre una unidad
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            UnitController targetUnit = hit.collider.GetComponent<UnitController>();
            if (targetUnit != null)
            {
                HandleEquipOnUnit(targetUnit);
                transform.SetParent(originalParent);
                transform.localPosition = Vector3.zero;
                return;
            }
        }

        // Si no es válido, vuelve a su lugar
        transform.SetParent(originalParent);
        transform.position = startPosition;
    }

    private void HandleEquipOnUnit(UnitController targetUnit)
    {
        if (equippedUnit != null && equippedUnit != targetUnit)
        {
            equippedUnit.UnequipArtifact();
        }

        if (targetUnit.EquippedArtifact != null && targetUnit.EquippedArtifact != this.originatingBenchIcon.artifactData)
        {
            ArtifactManager.Instance.FindAndClearEquippedIcon(targetUnit.EquippedArtifact);
        }

        targetUnit.EquipArtifact(originatingBenchIcon.artifactData);
        equippedUnit = targetUnit;

        if (unitIconImage != null && targetUnit.unitStats != null)
        {
            unitIconImage.sprite = targetUnit.unitStats.unitIcon;
        }

        if (unitIconOverlay != null)
        {
            unitIconOverlay.SetActive(true);
        }
    }

    public void ClearEquippedStatus()
    {
        if (equippedUnit != null)
        {
            equippedUnit.UnequipArtifact();
        }
        equippedUnit = null;

        if (unitIconOverlay != null)
        {
            unitIconOverlay.SetActive(false);
        }
    }
}