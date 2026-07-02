using UnityEngine;
using System.Collections.Generic;

public class NPCMemory : MonoBehaviour
{
    [Header("Stato Corrente (Debug)")]
    [SerializeField] private string _currentCostume = "";
    [SerializeField] private int _currentTrust = 50;
    
    // Trust separato per ogni costume (key = costume gameObjectName o "Base")
    private Dictionary<string, int> _trustByCostume = new Dictionary<string, int>();
    
    // Riassunti separati per ogni costume - l'NPC NON sa che sono la stessa persona!
    private Dictionary<string, string> _summaryByCostume = new Dictionary<string, string>();
    
    // Chat history temporanea per la sessione corrente
    // Viene cancellata e sostituita dal riassunto alla fine della conversazione
    [HideInInspector] 
    public List<OllamaMessage> chatHistory = new List<OllamaMessage>();
    
    // Proprietà per accedere al costume corrente
    public string CurrentCostume => _currentCostume;

    /// <summary>
    /// Ottiene il trust score per il costume attuale.
    /// </summary>
    public int trustScore
    {
        get => GetTrustForCurrentCostume();
        set => SetTrustForCurrentCostume(value);
    }
    
    /// <summary>
    /// Imposta quale costume sta indossando il giocatore (chiamato all'inizio del dialogo).
    /// </summary>
    public void SetCurrentCostume(string costumeName)
    {
        _currentCostume = costumeName;
        _currentTrust = GetTrustForCostume(costumeName);
        Debug.Log($"[NPCMemory] Costume impostato: {costumeName}, Trust: {_currentTrust}");
    }
    
    private int GetTrustForCurrentCostume()
    {
        return GetTrustForCostume(_currentCostume);
    }
    
    private int GetTrustForCostume(string costumeName)
    {
        if (string.IsNullOrEmpty(costumeName))
            costumeName = "Base";
            
        if (_trustByCostume.TryGetValue(costumeName, out int trust))
        {
            return trust;
        }
        
        // Default: 50 per costume mai visto
        return 50;
    }
    
    private void SetTrustForCurrentCostume(int value)
    {
        string key = string.IsNullOrEmpty(_currentCostume) ? "Base" : _currentCostume;
        _trustByCostume[key] = Mathf.Clamp(value, 0, 100);
        _currentTrust = _trustByCostume[key];
    }

    public void AddMessage(string role, string content)
    {
        chatHistory.Add(new OllamaMessage(role, content));
    }

    public void UpdateTrust(int amount)
    {
        trustScore += amount;
        Debug.Log($"[NPCMemory] Trust aggiornato per '{_currentCostume}': {trustScore}");
    }
    
    // ==================== SISTEMA RIASSUNTI DUALE ====================
    // 1. Per NPC (LLM): riassunti separati per costume - risparmia token
    // 2. Per Player (Taccuino): riassunto globale unico - accumula tutte le info
    
    // Riassunto globale per il taccuino del player
    private string _globalSummary = "";
    
    // --- METODI PER L'NPC (usa riassunti per costume) ---
    
    /// <summary>
    /// Salva il riassunto per il costume corrente (per l'NPC).
    /// Usato quando riprendi la conversazione con lo stesso costume.
    /// </summary>
    public void SetSummaryForCurrentCostume(string summary)
    {
        string key = string.IsNullOrEmpty(_currentCostume) ? "Base" : _currentCostume;
        _summaryByCostume[key] = summary;
        Debug.Log($"[NPCMemory] Riassunto costume '{key}' salvato: {summary}");
    }
    
    /// <summary>
    /// Ottiene il riassunto per il costume corrente.
    /// L'NPC usa questo per "ricordarsi" della conversazione precedente.
    /// </summary>
    public string GetSummaryForCurrentCostume()
    {
        string key = string.IsNullOrEmpty(_currentCostume) ? "Base" : _currentCostume;
        
        if (_summaryByCostume.TryGetValue(key, out string summary))
        {
            return summary;
        }
        
        return null; // Primo incontro con questo costume
    }
    
    /// <summary>
    /// Verifica se abbiamo già parlato con questo costume.
    /// </summary>
    public bool HasMetWithCurrentCostume()
    {
        string key = string.IsNullOrEmpty(_currentCostume) ? "Base" : _currentCostume;
        return _summaryByCostume.ContainsKey(key);
    }
    
    // --- METODI PER IL TACCUINO DEL PLAYER (usa riassunto globale) ---
    
    /// <summary>
    /// Aggiunge nuove info al riassunto globale (per il taccuino).
    /// Le info vengono accumulate da tutte le conversazioni.
    /// </summary>
    public void AppendToGlobalSummary(string newInfo)
    {
        if (string.IsNullOrWhiteSpace(newInfo)) return;
        
        string cleanInfo = newInfo.Trim();
        // Check fuzzy matching for "No info"
        // Also trim trailing punctuation to catch "Nessuna informazione rilevante." vs "Nessuna informazione rilevante"
        string checkString = cleanInfo.TrimEnd('.', ' ', '\n', '\r');
        
        if (checkString.Equals("Nessuna informazione rilevante", System.StringComparison.OrdinalIgnoreCase))
            return;
            
        if (!string.IsNullOrEmpty(_globalSummary))
        {
            _globalSummary += " " + cleanInfo;
        }
        else
        {
            _globalSummary = cleanInfo;
        }
        
        Debug.Log($"[NPCMemory] Taccuino aggiornato: {_globalSummary}");
    }
    
    /// <summary>
    /// Ottiene il riassunto globale per il taccuino del player.
    /// </summary>
    public string GetGlobalSummary()
    {
        return string.IsNullOrEmpty(_globalSummary) ? "Nessuna informazione raccolta." : _globalSummary;
    }
    
    /// <summary>
    /// Verifica se abbiamo raccolto informazioni su questo NPC.
    /// </summary>
    public bool HasAnyInfo()
    {
        return !string.IsNullOrEmpty(_globalSummary);
    }
    
    public Dictionary<string, string> GetAllSummaries()
    {
        return new Dictionary<string, string>(_summaryByCostume);
    }
    
    /// <summary>
    /// Cancella la chat history (chiamato dopo aver generato il riassunto).
    /// </summary>
    public void ClearChatHistory()
    {
        chatHistory.Clear();
    }
    
    /// <summary>
    /// Restituisce il testo descrittivo del livello di fiducia attuale.
    /// </summary>
    public string GetTrustLevelDescription()
    {
        int score = trustScore;
        
        if (score <= 20) return $"{score}/100 - Ostile";
        if (score <= 40) return $"{score}/100 - Sospettoso";
        if (score <= 60) return $"{score}/100 - Neutrale";
        if (score <= 80) return $"{score}/100 - Amichevole";
        return $"{score}/100 - Fiducia totale";
    }
}