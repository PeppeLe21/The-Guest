using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestisce i costumi del giocatore: sblocco, equipaggiamento, switch visivo.
/// Attaccare al GameObject del Player.
/// </summary>
public class CostumeManager : MonoBehaviour
{
    [Header("Modello Base (Investigatore)")]
    [Tooltip("Nome del GameObject del modello base (sempre disponibile).")]
    public string baseModelName = "Body";
    
    [TextArea(3, 5)]
    [Tooltip("Descrizione LLM per il modello base.")]
    public string baseLLMDescription = "L'interlocutore ha un aspetto sospetto: indossa un lungo cappello a cilindro nero, una camicia bianca con bretelle marroni e ha dei baffi curati. Sembra un investigatore privato o qualcuno che fa troppe domande. La gente del quartiere diffida di chi ficca il naso negli affari altrui.";
    
    [Header("Costumi Speciali")]
    [Tooltip("Lista dei costumi sbloccabili (escluso il base).")]
    public List<CostumeData> allCostumes = new List<CostumeData>();
    
    [Header("Runtime State (Debug)")]
    [SerializeField] private CostumeData _currentCostume;
    [SerializeField] private List<CostumeData> _unlockedCostumes = new List<CostumeData>();
    [SerializeField] private bool _isWearingBase = true;
    
    // Cache dei GameObject dei modelli
    private Dictionary<string, GameObject> _costumeModels = new Dictionary<string, GameObject>();
    
    // Proprietà pubbliche
    public CostumeData CurrentCostume => _currentCostume;
    public List<CostumeData> UnlockedCostumes => _unlockedCostumes;
    public bool IsWearingBase => _isWearingBase;
    
    /// <summary>
    /// Restituisce la descrizione LLM del costume attuale.
    /// </summary>
    public string GetCurrentCostumeLLMDescription()
    {
        // Se indossa il modello base, usa la descrizione base
        if (_isWearingBase)
        {
            return baseLLMDescription;
        }
        
        // Altrimenti usa la descrizione del costume
        if (_currentCostume != null && !string.IsNullOrEmpty(_currentCostume.llmDescription))
        {
            return _currentCostume.llmDescription;
        }
        
        return "Indossa abiti civili ordinari.";
    }

    private void Start()
    {
        // 1. Scansiona e mappa tutti i modelli figli
        ScanCostumeModels();
        
        // 2. Sblocca i costumi marcati come default
        foreach (var costume in allCostumes)
        {
            if (costume != null && costume.unlockedByDefault)
            {
                UnlockCostume(costume, silent: true);
            }
        }
        
        // 3. Equipaggia il modello base automaticamente
        EquipBase();
    }

    private void Update()
    {
        // // TAB per cambiare costume (solo fuori dal dialogo)
        // if (Input.GetKeyDown(KeyCode.Tab) && !DialogueUI.IsDialogueOpen)
        // {
        //     CycleToNextCostume();
        // }
    }

    /// <summary>
    /// Scansiona i figli del Player per trovare i modelli dei costumi.
    /// Supporta due modalità:
    /// 1. NUOVO: Cerca componenti CostumeModel (più robusto)
    /// 2. LEGACY: Fallback ai nomi degli oggetti (retrocompatibilità)
    /// </summary>
    private void ScanCostumeModels()
    {
        _costumeModels.Clear();

        // Prima prova: cerca componenti CostumeModel (metodo robusto)
        CostumeModel[] markers = GetComponentsInChildren<CostumeModel>(true);
        
        if (markers.Length > 0)
        {
            ScanWithMarkers(markers);
        }
        else
        {
            // Fallback: usa il vecchio sistema basato sui nomi
            Debug.LogWarning("[CostumeManager] Nessun CostumeModel trovato. Uso metodo legacy (nomi oggetti).");
            ScanLegacy();
        }
    }

