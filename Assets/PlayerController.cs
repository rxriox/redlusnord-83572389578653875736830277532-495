using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Referencias del Sistema")]
    public GridManager gridManager;
    public GameObject highlightPrefab;

    [Header("Referencias de UI")]
    public Image dragCursorImage;

    private GameObject highlightInstance;
    private UnitIconController currentlyDraggedIcon;
    private PlayerControl playerControls;

    void Awake() { playerControls = new PlayerControl(); }
    private void OnEnable() { playerControls.Gameplay.Enable(); }
    private void OnDisable() { playerControls.Gameplay.Disable(); }

    void Start()
    {
        if (highlightPrefab != null)
        {
            highlightInstance = Instantiate(highlightPrefab);
            highlightInstance.SetActive(false);
        }
        if (dragCursorImage != null)
        {
            dragCursorImage.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (currentlyDraggedIcon != null)
        {
            dragCursorImage.transform.position = Mouse.current.position.ReadValue();
            UpdateHighlight();
        }
    }

    public void StartDraggingUnit(UnitIconController iconController)
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Placement || currentlyDraggedIcon != null) return;
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
                int currentTeamID = PlacementUIManager.Instance.CurrentPlacementTeamID;

                // CORRECCIÓN: Le preguntamos de nuevo al GridManager antes de colocar.
                if (gridManager.IsNodeValidForPlacement(node, currentTeamID))
                {
                    PlaceUnitOnNode(node, currentlyDraggedIcon.GetUnitStats());
                    currentlyDraggedIcon.SetAsPlaced();
                }
            }
        }

        // Limpiamos el estado de arrastre sin importar el resultado.
        currentlyDraggedIcon = null;
        dragCursorImage.gameObject.SetActive(false);
        if (highlightInstance != null) highlightInstance.SetActive(false);
    }

    private void UpdateHighlight()
    {
        if (highlightInstance == null || PlacementUIManager.Instance == null) return;

        if (EventSystem.current.IsPointerOverGameObject())
        {
            highlightInstance.SetActive(false);
            return;
        }
        
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Node node = gridManager.NodeFromWorldPoint(hit.point);
            int currentTeamID = PlacementUIManager.Instance.CurrentPlacementTeamID;

            // CORRECCIÓN: Le preguntamos al GridManager si la casilla es válida para el equipo actual.
            if (gridManager.IsNodeValidForPlacement(node, currentTeamID))
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

    private void PlaceUnitOnNode(Node node, UnitStats unitStats)
    {
        Debug.Log("--- Intentando colocar unidad ---");

        if (unitStats == null)
        {
            Debug.LogError("FALLO: ¡unitStats es NULO! El icono en la UI no tiene su 'Character Data' asignado.");
            return;
        }

        Debug.Log("Unidad a colocar: " + unitStats.unitName);

        if (unitStats.characterPrefab == null)
        {
            Debug.LogError("FALLO: ¡El 'characterPrefab' en el ScriptableObject '" + unitStats.name + "' no está asignado en el Inspector!");
            return;
        }

        if (node == null)
        {
            Debug.LogError("FALLO: ¡El nodo donde intentas colocar es NULO! Problema con el GridManager o el Raycast.");
            return;
        }

        if (!node.isWalkable)
        {
            Debug.LogWarning("AVISO: El nodo en la posición " + node.worldPosition + " no está disponible (isWalkable es false).");
            return;
        }

        Debug.Log("ÉXITO: Todas las comprobaciones son correctas. Instanciando " + unitStats.characterPrefab.name);

        GameObject unitInstance = Instantiate(unitStats.characterPrefab, node.worldPosition, Quaternion.identity);
        node.isWalkable = false;

        UnitController unitController = unitInstance.GetComponent<UnitController>();
        if (unitController != null)
        {
            unitController.currentNode = node;
            unitController.teamID = 0; 
            GameManager.Instance.RegisterUnit(unitController);
            Debug.Log("<color=green>¡Unidad instanciada y registrada con éxito!</color>");
        }
        else
        {
            Debug.LogError($"El prefab de la unidad '{unitStats.unitName}' no tiene el componente UnitController.");
        }
    }
}