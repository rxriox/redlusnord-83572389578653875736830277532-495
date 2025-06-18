using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

    private Image iconImage;
    private bool isDraggable = true;
    private GameObject draggedObject;
    private RectTransform canvasRectTransform;

    void Awake()
    {
        iconImage = GetComponent<Image>();
        iconImage.sprite = availableSprite;
        canvasRectTransform = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
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
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedObject == null) return;
        
        Destroy(draggedObject);

        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        bool placementSuccessful = false;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Tile")))
        {
            GridManager gridManager = FindAnyObjectByType<GridManager>();
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