using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActiveArtifactIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public ArtifactIconController originatingBenchIcon;
    
    private Image iconImage;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 startPosition;

    void Awake()
    {
        iconImage = GetComponent<Image>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }
    public void Initialize(ArtifactIconController benchIcon)
    {
        this.originatingBenchIcon = benchIcon;
        if (iconImage != null && benchIcon.artifactData != null)
        {
            iconImage.sprite = benchIcon.artifactData.icon;
        }
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.ShowTrashZone();
        }

        if (PlacementUIManager.Instance != null)
        {
            PlacementUIManager.Instance.HideBenchesForDrag();
        }
        canvasGroup.blocksRaycasts = false;
        originalParent = transform.parent;
        startPosition = transform.position;
        transform.SetParent(transform.root);
    }


    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.HideTrashZone();
        }

        if (PlacementUIManager.Instance != null)
        {
            PlacementUIManager.Instance.ShowBenchesAfterDrag();
        }
        
        canvasGroup.blocksRaycasts = true;
        transform.SetParent(originalParent);
        transform.position = startPosition;
    }
}