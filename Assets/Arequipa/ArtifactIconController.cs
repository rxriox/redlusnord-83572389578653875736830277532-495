using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class ArtifactIconController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Artifact artifactData;

    private Image iconImage;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 startPosition;

    void Awake()
    {
        iconImage = GetComponent<Image>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        UpdateIconVisual(0);
    }

    public void SetAsPlaced(int teamID)
    {
        isPlacedByTeam[teamID] = true;
        UpdateIconVisual(teamID);
    }
    public void ResetIcon(int teamID)
    {
        isPlacedByTeam[teamID] = false;
        UpdateIconVisual(teamID);
    }
    public void ResetForAllTeams()
    {
        isPlacedByTeam[0] = false;
        isPlacedByTeam[1] = false;
        UpdateIconVisual(0); // Vuelve a la perspectiva del jugador por defecto
    }

    public void UpdateIconVisual(int teamID)
    {
        bool isPlaced = isPlacedByTeam.ContainsKey(teamID) && isPlacedByTeam[teamID];
        if (iconImage != null)
        {
            iconImage.sprite = isPlaced ? placedSprite : availableSprite;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (artifactData == null) return;
        
        // ===== MODIFICACIÓN #3: Comprueba el estado del equipo actual =====
        int currentTeamID = UIPerspectiveManager.Instance.GetCurrentTeamPerspective();
        if (isPlacedByTeam[currentTeamID])
        {
            eventData.pointerDrag = null; // Cancela el arrastre si ya fue colocado por este equipo
            return;
        }
        // ===== FIN DE LA MODIFICACIÓN =====

        TabGroupManager.Instance.OnArtifactDragStart(this);
        canvasGroup.alpha = 0.4f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData){}

    public void OnEndDrag(PointerEventData eventData)
    {
        if (TabGroupManager.Instance != null)
        {
            TabGroupManager.Instance.OnArtifactDragEnd();
        }
        
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
    
    [Header("Apariencia")]
    public Sprite availableSprite;
    public Sprite placedSprite;
    private Dictionary<int, bool> isPlacedByTeam = new Dictionary<int, bool> { {0, false}, {1, false} };
}