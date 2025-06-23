using UnityEngine;
using UnityEngine.EventSystems;

public class ArtifactDropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        Artifact droppedArtifact = TabGroupManager.Instance.draggedArtifact;
        ArtifactIconController benchIcon = eventData.pointerDrag.GetComponent<ArtifactIconController>();
        if (benchIcon != null)
        {
            ArtifactManager.Instance.PlaceArtifact(benchIcon);
            TabGroupManager.Instance.SetArtifactDropSuccessful();

            Debug.Log($"El artefacto '{droppedArtifact.artifactName}' se ha soltado sobre '{gameObject.name}'!");
        }
    }
}