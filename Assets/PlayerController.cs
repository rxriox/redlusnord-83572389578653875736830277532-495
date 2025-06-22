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
        if (trashZoneCanvasGroup != null)
        {
            trashZoneCanvasGroup.alpha = 0;
            trashZoneCanvasGroup.interactable = false;
            trashZoneCanvasGroup.blocksRaycasts = false;
        }
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

    // --- Lógica para arrastrar DESDE LA UI ---

    public void StartDraggingUnit(UnitIconController iconController)
    {
        if (GameManager.Instance.CanPlaceUnit(PlacementUIManager.Instance.CurrentPlacementTeamID) == false)
        {
            Debug.Log("No se puede arrastrar una nueva unidad. Límite del equipo alcanzado.");
            return;
        }
        if (GameManager.Instance.CurrentState != GameManager.GameState.Placement || unitToReposition != null) return;
        
        currentlyDraggedIcon = iconController;
        dragCursorImage.sprite = currentlyDraggedIcon.GetDragCursorSprite();
        dragCursorImage.raycastTarget = false;
        dragCursorImage.gameObject.SetActive(true);
        
        // CORRECCIÓN: Hemos quitado la línea que mostraba la papelera aquí.
        // La papelera solo debe aparecer al reubicar, no al colocar una unidad nueva.
    }

    public void StopDraggingUnit()
    {
        if (currentlyDraggedIcon == null) return;

        // Comprobamos si se soltó sobre la UI. Usamos IsPointerOverGameObject para una detección simple.
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
        
        // Limpiamos el estado de arrastre de la UI.
        currentlyDraggedIcon = null;
        dragCursorImage.gameObject.SetActive(false);
        if (highlightInstance != null) highlightInstance.SetActive(false);
        // CORRECCIÓN: Nos aseguramos de que no hay ninguna línea intentando ocultar la papelera aquí.
    }

    // --- Lógica para REPOSICIONAR unidades DEL TABLERO ---

    void TryStartRepositioning()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            UnitController unit = hit.collider.GetComponent<UnitController>();
            if (unit != null)
            {
                unitToReposition = unit;
                originalNodeOfRepositionedUnit = unit.currentNode;
                originalNodeOfRepositionedUnit.isWalkable = true;
                
                // Le decimos al UI Manager que oculte las bancas con un fade.
                PlacementUIManager.Instance.HideBenchesForDrag();
                
                // Mostramos la zona de eliminación con un fade.
                if (trashZoneCanvasGroup != null) 
                    StartCoroutine(FadeCanvasGroup(trashZoneCanvasGroup, 0f, 1f));
            }
        }
    }

    void DropRepositionedUnit()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
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

        // Le decimos al UI Manager que vuelva a mostrar la banca activa.
        PlacementUIManager.Instance.ShowBenchesAfterDrag();
        // Ocultamos la zona de eliminación con un fade.
        if (trashZoneCanvasGroup != null) 
            StartCoroutine(FadeCanvasGroup(trashZoneCanvasGroup, 1f, 0f));

        // Limpieza final.
        unitToReposition = null;
        originalNodeOfRepositionedUnit = null;
        if (highlightInstance != null) highlightInstance.SetActive(false);
    }
    
    // El resto de funciones se mantienen igual, pero las incluyo para que tengas el script completo.
    #region Funciones sin cambios
    void UpdateRepositioningUnit() {
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (groundPlane.Raycast(ray, out float distance)) {
            Vector3 worldPosition = ray.GetPoint(distance);
            unitToReposition.transform.position = new Vector3(worldPosition.x, 0.5f, worldPosition.z);
            UpdateHighlightForRepositioning();
        }
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

    private void UpdateHighlightForNewUnit() {
        if (highlightInstance == null || PlacementUIManager.Instance == null) return;
        int teamID = PlacementUIManager.Instance.CurrentPlacementTeamID;
        UpdateHighlight(teamID);
    }
    
    private void UpdateHighlightForRepositioning() {
        if (highlightInstance == null || unitToReposition == null) return;
        Node nodeUnderUnit = gridManager.NodeFromWorldPoint(unitToReposition.transform.position);
        UpdateHighlight(unitToReposition.teamID, nodeUnderUnit);
    }

    private void UpdateHighlight(int teamID, Node nodeToHighlight = null) {
        if (nodeToHighlight == null) {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
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
    }
    #endregion
}