using UnityEngine;

public enum ArtifactCategory { Categoria1, Categoria2, Categoria3, Categoria4 }

[CreateAssetMenu(fileName = "New Artifact", menuName = "Autobattler/Artifact")]
public class Artifact : ScriptableObject
{
    [Tooltip("La categoría a la que pertenece este artefacto.")]
    public ArtifactCategory category;
    public string artifactName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;

    //MODIFICADORES

    [Header("Efectos del Artefacto")]
    public int healthBonus;
    public int attackDamageBonus = 0;
    public float moveSpeedBonus = 0f;
    public float attackSpeedBonus = 0f;
    
}