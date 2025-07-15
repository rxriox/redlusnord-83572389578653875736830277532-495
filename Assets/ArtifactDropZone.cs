using UnityEngine;
using UnityEngine.EventSystems;

public class ArtifactDropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        Artifact droppedArtifact = TabGroupManager.Instance.draggedArtifact;
        if (droppedArtifact == null)
        {
            TabGroupManager.Instance.SetArtifactDropSuccessful();
            return;
        }

        ArtifactIconController benchIcon = eventData.pointerDrag.GetComponent<ArtifactIconController>();
        if (benchIcon != null)
        {
            // --- CAMBIO CLAVE ---
            // Simplemente le pedimos al ArtifactManager que coloque el artefacto.
            // El manager se encargará de todo lo demás, incluyendo la actualización de la UI.
            ArtifactManager.Instance.PlaceArtifact(benchIcon);
            TabGroupManager.Instance.SetArtifactDropSuccessful();

            Debug.Log($"El artefacto '{droppedArtifact.artifactName}' se ha soltado sobre '{gameObject.name}'!");
        }
    }
}