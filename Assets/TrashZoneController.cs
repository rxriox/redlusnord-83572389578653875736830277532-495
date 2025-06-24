using UnityEngine;
using UnityEngine.EventSystems;

public class TrashZoneController : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        ActiveArtifactIcon activeArtifact = eventData.pointerDrag.GetComponent<ActiveArtifactIcon>();
        if (activeArtifact != null)
        {
            if (PlacementUIManager.Instance != null)
            {
                PlacementUIManager.Instance.ShowBenchesAfterDrag();
            }
            ArtifactManager.Instance.RemoveArtifact(activeArtifact);
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.HideTrashZone();
            }
        }
    }
}