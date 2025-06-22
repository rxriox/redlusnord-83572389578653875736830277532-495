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
                if (node != null && node.isWalkable)
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

    private void UpdateHighlight()
    {
        if (highlightInstance == null) return;
        if (EventSystem.current.IsPointerOverGameObject())
        {
            highlightInstance.SetActive(false);
            return;
        }
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Node node = gridManager.NodeFromWorldPoint(hit.point);
            if (node != null && node.isWalkable)
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
        // CORRECCIÓN: Usando 'characterPrefab'.
        if (unitStats?.characterPrefab != null && node != null && node.isWalkable)
        {
            GameObject unitInstance = Instantiate(unitStats.characterPrefab, node.worldPosition, Quaternion.identity);
            node.isWalkable = false;

            UnitController unitController = unitInstance.GetComponent<UnitController>();
            if (unitController != null)
            {
                unitController.currentNode = node;
                unitController.teamID = 0; 
                GameManager.Instance.RegisterUnit(unitController);
            }
            else
            {
                Debug.LogError($"El prefab de la unidad '{unitStats.unitName}' no tiene el componente UnitController.");
            }
        }
    }
}