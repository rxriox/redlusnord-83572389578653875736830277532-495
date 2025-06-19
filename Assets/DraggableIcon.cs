using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Gestiona el comportamiento de arrastrar y soltar de un ícono de unidad en la UI,
/// permitiendo colocar unidades en el tablero.
/// Ahora utiliza un cubo de resaltado temporal en el tile sobre el que se pasa el ratón.
/// </summary>
public class DraggableIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Configuración de la Unidad")]
    [Tooltip("El prefab del personaje que este ícono representa.")]
    public GameObject unitPrefab;

    [Header("Configuración Visual")]
    [Tooltip("La imagen que se muestra cuando el ícono está disponible.")]
    public Sprite availableSprite;
    [Tooltip("La imagen que se muestra cuando el ícono está en uso o siendo arrastrado.")]
    public Sprite usedSprite;
    [Tooltip("Prefab del cubo de resaltado que se mostrará en el tile objetivo.")]
    [SerializeField] private GameObject highlightCubePrefab; // NUEVO: Prefab para el cubo de resaltado

    private Image iconImage;
    private bool isDraggable = true;
    private GameObject draggedObject;
    private RectTransform canvasRectTransform;
    private GridManager gridManager; 

    private GameObject currentHighlightCube; // NUEVO: Instancia actual del cubo de resaltado

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
            // NEW: Safety checks for the tile prefab in GridManager
            // Ahora tilePrefab es public en GridManager.
            if (gridManager.tilePrefab != null)
            {
                // Check if the tile prefab has a collider
                if (gridManager.tilePrefab.GetComponent<Collider>() == null)
                {
                    Debug.LogWarning("DraggableIcon: The Tile Prefab assigned in GridManager does NOT have a Collider component. Raycasts will not hit it!");
                }

                // Check if the tile prefab is on the correct layer
                // LayerMask.NameToLayer("Tile") devuelve -1 si la capa no existe.
                if (gridManager.tilePrefab.layer != LayerMask.NameToLayer("Tile") || LayerMask.NameToLayer("Tile") == -1)
                {
                    Debug.LogWarning($"DraggableIcon: The Tile Prefab assigned in GridManager is on layer '{LayerMask.LayerToName(gridManager.tilePrefab.layer)}' but the raycast targets layer 'Tile'. Make sure layer 'Tile' exists and is assigned correctly!");
                }
            }
            else
            {
                Debug.LogError("DraggableIcon: No Tile Prefab assigned in GridManager. Tiles cannot be generated or interacted with.");
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
        draggedImage.raycastTarget = false; // Importante para que los eventos de raycast pasen a los tiles debajo.
        
        OnDrag(eventData); // Posiciona el objeto arrastrado al inicio y maneja el cubo de resaltado.
        
        iconImage.sprite = usedSprite; // Cambia la apariencia del ícono original.
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedObject == null) return;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPos);
        draggedObject.transform.localPosition = localPos;

        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        RaycastHit hit;
        
        // Debug visualization for the raycast (visible in Scene view during play mode)
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red); 

        // Lógica para el cubo de resaltado
        if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("Tile")))
        {
            Node hoveredNode = gridManager.NodeFromWorldPoint(hit.transform.position);

            // Solo si el nodo existe y está disponible, mostramos el cubo.
            if (hoveredNode != null && hoveredNode.IsAvailable()) 
            {
                // Si no hay un cubo de resaltado activo o si estamos sobre un nuevo tile disponible
                if (currentHighlightCube == null || currentHighlightCube.transform.position != hoveredNode.worldPosition)
                {
                    // Destruye el cubo anterior si existe.
                    if (currentHighlightCube != null)
                    {
                        Destroy(currentHighlightCube);
                    }

                    // Instancia un nuevo cubo de resaltado en la posición del tile.
                    if (highlightCubePrefab != null)
                    {
                        // Ajusta la altura del cubo ligeramente para que no se fusione con el tile.
                        Vector3 cubePosition = hoveredNode.worldPosition + Vector3.up * 0.05f; 
                        currentHighlightCube = Instantiate(highlightCubePrefab, cubePosition, Quaternion.identity);
                        // Opcional: Podrías hacer que el cubo sea hijo del tile si lo prefieres.
                        // currentHighlightCube.transform.SetParent(hit.transform); 
                    }
                }
            }
            else // Si el nodo NO está disponible (ocupado, etc.) o no es válido
            {
                if (currentHighlightCube != null)
                {
                    Destroy(currentHighlightCube); // Oculta el cubo si el tile no es válido para colocar.
                    currentHighlightCube = null;
                }
            }
        }
        else // Si el raycast no golpea ningún tile
        {
            if (currentHighlightCube != null)
            {
                Destroy(currentHighlightCube); // Oculta el cubo.
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
        
        // Asegúrate de que la capa "Tile" esté configurada en tus objetos de tile.
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Tile")))
        {
            if (gridManager != null)
            {
                placementSuccessful = gridManager.PlaceUnitOnTile(unitPrefab, hit.transform);
            }
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