    /// <summary>
    /// Sblocca un costume (lo aggiunge all'inventario).
    /// </summary>
    public void UnlockCostume(CostumeData costume, bool silent = false)
    {
        if (costume == null) return;
        
        if (!_unlockedCostumes.Contains(costume))
        {
            _unlockedCostumes.Add(costume);
            
            if (!silent)
            {
                Debug.Log($"[CostumeManager] Sbloccato: {costume.displayName}!");
                // Qui puoi aggiungere: suono, popup UI, ecc.
                Messenger.Broadcast(GameEvent.COSTUME_UNLOCKED);
            }
        }
    }

    /// <summary>
    /// Equipaggia un costume (cambia il modello visivo).
    /// </summary>
    public bool EquipCostume(CostumeData costume)
    {
        if (costume == null)
        {
            Debug.LogWarning("[CostumeManager] Tentativo di equipaggiare costume null!");
            return false;
        }
        
        // Verifica che sia sbloccato
        if (!_unlockedCostumes.Contains(costume))
        {
            Debug.LogWarning($"[CostumeManager] Costume '{costume.displayName}' non sbloccato!");
            return false;
        }
        
        // Verifica che esista il modello
        if (!_costumeModels.TryGetValue(costume.gameObjectName, out GameObject model))
        {
            Debug.LogError($"[CostumeManager] Modello '{costume.gameObjectName}' non trovato tra i figli del Player!");
            return false;
        }
        
        // Spegni il costume attuale (o il base)
        if (_isWearingBase && _costumeModels.TryGetValue(baseModelName, out GameObject baseModel))
        {
            baseModel.SetActive(false);
        }
        else if (_currentCostume != null && _costumeModels.TryGetValue(_currentCostume.gameObjectName, out GameObject oldModel))
        {
            oldModel.SetActive(false);
        }
        
        // Accendi il nuovo costume
        model.SetActive(true);
        _currentCostume = costume;
        _isWearingBase = false;
        
        // Ogni costume ha il proprio Animator - aggiornalo nel PlayerController
        Animator costumeAnimator = model.GetComponent<Animator>();
        if (costumeAnimator == null) costumeAnimator = model.GetComponentInChildren<Animator>();
        
        PlayerController playerCtrl = GetComponent<PlayerController>();
        if (playerCtrl != null && costumeAnimator != null)
        {
            playerCtrl.UpdateAnimator(costumeAnimator);
            Debug.Log($"[CostumeManager] Animator aggiornato: {costumeAnimator.name}");
        }
        
        Debug.Log($"[CostumeManager] Equipaggiato: {costume.displayName}");
        
        // Evento per altri sistemi (UI, ecc.)
        Messenger.Broadcast(GameEvent.COSTUME_CHANGED);
        
        return true;
    }

    /// <summary>
    /// Equipaggia il modello base (investigatore).
    /// </summary>
    public void EquipBase()
    {
        // Spegni il costume attuale se presente
        if (_currentCostume != null && _costumeModels.TryGetValue(_currentCostume.gameObjectName, out GameObject oldModel))
        {
            oldModel.SetActive(false);
        }
        
        // Accendi il modello base
        if (_costumeModels.TryGetValue(baseModelName, out GameObject baseModel))
        {
            baseModel.SetActive(true);
            _isWearingBase = true;
            _currentCostume = null;
            
            // --- MODIFICA QUI ---
            
            // 1. Prova a cercare l'Animator sul figlio "Body"
            Animator newAnimator = baseModel.GetComponent<Animator>();
            
            // 2. SE NON C'È (perché è il modello base spezzato), prendi quello su MainCharacter (questo oggetto)
            if (newAnimator == null) 
            {
                newAnimator = GetComponent<Animator>(); 
            }
            
            // --------------------

            if (newAnimator == null)
            {
                Debug.LogWarning($"[CostumeManager] ATTENZIONE: Nessun Animator trovato né su '{baseModelName}' né sul Root!");
            }
            
            PlayerController playerCtrl = GetComponent<PlayerController>();
            if (playerCtrl != null && newAnimator != null)
            {
                // Forza il Rebind per svegliare l'animator principale
                newAnimator.Rebind();
                newAnimator.Update(0f);
                
                playerCtrl.UpdateAnimator(newAnimator);
                Debug.Log($"[CostumeManager] Animator base aggiornato: {newAnimator.name}");
            }
            
            Debug.Log($"[CostumeManager] Equipaggiato modello base: {baseModelName}");
            // Messenger.Broadcast(GameEvent.COSTUME_CHANGED); // Se usi i messaggi, scommenta
        }
        else
        {
            Debug.LogError($"[CostumeManager] Modello base '{baseModelName}' non trovato!");
        }
    }

