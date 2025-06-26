using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class RoundUIManager : MonoBehaviour
{
    [Tooltip("Arrastra aquí el componente Dropdown - TextMeshPro desde tu Canvas.")]
    public TMP_Dropdown roundDropdown;

    [Tooltip("Arrastra aquí todos los ScriptableObjects de RoundSettings que crees.")]
    public List<RoundSettings> roundSettingsList;

    void Start()
    {
        if (roundDropdown == null)
        {
            Debug.LogError("El Dropdown de Rondas no está asignado en el inspector de RoundUIManager.");
            return;
        }

        PopulateDropdown();
        
        roundDropdown.onValueChanged.AddListener(delegate {
            OnDropdownValueChanged(roundDropdown);
        });

        if (roundSettingsList != null && roundSettingsList.Count > 0)
        {
            ApplySettingsForRound(0);
        }
    }
    void PopulateDropdown()
    {
        roundDropdown.ClearOptions();
        if (roundSettingsList != null && roundSettingsList.Count > 0)
        {
            List<string> options = roundSettingsList.Select(settings => settings.roundName).ToList();
            roundDropdown.AddOptions(options);
        }
    }

    void OnDropdownValueChanged(TMP_Dropdown change)
    {
        ApplySettingsForRound(change.value);
    }
    
    void ApplySettingsForRound(int index)
    {
        if (roundSettingsList != null && index >= 0 && index < roundSettingsList.Count)
        {
            RoundSettings selectedSettings = roundSettingsList[index];
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ApplyRoundSettings(selectedSettings);
            }
            else
            {
                Debug.LogError("No se encuentra una instancia de GameManager en la escena.");
            }
        }
    }
}
