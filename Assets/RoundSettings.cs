using UnityEngine;

[CreateAssetMenu(fileName = "New RoundSettings", menuName = "Game/Round Settings")]
public class RoundSettings : ScriptableObject
{
    [Tooltip("El nombre que se mostrará en el dropdown, ej: 'RONDA 1'")]
    public string roundName = "Ronda X";

    [Tooltip("Límite máximo de unidades totales permitidas en el tablero.")]
    public int maxTotalUnits = 5;

    [Tooltip("Límite de unidades de rareza Fabulosa.")]
    public int fabulosaLimit = 5;

    [Tooltip("Límite de unidades de rareza Magnífica.")]
    public int magnificaLimit = 0;

    [Tooltip("Límite de unidades de rareza Suprema.")]
    public int supremaLimit = 0;
}