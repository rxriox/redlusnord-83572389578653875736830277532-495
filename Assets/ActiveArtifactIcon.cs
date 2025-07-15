using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActiveArtifactIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Referencias Internas")]
    public Image artifactImage;
    public GameObject unitIconOverlay;
    public Image unitIconImage;

    [HideInInspector]
    public ArtifactIconController originatingBenchIcon;

    private Transform originalParent;
    private UnitController equippedUnit;

    private Image iconImage;
    private CanvasGroup canvasGroup;
    private Vector3 startPosition;

    // Nueva variable para restaurar la posición relativa en la jerarquía
    private int originalSiblingIndex;

    public bool IsEquipped => equippedUnit != null;

    void Awake()
    {
        iconImage = GetComponent<Image>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Initialize(ArtifactIconController benchIcon)
    {
        this.originatingBenchIcon = benchIcon;

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

        // Guardamos el índice del icono en su contenedor original
        originalSiblingIndex = transform.GetSiblingIndex();

        transform.SetParent(transform.root);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;

        PlayerController.Instance?.ShowTrashZone();
        PlacementUIManager.Instance?.HideBenchesForDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        PlayerController.Instance?.HideTrashZone();
        PlacementUIManager.Instance?.ShowBenchesAfterDrag();

        canvasGroup.blocksRaycasts = true;

        // Soltar en la papelera
        if (eventData.pointerEnter != null &&
            eventData.pointerEnter.GetComponent<TrashZoneController>() != null)
        {
            ArtifactManager.Instance.RemoveArtifact(this);
            return;
        }

        // Soltar sobre unidad
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            UnitController targetUnit = hit.collider.GetComponent<UnitController>();
            if (targetUnit != null)
            {
                HandleEquipOnUnit(targetUnit);

                transform.SetParent(originalParent);
                transform.SetSiblingIndex(originalSiblingIndex);
                transform.localPosition = Vector3.zero;
                return;
            }
        }

        // Restaurar si no se suelta sobre unidad ni papelera
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        transform.position = startPosition;
    }

    private void HandleEquipOnUnit(UnitController targetUnit)
    {
        if (equippedUnit != null && equippedUnit != targetUnit)
        {
            equippedUnit.UnequipArtifact();
        }

        if (targetUnit.EquippedArtifact != null &&
            targetUnit.EquippedArtifact != this.originatingBenchIcon.artifactData)
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
