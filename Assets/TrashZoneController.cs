using UnityEngine;
using UnityEngine.EventSystems;

public class TrashZoneController : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // Comprobamos si lo que se ha soltado es un artefacto activo.
        ActiveArtifactIcon activeArtifact = eventData.pointerDrag.GetComponent<ActiveArtifactIcon>();
        if (activeArtifact != null)
        {
            // --- INICIO DE LA CORRECCIÓN ---
            // Le decimos al PlacementUIManager que muestre las bancas.
            // Es crucial hacerlo aquí, ANTES de que el objeto 'activeArtifact' se destruya.
            if (PlacementUIManager.Instance != null)
            {
                PlacementUIManager.Instance.ShowBenchesAfterDrag();
            }
            // --- FIN DE LA CORRECCIÓN ---

            // Le decimos al manager que elimine este artefacto (esto destruirá el objeto).
            ArtifactManager.Instance.RemoveArtifact(activeArtifact);

            // Le decimos al PlayerController que oculte la papelera (esto se mantiene igual).
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.HideTrashZone();
            }
        }
    }
}