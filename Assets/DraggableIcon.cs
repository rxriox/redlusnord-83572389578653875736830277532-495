using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Gestiona el comportamiento de arrastrar y soltar de un ícono de unit en la UI,
/// permitiendo colocar units en el tablero.
/// Ahora utiliza un cubo de resaltado temporal en el tile sobre el que se pasa el ratón,
/// y pasa el ID del equipo para la validación de zona de colocación.
/// </summary>
public class DraggableIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Configuración de la Unit")]
    [Tooltip("El prefab del personaje que este ícono representa.")]
    public GameObject unitPrefab;
    [Tooltip("El ID del equipo de esta unit. 0 para aliado, 1 para enemigo.")]
    [SerializeField] public int unitTeamID; // ID del equipo de la unidad para validación de zona.

    [Header("Configuración Visual")]
    [Tooltip("La imagen que se muestra cuando el ícono está disponible.")]
    public Sprite availableSprite;
    [Tooltip("La imagen que se muestra cuando el ícono está en uso o siendo arrastrado.")]
    public Sprite usedSprite;
    [Tooltip("Prefab del cubo de resaltado que se mostrará en el tile objetivo.")]
    [SerializeField] private GameObject highlightCubePrefab; 

    private Image iconImage;
    private bool isDraggable = true;
    private GameObject draggedObject;
    private RectTransform canvasRectTransform;
    private GridManager gridManager; 

    private GameObject currentHighlightCube; 

    void Awake()
    {
        iconImage = GetComponent<Image>();
        iconImage.sprite = availableSprite;
        canvasRectTransform = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        
        gridManager = FindAnyObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("DraggableIcon: No se encontró un GridManager en la escena. Asegúrate de que uno exista.");
        }
        else
        {
            if (gridManager.tilePrefab != null)
            {
                if (gridManager.tilePrefab.GetComponent<Collider>() == null)
                {
                    Debug.LogWarning("DraggableIcon: The Tile Prefab assigned in GridManager does NOT have a Collider component. Raycasts will not hit it!");
                }

                if (gridManager.tilePrefab.layer != LayerMask.NameToLayer("Tile") || LayerMask.NameToLayer("Tile") == -1)
                {
                    Debug.LogWarning($"DraggableIcon: The Tile Prefab assigned in GridManager is on layer '{LayerMask.LayerToName(gridManager.tilePrefab.layer)}' but the raycast targets layer 'Tile'. Make sure layer 'Tile' exists and is assigned correctly!");
                }
            }
            else
            {
                Debug.LogError("DraggableIcon: No Tile Prefab assigned in GridManager. Tiles cannot be generated or interacted with.");
            }
            if (highlightCubePrefab == null)
            {
                Debug.LogWarning("DraggableIcon: Highlight Cube Prefab is not assigned. No visual feedback for tile hovering will be shown.");
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isDraggable || draggedObject != null) return;

        draggedObject = new GameObject("DraggedIcon");
        RectTransform draggedRect = draggedObject.AddComponent<RectTransform>();
        draggedObject.transform.SetParent(transform.root);
        draggedObject.transform.SetAsLastSibling();
        
        draggedRect.sizeDelta = GetComponent<RectTransform>().sizeDelta;

        Image draggedImage = draggedObject.AddComponent<Image>();
        draggedImage.sprite = availableSprite;
        draggedImage.raycastTarget = false; 
        
        OnDrag(eventData); 
        
        iconImage.sprite = usedSprite; 
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedObject == null) return;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPos);
        draggedObject.transform.localPosition = localPos;

        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        RaycastHit hit;
        
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red); 

        Node hoveredNode = null;
        if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("Tile")))
        {
            hoveredNode = gridManager.NodeFromWorldPoint(hit.transform.position);
        }

        // Determinar si el nodo sobre el que se pasa el ratón es válido para la colocación visual.
        // Un nodo es válido si no es nulo, está disponible Y está en la zona de equipo correcta.
        bool isValidForHighlight = false; 
        if (hoveredNode != null && hoveredNode.IsAvailable())
        {
            int halfHeight = gridManager.gridHeight / 2;
            if (unitTeamID == 0) // Aliado (jugador) - mitad inferior
            {
                // Solo resalta si el nodo está en la mitad inferior de la cuadrícula (Z < halfHeight)
                if (hoveredNode.gridZ < halfHeight) 
                {
                    isValidForHighlight = true;
                }
            }
            else if (unitTeamID == 1) // Enemigo - mitad superior
            {
                // Solo resalta si el nodo está en la mitad superior de la cuadrícula (Z >= halfHeight)
                if (hoveredNode.gridZ >= halfHeight) 
                {
                    isValidForHighlight = true;
                }
            }
        }

        // Gestionar el cubo de resaltado
        if (isValidForHighlight) 
        {
            // Si no hay un cubo activo o si el cubo activo no está en la posición correcta
            if (currentHighlightCube == null || currentHighlightCube.transform.position != hoveredNode.worldPosition + Vector3.up * 0.05f)
            {
                if (currentHighlightCube != null)
                {
                    Destroy(currentHighlightCube);
                }
                if (highlightCubePrefab != null)
                {
                    currentHighlightCube = Instantiate(highlightCubePrefab, hoveredNode.worldPosition + Vector3.up * 0.05f, Quaternion.identity);
                }
            }
        }
        else // Si el nodo NO es válido para resaltar (ocupado, fuera de zona, etc.) o no se golpeó ningún tile
        {
            if (currentHighlightCube != null)
            {
                Destroy(currentHighlightCube);
                currentHighlightCube = null;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedObject == null) return;
        
        Destroy(draggedObject); 

        // Destruye el cubo de resaltado al finalizar el arrastre.
        if (currentHighlightCube != null)
        {
            Destroy(currentHighlightCube);
            currentHighlightCube = null;
        }

        bool placementSuccessful = false;
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Tile")))
        {
            // PlaceUnitOnTile en GridManager ahora maneja TODAS las validaciones:
            // 1. Si el nodo está disponible (no ocupado).
            // 2. Si está en la zona correcta para el unitTeamID.
            placementSuccessful = gridManager.PlaceUnitOnTile(unitPrefab, hit.transform, unitTeamID); 
        }

        if (placementSuccessful)
        {
            isDraggable = false; 
        }
        else
        {
            iconImage.sprite = availableSprite; 
        }
    }
}
