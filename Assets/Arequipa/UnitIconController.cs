using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class UnitIconController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Configuración de la Unidad")]
    public UnitStats characterData;

    [Header("Iconos y Cursor")]
    public Sprite availableIconSprite;
    public Sprite placedIconSprite;
    public Sprite dragCursorSprite;

    [Tooltip("Tiempo en segundos para que un clic en el icono se convierta en arrastre.")]
    public float dragDelay = 0.01f;
    private float pointerDownTimer = 0f;
    private bool isDragging = false;
    private bool isPointerDown = false;

    private Image iconImage;
    private PlayerController playerController;
    private bool isPlaced = false;

    void Start()
    {
        iconImage = GetComponent<Image>();
        playerController = FindFirstObjectByType<PlayerController>();
        ResetIcon();
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (isPlaced || eventData.button != PointerEventData.InputButton.Left) return;
        
        isPointerDown = true;
        pointerDownTimer = 0f;
        isDragging = false;
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPointerDown || eventData.button != PointerEventData.InputButton.Left) return;

        isPointerDown = false;

        if (isDragging)
        {
            playerController.StopDraggingUnit();
            PlacementUIManager.Instance.HideDetailsPanel();
        }
        else
        {
            PlacementUIManager.Instance.ShowDetailsPanel(characterData);
        }
        
        isDragging = false;
        pointerDownTimer = 0f;
    }

    

    public void OnDrag(PointerEventData eventData)
    {
        if (!isPointerDown || isPlaced || eventData.button != PointerEventData.InputButton.Left) return;
        if (!isDragging)
        {
            pointerDownTimer += Time.deltaTime;
            if (pointerDownTimer >= dragDelay)
            {
                isDragging = true;
                PlacementUIManager.Instance.ShowDetailsPanel(characterData);
                playerController.StartDraggingUnit(this);
            }
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