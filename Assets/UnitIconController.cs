using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class UnitIconController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Configuración de la Unidad")]
    public UnitStats characterData;

    [Header("Iconos y Cursor")]
    public Sprite availableIconSprite;
    public Sprite placedIconSprite;
    public Sprite dragCursorSprite;

    private Image iconImage;
    private PlayerController playerController;
    private bool isPlaced = false;

    void Start()
    {
        iconImage = GetComponent<Image>();
        playerController = FindFirstObjectByType<PlayerController>();
        ResetIcon();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isPlaced) return; // No hacer nada si ya está colocada
        
        // Muestra el panel de detalles si se hace un clic simple
        PlacementUIManager.Instance.ShowDetailsPanel(characterData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isPlaced && eventData.button == PointerEventData.InputButton.Left)
        {
            PlacementUIManager.Instance.ShowDetailsPanel(characterData);
            playerController.StartDraggingUnit(this);
        }
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            PlacementUIManager.Instance.HideDetailsPanel();
            playerController.StopDraggingUnit();
        }
    }
    
    public void SetSpriteToPlacedState()
    {
        if (placedIconSprite != null)
        {
            iconImage.sprite = placedIconSprite;
        }
    }

    public void SetAsPlaced()
    {
        isPlaced = true;
        if (placedIconSprite != null)
        {
            iconImage.sprite = placedIconSprite;
        }
    }
    
    public void ResetIcon()
    {
        isPlaced = false;
        if (availableIconSprite != null)
        {
            iconImage.sprite = availableIconSprite;
        }
    }
    
    public UnitStats GetUnitStats() { return characterData; }
    public Sprite GetDragCursorSprite() { return dragCursorSprite; }
}