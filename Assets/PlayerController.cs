using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Referencias del Sistema")]
    public GridManager gridManager;
    public GameObject highlightPrefab;

    [Header("Referencias de UI")]
    public Image dragCursorImage;
    public CanvasGroup trashZoneCanvasGroup;
    public float fadeDuration = 0.2f;
    
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
        if (trashZoneCanvasGroup != null)
        {
            trashZoneCanvasGroup.alpha = 0;
            trashZoneCanvasGroup.interactable = false;
            trashZoneCanvasGroup.blocksRaycasts = false;
        }
    }

    void Update()
    {
        Vector2 pointerPosition = playerControls.Gameplay.PointerPosition.ReadValue<Vector2>();

        if (currentlyDraggedIcon != null)
        {
            dragCursorImage.transform.position = pointerPosition;
            UpdateHighlightForNewUnit(pointerPosition);
            return;
        }

        if (unitToReposition != null)
        {
            UpdateRepositioningUnit(pointerPosition);
            if (playerControls.Gameplay.Click.WasReleasedThisFrame())
            {
                DropRepositionedUnit(pointerPosition);
            }
            return;
        }

        if (GameManager.Instance.CurrentState == GameManager.GameState.Placement && playerControls.Gameplay.Click.WasPressedThisFrame())
        {
            TryStartRepositioning(pointerPosition);
        }
    }
    
    // --- FUNCIÓN CLAVE A REVISAR ---
    public void StartDraggingUnit(UnitIconController iconController)
    {
        // 1. OBTENEMOS LOS DATOS
        int teamID = PlacementUIManager.Instance.CurrentPlacementTeamID;
        UnitStats stats = iconController.characterData;

        // 2. HACEMOS LA COMPROBACIÓN DE LÍMITES PRIMERO
        if (GameManager.Instance.CanPlaceUnit(teamID, stats) == false)
        {
            // Si CanPlaceUnit devuelve false, el GameManager ya habrá mostrado un error en la consola.
            // Simplemente detenemos la ejecución de esta función aquí.
            return; 
        }
        
        // 3. SI LA COMPROBACIÓN PASA, CONTINUAMOS CON EL ARRASTRE
        if (GameManager.Instance.CurrentState != GameManager.GameState.Placement || unitToReposition != null) return;
        
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
            Vector2 pointerPosition = playerControls.Gameplay.PointerPosition.ReadValue<Vector2>();
            Ray ray = Camera.main.ScreenPointToRay(pointerPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Node node = gridManager.NodeFromWorldPoint(hit.point);
                // La comprobación de límites no es necesaria aquí, porque ya la hicimos al empezar a arrastrar.
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
    
    // El resto de funciones se mantienen igual
    #region Funciones sin cambios
    void TryStartRepositioning(Vector2 pointerPosition) {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        Ray ray = Camera.main.ScreenPointToRay(pointerPosition);
        if (Physics.Raycast(ray, out RaycastHit hit)) {
            UnitController unit = hit.collider.GetComponent<UnitController>();
            if (unit != null) {
                unitToReposition = unit;
                originalNodeOfRepositionedUnit = unit.currentNode;
                originalNodeOfRepositionedUnit.isWalkable = true;
                PlacementUIManager.Instance.HideBenchesForDrag();
                if (trashZoneCanvasGroup != null) StartCoroutine(FadeCanvasGroup(trashZoneCanvasGroup, 0f, 1f));
            }
        }
    }

    void UpdateRepositioningUnit(Vector2 pointerPosition) {
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(pointerPosition);
        if (groundPlane.Raycast(ray, out float distance)) {
            Vector3 worldPosition = ray.GetPoint(distance);
            unitToReposition.transform.position = new Vector3(worldPosition.x, 0.5f, worldPosition.z);
            UpdateHighlightForRepositioning(pointerPosition);
        }
    }

    void DropRepositionedUnit(Vector2 pointerPosition) {
        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = pointerPosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        bool droppedOnTrash = false;
        foreach (RaycastResult result in results) { if (result.gameObject.GetComponent<TrashZoneController>() != null) { droppedOnTrash = true; break; } }
        if (droppedOnTrash) {
            if (unitToReposition.originatingIcon != null) { unitToReposition.originatingIcon.ResetIcon(); }
            unitToReposition.Die();
        } else {
            Node destinationNode = gridManager.NodeFromWorldPoint(unitToReposition.transform.position);
            if (gridManager.IsNodeValidForPlacement(destinationNode, unitToReposition.teamID)) {
                unitToReposition.transform.position = destinationNode.worldPosition;
                unitToReposition.currentNode = destinationNode;
                destinationNode.isWalkable = false;
            } else {
                unitToReposition.transform.position = originalNodeOfRepositionedUnit.worldPosition;
                unitToReposition.currentNode = originalNodeOfRepositionedUnit;
                originalNodeOfRepositionedUnit.isWalkable = false;
            }
        }
        PlacementUIManager.Instance.ShowBenchesAfterDrag();
        if (trashZoneCanvasGroup != null) StartCoroutine(FadeCanvasGroup(trashZoneCanvasGroup, 1f, 0f));
        unitToReposition = null;
        originalNodeOfRepositionedUnit = null;
        if (highlightInstance != null) highlightInstance.SetActive(false);
    }
    
    private void PlaceUnitOnNode(Node node, UnitStats unitStats) {
        if (unitStats?.characterPrefab != null) {
            GameObject unitInstance = Instantiate(unitStats.characterPrefab, node.worldPosition, Quaternion.identity);
            node.isWalkable = false;
            UnitController unitController = unitInstance.GetComponent<UnitController>();
            if (unitController != null) {
                unitController.unitStats = unitStats;
                unitController.currentNode = node;
                unitController.teamID = PlacementUIManager.Instance.CurrentPlacementTeamID;
                unitController.originatingIcon = currentlyDraggedIcon;
                GameManager.Instance.RegisterUnit(unitController);
            }
        }
    }
    
    private void UpdateHighlightForNewUnit(Vector2 pointerPosition) {
        if (highlightInstance == null || PlacementUIManager.Instance == null) return;
        int teamID = PlacementUIManager.Instance.CurrentPlacementTeamID;
        UpdateHighlight(teamID, pointerPosition);
    }
    
    private void UpdateHighlightForRepositioning(Vector2 pointerPosition) {
        if (highlightInstance == null || unitToReposition == null) return;
        Node nodeUnderUnit = gridManager.NodeFromWorldPoint(unitToReposition.transform.position);
        UpdateHighlight(unitToReposition.teamID, pointerPosition, nodeUnderUnit);
    }

    private void UpdateHighlight(int teamID, Vector2 pointerPosition, Node nodeToHighlight = null) {
        if (nodeToHighlight == null) {
            if (EventSystem.current.IsPointerOverGameObject()) { if (highlightInstance != null) highlightInstance.SetActive(false); return; }
            Ray ray = Camera.main.ScreenPointToRay(pointerPosition);
            if (Physics.Raycast(ray, out RaycastHit hit)) {
                nodeToHighlight = gridManager.NodeFromWorldPoint(hit.point);
            }
        }
        if (gridManager.IsNodeValidForPlacement(nodeToHighlight, teamID)) {
            highlightInstance.SetActive(true);
            highlightInstance.transform.position = nodeToHighlight.worldPosition;
        } else {
            highlightInstance.SetActive(false);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end) {
        float counter = 0f;
        if (end > start) {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
        while (counter < fadeDuration) {
            counter += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, counter / fadeDuration);
            yield return null;
        }
        cg.alpha = end;
        if (end < start) {
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        yield break;
    }
    #endregion
}