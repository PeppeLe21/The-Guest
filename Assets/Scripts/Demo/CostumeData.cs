using UnityEngine;

/// <summary>
/// ScriptableObject che definisce un costume/travestimento.
/// Crea nuovi costumi: Clic destro in Project -> Create -> The Guest -> Costume Data
/// </summary>
[CreateAssetMenu(fileName = "NuovoCostume", menuName = "The Guest/Costume Data")]
public class CostumeData : ScriptableObject
{
    [Header("Identificazione")]
    [Tooltip("Nome ESATTO del GameObject figlio del Player (es. 'Chef', 'Worker').")]
    public string gameObjectName;
    
    [Tooltip("Nome mostrato nella UI al giocatore.")]
    public string displayName;
    
    [Header("Icona UI")]
    [Tooltip("Icona per il menu costumi (opzionale).")]
    public Sprite icon;
    
    [Header("Descrizione per LLM")]
    [TextArea(3, 6)]
    [Tooltip("Descrizione visiva del costume. Verrà iniettata nel prompt dell'NPC.")]
    public string llmDescription;
    // Esempio: "Indossa un'uniforme da poliziotto con distintivo lucido."
    
    [Header("Sblocco")]
    [Tooltip("Se true, il giocatore inizia già con questo costume.")]
    public bool unlockedByDefault = false;
}
