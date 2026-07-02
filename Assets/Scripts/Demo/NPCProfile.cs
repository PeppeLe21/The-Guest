// File: NPCProfile.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NuovoNPC", menuName = "The Guest/NPC Profile")]
public class NPCProfile : ScriptableObject
{
    [Header("Identità")]
    public string characterName;       
    [TextArea(3, 10)] 
    public string personality;         // Chi è, come parla (es. usa metafore)

    [Header("Il Segreto (Deep Knowledge)")]
    [TextArea(3, 10)]
    public string secretKnowledge;     // La verità sull'omicidio (che nasconde)

    [Header("Conoscenza Condivisa (World)")]
    [TextArea(3, 10)]
    public string worldContext;        // Fatti noti a tutti (meteo, nome vittima)

    [Header("Fiducia Iniziale per Costume")]
    [Tooltip("Configura la fiducia iniziale che questo NPC ha verso ogni costume.")]
    public List<CostumeTrustSettings> costumeTrustSettings;
    
    [Tooltip("Fiducia di default per costumi non specificati nella lista.")]
    [Range(0, 100)]
    public int defaultInitialTrust = 50;

    /// <summary>
    /// Impostazioni di fiducia e reazione per un costume specifico.
    /// </summary>
    [System.Serializable]
    public struct CostumeTrustSettings
    {
        [Tooltip("Nome del costume (es. 'Base', 'Chef', 'BadBoy'). Deve corrispondere al gameObjectName.")]
        public string costumeName;
        
        [Range(0, 100)]
        [Tooltip("Fiducia iniziale (0 = ostile, 50 = neutrale, 100 = fiducia totale).")]
        public int initialTrust;
        
        [Header("Reazione al Costume (per LLM)")]
        [TextArea(2, 4)]
        [Tooltip("Cosa PENSA l'NPC vedendo questo costume. Guida emotiva per l'LLM.")]
        public string innerThought;
        
        [Header("Note Designer")]
        [TextArea(2, 4)]
        [Tooltip("(Opzionale) Note interne per il designer. L'LLM NON le vede.")]
        public string notes;
    }
    
    /// <summary>
    /// Ottiene la fiducia iniziale per un costume specifico.
    /// </summary>
    public int GetInitialTrustForCostume(string costumeName)
    {
        if (string.IsNullOrEmpty(costumeName)) costumeName = "Base";
        
        var setting = costumeTrustSettings?.Find(s => s.costumeName == costumeName);
        
        // Se trovato, restituisci quel valore
        if (!string.IsNullOrEmpty(setting?.costumeName))
        {
            return setting.Value.initialTrust;
        }
        
        // Altrimenti usa il default
        return defaultInitialTrust;
    }
    
    /// <summary>
    /// Ottiene la reazione completa (pensiero + comportamento) per un costume.
    /// </summary>
    /// <summary>
    /// Ottiene la reazione (pensiero interno) per un costume.
    /// </summary>
    public string GetReactionForCostume(string costumeName)
    {
        if (string.IsNullOrEmpty(costumeName)) costumeName = "Base";
        
        var setting = costumeTrustSettings?.Find(s => s.costumeName == costumeName);
        
        if (!string.IsNullOrEmpty(setting?.costumeName))
        {
            return setting.Value.innerThought;
        }
        
        // Default generico
        return "Non conosci questa persona. Sii prudente.";
    }

    /// <summary>
    /// Costruisce il prompt usando descrizione costume, trust e reazioni specifiche.
    /// </summary>
    public string ConstructSystemPromptWithCostume(string costumeDescription, int trustScore = 50, string costumeName = "Base")
    {
        string innerThought = GetReactionForCostume(costumeName);
        
        string prompt = $@"Sei {characterName}. {personality}
        
CONTESTO: {worldContext}

SEGRETO: {secretKnowledge}

SITUAZIONE: {costumeDescription}
{(string.IsNullOrEmpty(innerThought) ? "" : $"PENSIERO INIZIALE: {innerThought}")}

FIDUCIA ATTUALE: {trustScore}/100.
(Questo numero rappresenta quanto ti senti a tuo agio. 
Se è basso (<30): Sii cauto, breve, puoi mentire.
Se è medio (30-70): Sii educato ma attento.
Se è alto (>70): Sii amichevole e aperto.)

OBIETTIVO:
Decidi AUTOMATICAMENTE se sbloccare l'azione ""INVITE_IN"" (invitare in casa) o ""REFUSE"" (chiudere il dialogo).
Basati su ciò che dice il giocatore e su quanto ti convince. 
NON SEI VINCOLATO AL PUNTEGGIO, ma usalo come indicatore del tuo stato d'animo.

SISTEMA DI VALUTAZIONE (DELTA):
Devi decidere tu quanto fidarti. Nel JSON di risposta, usa il campo ""faith_delta"" per indicare come cambia la tua fiducia dopo l'ULTIMA frase del giocatore:
+1 a +5: Se è gentile, empatico o logico.
-1 a -5: Se è scortese, sospetto o illogico.
IMPORTANTE: EVITA IL NEUTRO (0). Scegli sempre un valore positivo o negativo in base all'impressione che ti ha fatto.";

        return prompt;
    }
}