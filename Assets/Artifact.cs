using UnityEngine;

[CreateAssetMenu(fileName = "New Artifact", menuName = "Autobattler/Artifact")]
public class Artifact : ScriptableObject
{
    public string artifactName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;
    
    // Aquí podrías añadir variables para los efectos del artefacto
    // public float damageBonus;
    // public int healthBonus;
}