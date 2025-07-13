using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ArtifactIconController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("El ScriptableObject del artefacto que este icono representa.")]
    public Artifact artifactData;

    private Image iconImage;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 startPosition;

    void Awake()
    {
        iconImage = GetComponent<Image>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        ResetIcon();
    }

    public void SetAsPlaced()
    {
        isPlaced = true;
        if (placedSprite != null) iconImage.sprite = placedSprite;
    }
    public void ResetIcon()
    {
        isPlaced = false;
        if (availableSprite != null) iconImage.sprite = availableSprite;
    }
    public void OnBeginDrag(PointerEventData eventData)
    {

        if (isPlaced) return;
        
        if (artifactData == null) return;
        
        TabGroupManager.Instance.OnArtifactDragStart(this);
        
        canvasGroup.alpha = 0.4f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData){}

    public void OnEndDrag(PointerEventData eventData)
    {
        // Notificamos al manager que el arrastre ha terminado.
        if (TabGroupManager.Instance != null)
        {
            TabGroupManager.Instance.OnArtifactDragEnd();
        }
        
        // Restauramos la apariencia y la interacción del icono original.
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
    
    [Header("Apariencia")]
    public Sprite availableSprite;
    public Sprite placedSprite;
    private bool isPlaced = false;
}