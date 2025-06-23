using UnityEngine;
using UnityEngine.EventSystems;

public class ArtifactDropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // Obtenemos la información del artefacto que se está arrastrando desde el manager.
        Artifact droppedArtifact = TabGroupManager.Instance.draggedArtifact;

        if (droppedArtifact != null)
        {
            Debug.Log($"El artefacto '{droppedArtifact.artifactName}' se ha soltado sobre '{gameObject.name}'!");
            
            // --- AQUÍ VA LA LÓGICA DE QUÉ HACER CON EL ARTEFACTO ---
            // Por ejemplo:
            // ApplyArtifactEffect(droppedArtifact);
        }
    }
}