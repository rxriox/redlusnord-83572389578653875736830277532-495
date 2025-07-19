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

    [HideInInspector]
    public int teamID; // Guardará si el artefacto es del equipo 0 o 1

    private Transform originalParent;
    private UnitController equippedUnit;

    private UnitController lastHighlightedUnit = null;

    private Image iconImage;
    private CanvasGroup canvasGroup;
    private Vector3 startPosition;

    private int originalSiblingIndex;

    public bool IsEquipped => equippedUnit != null;

    void Awake()
    {
        iconImage = GetComponent<Image>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Initialize(ArtifactIconController benchIcon, int ownerTeamID)
    {
        this.originatingBenchIcon = benchIcon;
        this.teamID = ownerTeamID;

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

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Combat)
        {
            Debug.LogWarning("No se pueden arrastrar artefactos durante el combate.");
            eventData.pointerDrag = null; 
            return; 
        }

        originalParent = transform.parent;
        startPosition = transform.position;

        originalSiblingIndex = transform.GetSiblingIndex();

        transform.SetParent(transform.root);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;

        // NUEVA LÓGICA: Solo muestra la Trash Zone si NO hay combate en curso.
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Combat)
        {
            PlayerController.Instance?.ShowTrashZone();
            PlacementUIManager.Instance?.HideBenchesForDrag();
        }
        else
        {
            PlacementUIManager.Instance?.HideBenchesForDrag();
        }

        lastHighlightedUnit = null;
        PlayerController.Instance?.HideSelectionHighlight();
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;

        UnitController unitUnderCursor = null;

        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            unitUnderCursor = hit.collider.GetComponent<UnitController>();
        }

        if (unitUnderCursor != null && unitUnderCursor != lastHighlightedUnit)
        {
            PlayerController.Instance.ShowSelectionHighlight(unitUnderCursor.currentNode);
            lastHighlightedUnit = unitUnderCursor;
        }
        else if (unitUnderCursor == null && lastHighlightedUnit != null)
        {
            PlayerController.Instance.HideSelectionHighlight();
            lastHighlightedUnit = null;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        PlayerController.Instance?.HideTrashZone();
        PlacementUIManager.Instance?.ShowBenchesAfterDrag();

        canvasGroup.blocksRaycasts = true;

        PlayerController.Instance?.HideSelectionHighlight();

        // INICIO DE LA LÓGICA DE PREVENCIÓN EN COMBATE
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Combat)
        {
            Debug.LogWarning("No se pueden modificar artefactos durante el combate. El artefacto vuelve a su posición original.");
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
            transform.position = startPosition;
            return;
        }
        // FIN DE LA LÓGICA DE PREVENCIÓN EN COMBATE

        // Soltar en TrashZone
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

        // Si no se soltó en una unidad válida ni en TrashZone
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
        transform.position = startPosition;
    }

    private void HandleEquipOnUnit(UnitController targetUnit)
    {
        // VALIDACIÓN: No se puede equipar un artefacto a una unidad de otro equipo.
        if (targetUnit.teamID != this.teamID)
        {
            Debug.LogWarning("Intento de equipar artefacto a un equipo incorrecto.");
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
            transform.position = startPosition;
            return;
        }

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

    public void DetachFromUnit()
    {
        equippedUnit = null;

        if (unitIconOverlay != null)
        {
            unitIconOverlay.SetActive(false);
        }
    }

    public void LinkToUnit(UnitController unit)
    {
        equippedUnit = unit;

        if (unitIconImage != null && unit.unitStats != null)
        {
            unitIconImage.sprite = unit.unitStats.unitIcon;
        }

        if (unitIconOverlay != null)
        {
            unitIconOverlay.SetActive(true);
        }
    }
}
