using UnityEngine;
using UnityEngine.EventSystems;

public class TrashZoneController : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Combat)
        {
            Debug.LogWarning("No se puede eliminar un artefacto en la papelera durante el combate.");
            return;
        }

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