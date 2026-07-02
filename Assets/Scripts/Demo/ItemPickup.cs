using UnityEngine;

/// <summary>
/// Oggetto raccoglibile generico che aggiunge un item all'inventario
/// e registra una nota nel taccuino del detective.
/// Implementa IInteractable per funzionare con il sistema di interazione esistente.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Configurazione Item")]
    [Tooltip("Nome univoco dell'oggetto (usato come ID nell'inventario).")]
    public string itemID = "item_01";
    
    [Tooltip("Nome visualizzato al giocatore.")]
    public string displayName = "Oggetto Misterioso";
    
    [TextArea(2, 5)]
    [Tooltip("Testo che apparirà nel taccuino quando raccogli l'oggetto.")]
    public string additionalDescription = "";
    
    [Header("Impostazioni Taccuino")]
    [Tooltip("ID della sezione nel taccuino dove aggiungere la nota.")]
    public string notebookSectionID = "Oggetti trovati";
    
    [Tooltip("Icona da mostrare nel taccuino (opzionale).")]
    public Sprite notebookIcon;
    
    [Header("Feedback")]
    [Tooltip("Se true, distrugge l'oggetto dopo il pickup.")]
    public bool destroyOnPickup = true;
    
    [Tooltip("Suono da riprodurre al pickup (opzionale).")]
    public AudioClip pickupSound;
    
    [Tooltip("Messaggio mostrato al momento della raccolta.")]
    public string pickupMessage = "Hai raccolto: {0}";
    
    // Stato interno
    private bool _alreadyPickedUp = false;

    private void Start()
    {
        // Assicurati che il Collider sia impostato come Trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"[ItemPickup] '{name}': Il Collider dovrebbe essere isTrigger = true per il sistema di interazione!");
        }
    }

    /// <summary>
    /// Chiamato quando il player preme E vicino all'oggetto.
    /// </summary>
    public void Interact(PlayerController player)
    {
        if (_alreadyPickedUp) return;
        
        // 1. AGGIUNGI ALL'INVENTARIO
        if (Managers.Inventory != null)
        {
            // Controlla se già posseduto
            if (Managers.Inventory.HasItem(itemID))
            {
                Debug.Log($"[ItemPickup] '{displayName}' già nell'inventario.");
                ShowPickupMessage($"Possiedi già: {displayName}");
                return;
            }
            
            Managers.Inventory.AddItem(itemID);
        }
        else
        {
            Debug.LogWarning("[ItemPickup] InventoryManager non trovato in Managers!");
        }
        
        // 2. REGISTRA NEL TACCUINO
        AddToNotebook();
        
        // 3. FEEDBACK AUDIO
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // 4. MOSTRA MESSAGGIO DI RACCOLTA
        string message = string.Format(pickupMessage, displayName);
        ShowPickupMessage(message);
        
        Debug.Log($"[ItemPickup] Raccolto: {displayName} (ID: {itemID})");
        
        // 5. MARCA COME RACCOLTO E RIMUOVI
        _alreadyPickedUp = true;
        
        // Nascondi il prompt "Premi E"
        if (InteractionUI.Instance != null)
        {
            InteractionUI.Instance.Hide();
        }
        
        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Aggiunge o aggiorna la nota nel taccuino del detective.
    /// </summary>
    private void AddToNotebook()
    {
        // Trova il NotebookManager nella scena
        NotebookManager notebook = FindFirstObjectByType<NotebookManager>();
        
        if (notebook == null)
        {
            Debug.LogWarning("[ItemPickup] NotebookManager non trovato nella scena!");
            return;
        }
        
        // Usa direttamente il testo scritto in Additional Description
        if (string.IsNullOrEmpty(additionalDescription))
        {
            Debug.LogWarning($"[ItemPickup] '{displayName}' non ha una descrizione per il taccuino!");
            return;
        }
        
        string noteText = additionalDescription;
        
        // Cerca se esiste già una sezione per gli oggetti
        NotebookManager.NotebookEntry existingEntry = notebook.entries.Find(e => e.npcID == notebookSectionID);
        
        if (existingEntry != null)
        {
            // Aggiungi alla nota esistente
            string separator = string.IsNullOrEmpty(existingEntry.currentNote) ? "" : "\n\n";
            existingEntry.currentNote += $"{separator}• {noteText}";
            
            // Aggiorna la UI se il taccuino è aperto
            notebook.UpdateEntry(notebookSectionID, existingEntry.currentNote);
        }
        else
        {
            // Crea una nuova entry per gli oggetti
            NotebookManager.NotebookEntry newEntry = new NotebookManager.NotebookEntry
            {
                npcID = notebookSectionID,
                portrait = notebookIcon,
                currentNote = $"• {noteText}"
            };
            
            notebook.entries.Add(newEntry);
        }
        
        Debug.Log($"[ItemPickup] Aggiunto al taccuino: {noteText}");
    }
    
    /// <summary>
    /// Mostra un messaggio temporaneo al giocatore.
    /// Usa DialogueUI.ShowSimpleDialogue se disponibile, altrimenti solo log.
    /// </summary>
    private void ShowPickupMessage(string message)
    {
        // NUOVO SISTEMA: Usa il Manager, non la UI diretta
        if (Managers.Dialogue != null)
        {
            // Nota: Qui usiamo StartSimpleDialogue perché ShowPickupPopup non è esposto nel Manager.
            // Se vuoi mantenere lo stile popup specifico, potremmo dover aggiungere un metodo al Manager,
            // ma per ora usiamo il dialogo semplice che è già supportato.
            Managers.Dialogue.StartPickupDialogue(message, null);
        }
        else
        {
            // Fallback: solo log
            Debug.Log($"[PICKUP] {message}");
        }
    }
    
    // Visualizzazione in Editor per debug
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, displayName);
        #endif
    }
}
