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
        Debug.Log("<color=cyan>ICONO:</color> Arrastre de artefacto iniciado para: " + artifactData.artifactName);
        
        if (artifactData == null) return;
        TabGroupManager.Instance.OnArtifactDragStart(artifactData);
        originalParent = transform.parent;
        if (isPlaced || !ArtifactManager.Instance.CanPlaceArtifact())
        {
            eventData.pointerDrag = null;
            return;
        }
        
        originalParent = transform.parent;
        startPosition = transform.position;
        transform.SetParent(transform.root);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        TabGroupManager.Instance.OnArtifactDragEnd();
        transform.SetParent(originalParent);
        transform.position = startPosition;
        canvasGroup.blocksRaycasts = true;
    }
    
    [Header("Apariencia")]
    public Sprite availableSprite;
    public Sprite placedSprite;
    private bool isPlaced = false;
}