using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections; // Necesario para Coroutines

public class PlayerController : MonoBehaviour
{
    [Header("Referencias del Sistema")]
    public GridManager gridManager;
    public GameObject highlightPrefab;

    [Header("Referencias de UI")]
    public Image dragCursorImage;
    [Tooltip("El CanvasGroup de la UI que funciona como zona para eliminar unidades.")]
    public CanvasGroup trashZoneCanvasGroup; // <-- CORRECCIÓN: Ahora es un CanvasGroup
    [Tooltip("La duración en segundos de la animación de fade.")]
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
        
        // Al empezar, nos aseguramos de que la zona de eliminación esté completamente oculta.
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

    // --- Lógica de Reposicionar y Eliminar (con animación para la Trash Zone) ---

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
                
                PlacementUIManager.Instance.HideBenchesForDrag();

                // CORRECCIÓN: Usamos la corutina para mostrar la zona con un fade-in.
                if (trashZoneCanvasGroup != null) 
                    StartCoroutine(FadeCanvasGroup(trashZoneCanvasGroup, 0f, 1f));
            }
        }
    }

    void DropRepositionedUnit()
    {
        // ... (la lógica de detección de la papelera y reubicación es la misma) ...
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

        // --- Limpieza y Animación de Salida ---
        PlacementUIManager.Instance.ShowBenchesAfterDrag();

        // CORRECCIÓN: Usamos la corutina para ocultar la zona con un fade-out.
        if (trashZoneCanvasGroup != null) 
            StartCoroutine(FadeCanvasGroup(trashZoneCanvasGroup, 1f, 0f));

        unitToReposition = null;
        originalNodeOfRepositionedUnit = null;
        if (highlightInstance != null) highlightInstance.SetActive(false);
    }
    
    // --- NUEVA Corutina para la animación de Fade ---
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end)
    {
        float counter = 0f;
        
        // Hacemos la zona interactuable al empezar a mostrarse
        if (end > start)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        while (counter < fadeDuration)
        {
            counter += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, counter / fadeDuration);
            yield return null;
        }
        
        cg.alpha = end;

        // Dejamos de hacerla interactuable al terminar de ocultarse
        if (end < start)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }

    // --- El resto de funciones se mantienen igual ---
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