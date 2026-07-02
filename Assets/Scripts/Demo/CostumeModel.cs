using UnityEngine;

/// <summary>
/// Componente marker per identificare i modelli costume.
/// Attaccare a ogni GameObject che rappresenta un modello indossabile.
/// Questo permette a CostumeManager di trovare i modelli in modo robusto,
/// senza dipendere dai nomi degli oggetti.
/// </summary>
[DisallowMultipleComponent]
public class CostumeModel : MonoBehaviour
{
    [Tooltip("ID univoco del costume (deve corrispondere a CostumeData.gameObjectName oppure essere 'Base').")]
    public string costumeID = "";
    
    [Tooltip("Se true, questo è il modello base (investigatore).")]
    public bool isBaseModel = false;

    private void OnValidate()
    {
        // Auto-imposta l'ID se vuoto
        if (string.IsNullOrEmpty(costumeID))
        {
            costumeID = gameObject.name;
        }
    }

    private void Reset()
    {
        // Quando aggiungi il componente, usa il nome dell'oggetto come ID
        costumeID = gameObject.name;
    }
}
