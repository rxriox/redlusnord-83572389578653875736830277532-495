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
        canvasGroup = gameObject.AddComponent<CanvasGroup>(); // Añadimos CanvasGroup para el arrastre
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (artifactData == null) return;
        
        // Notificamos al TabGroupManager que un arrastre ha comenzado.
        TabGroupManager.Instance.OnArtifactDragStart(artifactData);
        
        // Hacemos que el icono se pueda arrastrar por toda la pantalla
        // y que no bloquee los eventos del ratón.
        originalParent = transform.parent;
        startPosition = transform.position;
        transform.SetParent(transform.root); // Lo movemos a la raíz del Canvas
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // El icono sigue al puntero (ratón o dedo)
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Notificamos al manager que el arrastre ha terminado.
        TabGroupManager.Instance.OnArtifactDragEnd();

        // Devolvemos el icono a su posición y estado original.
        transform.SetParent(originalParent);
        transform.position = startPosition;
        canvasGroup.blocksRaycasts = true;
    }
}