    /// <summary>
    /// Equipaggia un costume per nome (utility).
    /// </summary>
    public bool EquipCostumeByName(string gameObjectName)
    {
        // Se è il modello base
        if (gameObjectName == baseModelName)
        {
            EquipBase();
            return true;
        }
        
        var costume = allCostumes.Find(c => c.gameObjectName == gameObjectName);
        if (costume != null)
        {
            return EquipCostume(costume);
        }
        Debug.LogWarning($"[CostumeManager] Costume con nome '{gameObjectName}' non trovato nella lista!");
        return false;
    }

    /// <summary>
    /// Scansione tramite componenti CostumeModel (metodo preferito).
    /// </summary>
    private void ScanWithMarkers(CostumeModel[] markers)
    {
        foreach (var marker in markers)
        {
            string id = marker.costumeID;
            
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[CostumeManager] CostumeModel su '{marker.name}' ha ID vuoto!");
                continue;
            }

            if (_costumeModels.ContainsKey(id))
            {
                Debug.LogWarning($"[CostumeManager] ID duplicato: '{id}' su '{marker.name}'");
                continue;
            }

            _costumeModels[id] = marker.gameObject;
            marker.gameObject.SetActive(false);
            
            // Se è marcato come base, aggiorna il nome
            if (marker.isBaseModel)
            {
                baseModelName = id;
            }

            Debug.Log($"[CostumeManager] Trovato modello (marker): {id}");
        }
    }

    /// <summary>
    /// Scansione legacy basata sui nomi (retrocompatibilità).
    /// </summary>
    private void ScanLegacy()
    {
        // Lista di prefissi/contenuti da ignorare
        string[] ignorePatterns = { "Interaction", "Camera", "Point", "Target", "Armature", "Collider" };
        
        foreach (Transform child in transform)
        {
            bool shouldIgnore = false;
            
            foreach (string pattern in ignorePatterns)
            {
                if (child.name.Contains(pattern))
                {
                    shouldIgnore = true;
                    break;
                }
            }
            
            if (shouldIgnore) continue;

            _costumeModels[child.name] = child.gameObject;
            child.gameObject.SetActive(false);

            Debug.Log($"[CostumeManager] Trovato modello (legacy): {child.name}");
        }
    }

    /// <summary>
    /// Passa al costume successivo (Base → Costume1 → Costume2 → ... → Base).
    /// </summary>
    public void CycleToNextCostume()
    {
        if (_unlockedCostumes.Count == 0)
        {
            // Solo il base disponibile, non fare nulla
            return;
        }
        
        if (_isWearingBase)
        {
            // Dal base vai al primo costume sbloccato
            EquipCostume(_unlockedCostumes[0]);
        }
        else
        {
            // Trova il prossimo costume o torna al base
            int currentIndex = _unlockedCostumes.IndexOf(_currentCostume);
            int nextIndex = currentIndex + 1;
            
            if (nextIndex >= _unlockedCostumes.Count)
            {
                // Torna al base
                EquipBase();
            }
            else
            {
                EquipCostume(_unlockedCostumes[nextIndex]);
            }
        }
    }

    /// <summary>
    /// Verifica se un costume è sbloccato.
    /// </summary>
    public bool IsCostumeUnlocked(CostumeData costume)
    {
        return _unlockedCostumes.Contains(costume);
    }
}
