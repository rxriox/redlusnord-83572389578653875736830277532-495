using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic; // Necesario para List<RaycastResult>

public class PlayerController : MonoBehaviour
{
    [Header("Referencias del Sistema")]
    public GridManager gridManager;
    public GameObject highlightPrefab;

    [Header("Referencias de UI")]
    public Image dragCursorImage;
    [Tooltip("El objeto de la UI que funcionará como zona para eliminar unidades.")]
    public GameObject trashZoneUI; // <-- NUEVO CAMPO

    // --- Variables de Estado ---
    private GameObject highlightInstance;
    private UnitIconController currentlyDraggedIcon;
    private UnitController unitToReposition;
    private Node originalNodeOfRepositionedUnit;
    private PlayerControl playerControls;

    void Awake() { playerControls = new PlayerControl(); }
    private void OnEnable() { playerControls.Gameplay.Enable(); }
    private void OnDisable() { playerControls.Gameplay.Disable(); }

    void Start()
    {
        if (highlightPrefab != null) { highlightInstance = Instantiate(highlightPrefab); highlightInstance.SetActive(false); }
        if (dragCursorImage != null) { dragCursorImage.gameObject.SetActive(false); }
        if (trashZoneUI != null) { trashZoneUI.SetActive(false); } // Ocultamos la zona al empezar
    }

    void Update()
    {
        if (currentlyDraggedIcon != null)
        {
            dragCursorImage.transform.position = Mouse.current.position.ReadValue();
            UpdateHighlightForNewUnit();
            return;
        }

        if (unitToReposition != null)
        {
            UpdateRepositioningUnit();
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                DropRepositionedUnit();
            }
            return;
        }

        if (GameManager.Instance.CurrentState == GameManager.GameState.Placement && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryStartRepositioning();
        }
    }

    // --- Lógica de Reposicionar y Eliminar ---

    void TryStartRepositioning()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            UnitController unit = hit.collider.GetComponent<UnitController>();
            if (unit != null && unit.teamID == PlacementUIManager.Instance.CurrentPlacementTeamID)
            {
                unitToReposition = unit;
                originalNodeOfRepositionedUnit = unit.currentNode;
                originalNodeOfRepositionedUnit.isWalkable = true;
                if (trashZoneUI != null) trashZoneUI.SetActive(true); // Mostramos la zona de eliminación
            }
        }
    }

    void UpdateRepositioningUnit()
    {
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPosition = ray.GetPoint(distance);
            unitToReposition.transform.position = new Vector3(worldPosition.x, 0.5f, worldPosition.z);
            UpdateHighlightForRepositioning();
        }
    }

    void DropRepositionedUnit()
    {
        // Comprobamos si el ratón se soltó sobre un objeto de la UI
        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        bool droppedOnTrash = false;
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.GetComponent<TrashZoneController>() != null)
            {
                droppedOnTrash = true;
                break;
            }
        }

        if (droppedOnTrash)
        {
            // --- LÓGICA DE ELIMINACIÓN ---
            if (unitToReposition.originatingIcon != null)
            {
                unitToReposition.originatingIcon.ResetIcon(); // Reinicia el icono en la UI
            }
            unitToReposition.Die(); // Elimina la unidad del tablero
        }
        else
        {
            // --- LÓGICA DE REUBICACIÓN ---
            Node destinationNode = gridManager.NodeFromWorldPoint(unitToReposition.transform.position);
            if (gridManager.IsNodeValidForPlacement(destinationNode, unitToReposition.teamID))
            {
                unitToReposition.transform.position = destinationNode.worldPosition;
                unitToReposition.currentNode = destinationNode;
                destinationNode.isWalkable = false;
            }
            else
            {
                unitToReposition.transform.position = originalNodeOfRepositionedUnit.worldPosition;
                unitToReposition.currentNode = originalNodeOfRepositionedUnit;
                originalNodeOfRepositionedUnit.isWalkable = false;
            }
        }

        // --- LIMPIEZA ---
        if (trashZoneUI != null) trashZoneUI.SetActive(false); // Ocultamos la zona
        unitToReposition = null;
        originalNodeOfRepositionedUnit = null;
        if (highlightInstance != null) highlightInstance.SetActive(false);
    }
    
    // --- Lógica de Colocar (con un pequeño añadido) ---

    private void PlaceUnitOnNode(Node node, UnitStats unitStats)
    {
        if (unitStats?.characterPrefab != null)
        {
            GameObject unitInstance = Instantiate(unitStats.characterPrefab, node.worldPosition, Quaternion.identity);
            node.isWalkable = false;
            UnitController unitController = unitInstance.GetComponent<UnitController>();
            if (unitController != null)
            {
                unitController.unitStats = unitStats;
                unitController.currentNode = node;
                unitController.teamID = PlacementUIManager.Instance.CurrentPlacementTeamID;
                unitController.originatingIcon = currentlyDraggedIcon; // <--- AÑADIDO: Creamos el vínculo
                GameManager.Instance.RegisterUnit(unitController);
            }
        }
    }

    // El resto de funciones (StartDraggingUnit, StopDraggingUnit, UpdateHighlight, etc.) se mantienen igual.
    // Pega el script completo para asegurar que todo está en su sitio.

    public void StartDraggingUnit(UnitIconController iconController)
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Placement) return;
        currentlyDraggedIcon = iconController;
        dragCursorImage.sprite = currentlyDraggedIcon.GetDragCursorSprite();
        dragCursorImage.raycastTarget = false;
        dragCursorImage.gameObject.SetActive(true);

    }
    public void StopDraggingUnit()
    {
        if (currentlyDraggedIcon == null) return;
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Node node = gridManager.NodeFromWorldPoint(hit.point);
                if (gridManager.IsNodeValidForPlacement(node, PlacementUIManager.Instance.CurrentPlacementTeamID))
                {
                    PlaceUnitOnNode(node, currentlyDraggedIcon.GetUnitStats());
                    currentlyDraggedIcon.SetAsPlaced();
                }
            }
        }
        currentlyDraggedIcon = null;
        dragCursorImage.gameObject.SetActive(false);
        if (highlightInstance != null) highlightInstance.SetActive(false);

    }
    private void UpdateHighlightForNewUnit()
    {
        if (highlightInstance == null || PlacementUIManager.Instance == null) return;
        int teamID = PlacementUIManager.Instance.CurrentPlacementTeamID;
        UpdateHighlight(teamID);

    }
    private void UpdateHighlightForRepositioning()
    {
        if (highlightInstance == null || unitToReposition == null) return;
        int teamID = unitToReposition.teamID;
        UpdateHighlight(teamID);

    }
    private void UpdateHighlight(int teamID)
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Node node = gridManager.NodeFromWorldPoint(hit.point);
            if (gridManager.IsNodeValidForPlacement(node, teamID))
            {
                highlightInstance.SetActive(true);
                highlightInstance.transform.position = node.worldPosition;
            }
            else
            {
                highlightInstance.SetActive(false);
            }
        }
        else
        {
            highlightInstance.SetActive(false);
        }
    }
}