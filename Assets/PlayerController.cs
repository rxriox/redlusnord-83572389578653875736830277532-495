using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    [Header("Indicadores 3D Fijos")]
    [Tooltip("El objeto Plane que se activa al arrastrar una unidad aliada.")]
    public GameObject allyDragIndicatorPlane;
    [Tooltip("El objeto Plane que se activa al arrastrar una unidad enemiga.")]
    public GameObject enemyDragIndicatorPlane;
    public static PlayerController Instance { get; private set; }
    [Header("Referencias del Sistema")]
    public GridManager gridManager;
    public GameObject highlightPrefab;

    [Header("Referencias de UI")]
    public Image dragCursorImage;
    public CanvasGroup trashZoneCanvasGroup;
    public float fadeDuration = 0.2f;

    [Header("Configuración de Interacción")]
    [Tooltip("Tiempo en segundos para que un clic se convierta en arrastre.")]
    public float dragDelay = 0.2f;

    private float pointerDownTimer = 0f;
    private bool isDraggingForReposition = false;
    private UnitController potentialRepositionTarget;

    private GameObject highlightInstance;
    private UnitIconController currentlyDraggedIcon;
    private UnitController unitToReposition;
    private Node originalNodeOfRepositionedUnit;
    private PlayerControl playerControls;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        playerControls = new PlayerControl();
    }
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
        if (allyDragIndicatorPlane != null) allyDragIndicatorPlane.SetActive(false);
        if (enemyDragIndicatorPlane != null) enemyDragIndicatorPlane.SetActive(false);
    }

    void Update()
    {
        Vector2 pointerPosition = playerControls.Gameplay.PointerPosition.ReadValue<Vector2>();

        // --- MODIFICACIÓN CLAVE: Actualiza la posición del cursor si se está arrastrando CUALQUIER COSA ---
        if (currentlyDraggedIcon != null || isDraggingForReposition)
        {
            if (dragCursorImage != null && dragCursorImage.gameObject.activeInHierarchy)
            {
                dragCursorImage.transform.position = pointerPosition;
            }
        }
        
        // Caso 1: Arrastrando un icono NUEVO desde la banca
        if (currentlyDraggedIcon != null)
        {
            UpdateHighlight(PlacementUIManager.Instance.CurrentPlacementTeamID, pointerPosition);
            return;
        }

        // Caso 2: Ya estamos en medio del proceso de REPOSICIONAR una unidad
        if (isDraggingForReposition && unitToReposition != null)
        {
            // Ya no necesitamos mover el cursor aquí, se hace arriba.
            // Solo actualizamos el highlight del tablero.
            UpdateRepositioningUnit(pointerPosition);

            if (playerControls.Gameplay.Click.WasReleasedThisFrame())
            {
                DropRepositionedUnit(pointerPosition);
            }
            return;
        }
        
        // Lógica principal para detectar clic vs arrastre en el tablero
        HandleBoardInteraction(pointerPosition);
    }

    public void ClearInteractionState()
    {
        ResetInteractionState();
    }

    public void StartDraggingUnit(UnitIconController iconController)
    {
        int teamID = PlacementUIManager.Instance.CurrentPlacementTeamID;
        UnitStats stats = iconController.characterData;

        if (GameManager.Instance.CanPlaceUnit(teamID, stats) == false)
        {
            return;
        }

        if (GameManager.Instance.CurrentState != GameManager.GameState.Placement || unitToReposition != null) return;

        currentlyDraggedIcon = iconController;
        // La siguiente línea ya la hace el icono, pero la dejamos por si acaso.
        // Opcionalmente, puedes eliminarla si quieres que el icono solo cambie al colocarlo con éxito.
        currentlyDraggedIcon.SetSpriteToPlacedState(); 
        
        dragCursorImage.sprite = currentlyDraggedIcon.GetDragCursorSprite();
        dragCursorImage.raycastTarget = false;
        dragCursorImage.gameObject.SetActive(true);
        if (teamID == 0)
        {
            if (allyDragIndicatorPlane != null) allyDragIndicatorPlane.SetActive(true);
        }
        else if (teamID == 1)
        {
            if (enemyDragIndicatorPlane != null) enemyDragIndicatorPlane.SetActive(true);
        }

        // NO llamamos a PlacementUIManager.Instance.ShowDetailsPanel(stats); aquí.
    }

    public void StopDraggingUnit()
    {
        if (currentlyDraggedIcon == null) return;
        bool placementSuccessful = false;
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            Vector2 pointerPosition = playerControls.Gameplay.PointerPosition.ReadValue<Vector2>();
            Ray ray = Camera.main.ScreenPointToRay(pointerPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Node node = gridManager.NodeFromWorldPoint(hit.point);
                if (gridManager.IsNodeValidForPlacement(node, PlacementUIManager.Instance.CurrentPlacementTeamID))
                {
                    PlaceUnitOnNode(node, currentlyDraggedIcon.GetUnitStats());
                    currentlyDraggedIcon.SetAsPlaced();
                    placementSuccessful = true;
                }
            }
        }

        if (!placementSuccessful)
        {
            currentlyDraggedIcon.ResetIcon();
        }
        currentlyDraggedIcon = null;
        dragCursorImage.gameObject.SetActive(false);
        if (highlightInstance != null) highlightInstance.SetActive(false);
        if (allyDragIndicatorPlane != null) allyDragIndicatorPlane.SetActive(false);
        if (enemyDragIndicatorPlane != null) enemyDragIndicatorPlane.SetActive(false);
    }

    private void HandleBoardInteraction(Vector2 pointerPosition)
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Placement || EventSystem.current.IsPointerOverGameObject())
        {
            ResetInteractionState();
            return;
        }

        // Al presionar el clic
        if (playerControls.Gameplay.Click.WasPressedThisFrame())
        {
            Ray ray = Camera.main.ScreenPointToRay(pointerPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                UnitController unit = hit.collider.GetComponent<UnitController>();
                if (unit != null)
                {
                    potentialRepositionTarget = unit;
                    pointerDownTimer = 0f; // Inicia el temporizador
                }
            }
        }
        
        // Mientras se mantiene presionado el clic
        if (playerControls.Gameplay.Click.IsPressed() && potentialRepositionTarget != null)
        {
            pointerDownTimer += Time.deltaTime;
            // Si el tiempo supera el umbral y aún no estamos arrastrando
            if (pointerDownTimer >= dragDelay && !isDraggingForReposition)
            {
                StartRepositioning(potentialRepositionTarget); // Inicia el arrastre
            }
        }

        // Al soltar el clic
        if (playerControls.Gameplay.Click.WasReleasedThisFrame())
        {
            // Si teníamos una unidad como objetivo pero NO se inició el arrastre (clic corto)
            if (potentialRepositionTarget != null && !isDraggingForReposition)
            {
                // ¡Esto es un CLIC! Mostramos el panel.
                PlacementUIManager.Instance.ShowDetailsPanel(potentialRepositionTarget.unitStats);
            }
            // Reseteamos el estado de la interacción
            ResetInteractionState();
        }
    }

    private void ResetInteractionState()
    {
        pointerDownTimer = 0f;
        potentialRepositionTarget = null;
    }
    void StartRepositioning(UnitController unit)
    {
        isDraggingForReposition = true; // Marcamos que estamos arrastrando

        // Muestra el panel de detalles al empezar a arrastrar
        PlacementUIManager.Instance.ShowDetailsPanel(unit.unitStats);

        unitToReposition = unit;
        originalNodeOfRepositionedUnit = unit.currentNode;
        originalNodeOfRepositionedUnit.isWalkable = true;

        PlacementUIManager.Instance.HideBenchesForDrag();

        if (trashZoneCanvasGroup != null)
            StartCoroutine(FadeCanvasGroup(trashZoneCanvasGroup, 0f, 1f));

        unit.gameObject.SetActive(false);

        if (dragCursorImage != null && unit.originatingIcon != null)
        {
            dragCursorImage.sprite = unit.originatingIcon.GetDragCursorSprite();
            dragCursorImage.gameObject.SetActive(true);
        }

        if (unit.teamID == 0)
        {
            if (allyDragIndicatorPlane != null)
                allyDragIndicatorPlane.SetActive(true);
        }
        else if (unit.teamID == 1)
        {
            if (enemyDragIndicatorPlane != null)
                enemyDragIndicatorPlane.SetActive(true);
        }
    }



    void UpdateRepositioningUnit(Vector2 pointerPosition)
    {
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(pointerPosition);
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPosition = ray.GetPoint(distance);
            unitToReposition.transform.position = new Vector3(worldPosition.x, 0.5f, worldPosition.z);
            UpdateHighlightForRepositioning(pointerPosition);
        }
        
    }

    void DropRepositionedUnit(Vector2 pointerPosition)
    {
        PlacementUIManager.Instance.HideDetailsPanel();
        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = pointerPosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        bool droppedOnTrash = false;
        foreach (RaycastResult result in results) { if (result.gameObject.GetComponent<TrashZoneController>() != null) { droppedOnTrash = true; break; } }
        if (droppedOnTrash)
        {
            if (unitToReposition.originatingIcon != null) { unitToReposition.originatingIcon.ResetIcon(); }
            unitToReposition.Die(null);
        }
        else
        {
            Node destinationNode = gridManager.NodeFromWorldPoint(unitToReposition.transform.position);
            if (gridManager.IsNodeValidForPlacement(destinationNode, unitToReposition.teamID))
            {
                unitToReposition.transform.position = destinationNode.worldPosition;
                unitToReposition.currentNode = destinationNode;
                destinationNode.isWalkable = false;
                unitToReposition.gameObject.SetActive(true);
            }
            else
            {
                unitToReposition.transform.position = originalNodeOfRepositionedUnit.worldPosition;
                unitToReposition.currentNode = originalNodeOfRepositionedUnit;
                originalNodeOfRepositionedUnit.isWalkable = false;
                unitToReposition.gameObject.SetActive(true);
            }
        }
        isDraggingForReposition = false; // Reseteamos el flag de arrastre
        unitToReposition = null;         // Liberamos la referencia
        originalNodeOfRepositionedUnit = null;
        PlacementUIManager.Instance.ShowBenchesAfterDrag();
        if (trashZoneCanvasGroup != null) StartCoroutine(FadeCanvasGroup(trashZoneCanvasGroup, 1f, 0f));
        if (dragCursorImage != null)
            dragCursorImage.gameObject.SetActive(false);
        unitToReposition = null;
        originalNodeOfRepositionedUnit = null;
        if (highlightInstance != null) highlightInstance.SetActive(false);
        if (allyDragIndicatorPlane != null) allyDragIndicatorPlane.SetActive(false);
        if (enemyDragIndicatorPlane != null) enemyDragIndicatorPlane.SetActive(false);
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
                unitController.originatingIcon = currentlyDraggedIcon;
                GameManager.Instance.RegisterUnit(unitController);
            }
        }
    }

    private void UpdateHighlightForNewUnit(Vector2 pointerPosition)
    {
        if (highlightInstance == null || PlacementUIManager.Instance == null) return;
        int teamID = PlacementUIManager.Instance.CurrentPlacementTeamID;
        UpdateHighlight(teamID, pointerPosition);
    }


    private void UpdateHighlightForRepositioning(Vector2 pointerPosition)
    {
        if (highlightInstance == null || unitToReposition == null) return;
        Node nodeUnderUnit = gridManager.NodeFromWorldPoint(unitToReposition.transform.position);
        UpdateHighlight(unitToReposition.teamID, pointerPosition, nodeUnderUnit);
    }

    private void UpdateHighlight(int teamID, Vector2 pointerPosition, Node nodeToHighlight = null)
    {
        if (nodeToHighlight == null)
        {
            if (EventSystem.current.IsPointerOverGameObject()) { if (highlightInstance != null) highlightInstance.SetActive(false); return; }
            Ray ray = Camera.main.ScreenPointToRay(pointerPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                nodeToHighlight = gridManager.NodeFromWorldPoint(hit.point);
            }
        }
        if (gridManager.IsNodeValidForPlacement(nodeToHighlight, teamID))
        {
            highlightInstance.SetActive(true);
            highlightInstance.transform.position = nodeToHighlight.worldPosition;
        }
        else
        {
            highlightInstance.SetActive(false);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end)
    {
        float counter = 0f;
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
        if (end < start)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        yield break;
    }
    public void ShowTrashZone()
    {
        if (trashZoneCanvasGroup != null)
            StartCoroutine(FadeCanvasGroup(trashZoneCanvasGroup, 0f, 1f));
    }
    public void HideTrashZone()
    {
        if (trashZoneCanvasGroup != null)
            StartCoroutine(FadeCanvasGroup(trashZoneCanvasGroup, 1f, 0f));
    }
}