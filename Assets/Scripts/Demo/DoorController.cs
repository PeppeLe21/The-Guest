using UnityEngine;

/// <summary>
/// Controller per le porte.
/// Gestisce apertura/chiusura e integrazione con DialogueManager.
/// </summary>
public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Impostazioni Porta")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float smoothSpeed = 2f;
    
    [Header("Game Logic")]
    [SerializeField] private bool isLocked = false;
    
    [Tooltip("Se TRUE, bussare attiva il dialogo con l'NPC assegnato.")]
    [SerializeField] private bool isPlotDoor = false;
    
    [Header("Plot Door - NPC")]
    [Tooltip("L'NPC dietro questa porta. Trascinalo dalla scena.")]
    [SerializeField] private GameObject assignedNPC;

    // Stato
    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion targetRotation;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        Messenger.AddListener(GameEvent.DOOR_UNLOCK, OnDoorUnlock);
    }

    private void OnDestroy()
    {
        Messenger.RemoveListener(GameEvent.DOOR_UNLOCK, OnDoorUnlock);
    }

    private void Start()
    {
        isOpen = false;
        closedRotation = transform.localRotation;
        targetRotation = closedRotation;
    }

    private void Update()
    {
        // Animazione fluida della porta
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smoothSpeed);
    }

    private void OnDoorUnlock()
    {
        isLocked = false;
        Debug.Log("🚪 Porta Sbloccata!");
    }

    /// <summary>
    /// Chiamato quando il player interagisce con la porta
    /// </summary>
    public void Interact(PlayerController player)
    {
        if (isLocked)
        {
            Debug.Log("🔒 La porta è chiusa a chiave!");
            return;
        }

        if (isPlotDoor)
        {
            HandlePlotDoorInteraction();
            return;
        }

        // Porta normale: apri/chiudi
        ToggleDoor();
    }

    /// <summary>
    /// Gestisce l'interazione con una porta di trama (NPC)
    /// </summary>
    private void HandlePlotDoorInteraction()
    {
        if (isOpen)
        {
            // Se la porta è già aperta, permettiamo di chiuderla normalmente
            ToggleDoor();
            return;
        }

        Debug.Log("✊ Toc Toc...");

        if (assignedNPC == null)
        {
            Debug.LogError($"[DoorController] {name}: Nessun NPC assegnato! Trascina l'NPC nel campo 'Assigned NPC'.");
            return;
        }

        // Usa il DialogueManager centrale
        if (Managers.Dialogue != null)
        {
            Managers.Dialogue.StartDialogue(assignedNPC, openDoorFirst: true, doorController: this);
        }
        else
        {
            Debug.LogError("[DoorController] DialogueManager non trovato!");
        }
    }

    /// <summary>
    /// Apre/chiude la porta (toggle)
    /// </summary>
    public void ToggleDoor()
    {
        isOpen = !isOpen;
        UpdateRotation();
        Debug.Log(isOpen ? "🚪 Porta Aperta" : "🚪 Porta Chiusa");
    }

    /// <summary>
    /// Forza l'apertura della porta (chiamato da DialogueManager)
    /// </summary>
    public void ForceOpen()
    {
        if (isOpen) return;
        
        isOpen = true;
        UpdateRotation();
        Debug.Log("🚪 Porta Aperta (Forzata)");
    }

    /// <summary>
    /// Forza la chiusura della porta
    /// </summary>
    public void ForceClose()
    {
        if (!isOpen) return;
        
        isOpen = false;
        UpdateRotation();
        Debug.Log("🚪 Porta Chiusa (Forzata)");
    }

    private void UpdateRotation()
    {
        if (isOpen)
        {
            targetRotation = Quaternion.Euler(
                transform.localEulerAngles.x,
                closedRotation.eulerAngles.y + openAngle,
                transform.localEulerAngles.z
            );
        }
        else
        {
            targetRotation = closedRotation;
        }
    }
}