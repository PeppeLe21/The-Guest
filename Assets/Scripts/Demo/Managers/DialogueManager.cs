using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Manager centrale per il sistema di dialogo.
/// Gestisce la camera dinamica e le transizioni tra gameplay e conversazione.
/// Singleton global - va attaccato al GameObject "Managers".
/// </summary>
public class DialogueManager : MonoBehaviour, IGameManager
{
    public ManagerStatus status { get; private set; }

    [Header("Camera Setup")]
    [Tooltip("La Virtual Camera usata per i dialoghi. Verrà attivata/disattivata automaticamente.")]
    public GameObject dialogueVCam;
    
    [Header("Camera Offsets")]
    [Tooltip("Offset dalla testa dell'NPC per il Follow point.")]
    public Vector3 cameraFollowOffset = new Vector3(0.5f, 0f, 0f);
    
    [Header("Timing")]
    public float cameraTransitionDelay = 0.3f;
    
    // Stato interno
    private Transform _currentNPCTarget;
    private DoorController _activeDoorController;
    private bool _isInDialogue = false;
    
    public bool IsInDialogue => _isInDialogue;

    // --- DATA FOR UI (Decoupled) ---
    public NPCProfile CurrentProfile { get; private set; }
    public NPCMemory CurrentMemory { get; private set; }
    public NPCInteraction CurrentInteraction { get; private set; }
    public bool IsKnocking { get; private set; }

    public string SimpleName { get; private set; }
    public string SimpleText { get; private set; }
    public System.Action SimpleOnEnd { get; private set; }
    public System.Action SimpleOnCancel { get; private set; }
    // -------------------------------

    public void Startup(NetworkService service)
    {
        Debug.Log("DialogueManager starting...");
        if (dialogueVCam != null)
        {
            dialogueVCam.SetActive(false);
        }

        // Ascolta il cambio scena per resettare lo stato (essenziale per i Manager immortali)
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        status = ManagerStatus.Started;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset stato al cambio scena
        _isInDialogue = false;
        _currentNPCTarget = null;
        _activeDoorController = null;
        // aspettiamo che la nuova camera si registri
        if (dialogueVCam != null) dialogueVCam.SetActive(false);
    }

    /// <summary>
    /// Metodo chiamato dallo script della Camera nella nuova scena per registrarsi al Manager.
    /// Pattern: Il componente di scena si collega al Manager persistente.
    /// </summary>
    public void RegisterDialogueCamera(GameObject vcam)
    {
        this.dialogueVCam = vcam;
        if (this.dialogueVCam != null)
        {
            this.dialogueVCam.SetActive(false); // Parte spenta
            Debug.Log($"[DialogueManager] Camera registrata: {vcam.name}");
        }
    }

    /// <summary>
    /// Avvia una sequenza di dialogo con un NPC.
    /// Chiamato da DoorController (bussata) o NPCInteraction (dialogo diretto).
    /// </summary>
    public void StartDialogue(GameObject npc, bool openDoorFirst = false, DoorController doorController = null)
    {
        if (_isInDialogue)
        {
            Debug.LogWarning("[DialogueManager] Dialogo già in corso!");
            return;
        }

        if (npc == null)
        {
            Debug.LogError("[DialogueManager] NPC è null!");
            return;
        }
        
        _activeDoorController = doorController;
        StartCoroutine(DialogueSequence(npc, openDoorFirst, doorController));
    }

    /// <summary>
    /// Termina il dialogo e resetta tutto
    /// </summary>
    public void EndDialogue()
    {
        Debug.Log("[DialogueManager] Fine dialogo.");
        
        bool keepDoorOpen = false;

        // Controlla se l'NPC ci ha invitato
        if (_currentNPCTarget != null)
        {
            NPCInteraction npcInt = _currentNPCTarget.GetComponent<NPCInteraction>();
            if (npcInt != null && npcInt.HasInvitedPlayer)
            {
                keepDoorOpen = true;
                Debug.Log("[DialogueManager] Il giocatore è stato invitato! La porta resta aperta.");
                
                // Ordina all'NPC di andare dentro
                npcInt.MoveToInside();
            }
        }
        
        _isInDialogue = false;
        _currentNPCTarget = null;

        // Disattiva la camera dialogo
        if (dialogueVCam != null)
        {
            dialogueVCam.SetActive(false);
        }
        
        // Chiudi la porta SOLO se non siamo stati invitati
        if (_activeDoorController != null)
        {
            if (!keepDoorOpen)
            {
                _activeDoorController.ForceClose();
                Debug.Log("[DialogueManager] Porta chiusa (nessun invito).");
            }
            
            _activeDoorController = null;
        }
    }

    private IEnumerator DialogueSequence(GameObject npc, bool openDoorFirst, DoorController doorController)
    {
        _isInDialogue = true;
        _currentNPCTarget = npc.transform;

        // 1. Se dobbiamo aprire una porta prima, fallo
        if (openDoorFirst && doorController != null)
        {
            yield return new WaitForSeconds(0.5f);
            doorController.ForceOpen();
            Debug.Log("[DialogueManager] Porta aperta.");
            yield return new WaitForSeconds(cameraTransitionDelay);
        }

        // 2. Setup Camera
        Transform headTarget = FindNPCHead(npc);
        Vector3 cameraPosition = CalculateCameraPosition(npc, headTarget);
        
        if (dialogueVCam != null)
        {
            dialogueVCam.transform.position = cameraPosition;
            dialogueVCam.transform.LookAt(headTarget);
            SetupDialogueCamera(npc.transform, headTarget);
            dialogueVCam.SetActive(true);
        }

        yield return new WaitForSeconds(0.2f);

        // 3. Recupera Dati
        NPCInteraction npcInteraction = npc.GetComponent<NPCInteraction>();
        NPCMemory npcMemory = npc.GetComponent<NPCMemory>();
        NPCProfile npcProfile = npcInteraction?.npcProfile;

        if (npcProfile != null && npcMemory != null)
        {
            // 4. Imposta i Dati per la UI
            CurrentProfile = npcProfile;
            CurrentMemory = npcMemory;
            CurrentInteraction = npcInteraction;
            IsKnocking = openDoorFirst;

            // 5. Lancia l'evento (la UI risponderà)
            Messenger.Broadcast(GameEvent.SHOW_DIALOGUE_UI);
            Messenger.AddListener(GameEvent.STOP_DIALOGUE, OnDialogueEnded);
            
            // Registra il contatto nel GameManager per il progresso
            if (GameManager.Instance != null)
            {
                Debug.Log($"[DialogueManager] Chiamo RegistraContatto per: {npcProfile.characterName}");
                GameManager.Instance.RegistraContatto(npcProfile.characterName);
                Debug.Log($"[DialogueManager] Dialoghi ATTUALI nel GM: {GameManager.Instance.dialoghiCompletati}");
            }
            
            Debug.Log($"[DialogueManager] Evento SHOW_DIALOGUE_UI inviato per '{npcProfile.characterName}'.");
        }
        else
        {
            Debug.LogError("[DialogueManager] Impossibile avviare dialogo: manca NPCProfile o NPCMemory!");
            EndDialogue();
        }
    }

    /// <summary>
    /// Avvia un dialogo SEMPLICE (senza memoria AI), ma con la telecamera corretta.
    /// Supporta callback di fine (INVIO) e annullamento (ESC).
    /// </summary>
    public void StartSimpleDialogue(GameObject npc, string name, string text, System.Action onEnd = null, System.Action onCancel = null)
    {
        if (_isInDialogue) return;
        _isInDialogue = true;
        
        if (npc != null)
        {
            _currentNPCTarget = npc.transform;

            // Camera
            Transform headTarget = FindNPCHead(npc);
            Vector3 cameraPosition = CalculateCameraPosition(npc, headTarget);
            if (dialogueVCam != null)
            {
                dialogueVCam.transform.position = cameraPosition;
                dialogueVCam.transform.LookAt(headTarget);
                SetupDialogueCamera(npc.transform, headTarget);
                dialogueVCam.SetActive(true);
            }
        }
        else
        {
             // Nessun NPC: non muovere la camera, usa quella attuale (o nessuna se in FPS)
             // Se servisse un comportamento statico per gli oggetti, andrebbe gestito qui.
             // Per ora lasciamo la camera dov'è (FPS player).
             _currentNPCTarget = null;
        }

        // Imposta Dati
        SimpleName = name;
        SimpleText = text;
        SimpleOnEnd = onEnd;
        SimpleOnCancel = onCancel;

        // Lancia Evento
        Messenger.Broadcast(GameEvent.SHOW_SIMPLE_DIALOGUE);
        Messenger.AddListener(GameEvent.STOP_DIALOGUE, OnDialogueEnded);
    }

    private void OnDialogueEnded()
    {
        Messenger.RemoveListener(GameEvent.STOP_DIALOGUE, OnDialogueEnded);
        EndDialogue();
    }

    /// <summary>
    /// Avvia il popup speciale per gli oggetti raccolti (senza camera, UI diversa).
    /// </summary>
    public void StartPickupDialogue(string message, System.Action onEnd = null)
    {
        if (_isInDialogue) return;
        _isInDialogue = true;
        
        _currentNPCTarget = null; // Nessun target camera
        
        // Imposta Dati (riutilizziamo i campi "Simple" per comodità, o ne creiamo di nuovi)
        SimpleText = message;
        SimpleOnEnd = onEnd;
        SimpleOnCancel = null;

        // Lancia Evento SPECIFICO per il popup
        Messenger.Broadcast(GameEvent.SHOW_PICKUP_POPUP);
        Messenger.AddListener(GameEvent.STOP_DIALOGUE, OnDialogueEnded);
    }

    /// <summary>
    /// Configura la Virtual Camera per Cinemachine (supporta 2.x e 3.x)
    /// </summary>
    private void SetupDialogueCamera(Transform npcTransform, Transform headTarget)
    {
        if (dialogueVCam == null) return;
        
        // Reflection per supportare diverse versioni di Cinemachine senza errori di compilazione
        Component vcam = dialogueVCam.GetComponent("CinemachineCamera"); // CM 3.x
        if (vcam == null) vcam = dialogueVCam.GetComponent("CinemachineVirtualCamera"); // CM 2.x

        if (vcam != null)
        {
            try
            {
                System.Type vcamType = vcam.GetType();
                
                // CM 3.x
                var trackingProp = vcamType.GetProperty("TrackingTarget");
                if (trackingProp != null) trackingProp.SetValue(vcam, npcTransform);
                
                var lookAtTargetProp = vcamType.GetProperty("LookAtTarget");
                if (lookAtTargetProp != null) lookAtTargetProp.SetValue(vcam, headTarget);

                // CM 2.x Fallback
                if (trackingProp == null)
                {
                    var followProp = vcamType.GetProperty("Follow");
                    if (followProp != null) followProp.SetValue(vcam, npcTransform);
                }
                
                if (lookAtTargetProp == null)
                {
                    var lookAtProp = vcamType.GetProperty("LookAt");
                    if (lookAtProp != null) lookAtProp.SetValue(vcam, headTarget);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DialogueManager] Errore configurazione camera: {e.Message}");
            }
        }
    }

    private Transform FindNPCHead(GameObject npc)
    {
        Transform[] children = npc.GetComponentsInChildren<Transform>();
        foreach (Transform t in children)
        {
            string nameLower = t.name.ToLower();
            if (nameLower.Contains("head") && !nameLower.Contains("end")) return t;
        }
        foreach (Transform t in children)
        {
            if (t.name.ToLower().Contains("neck") || t.name.ToLower().Contains("spine")) return t;
        }
        return npc.transform;
    }

    private Vector3 CalculateCameraPosition(GameObject npc, Transform headTarget)
    {
        Vector3 npcPosition = npc.transform.position;
        Vector3 npcForward = npc.transform.forward;
        Vector3 npcRight = npc.transform.right;
        
        float cameraHeight = headTarget != null ? headTarget.position.y : npcPosition.y + 1.5f;
        float distanceFromNPC = 3.0f; 
        float lateralOffset = 1.5f;
        
        Vector3 cameraPosition = npcPosition 
            + (npcForward * distanceFromNPC)
            + (npcRight * lateralOffset)
            + (Vector3.up * (cameraHeight - npcPosition.y));
        
        return cameraPosition;
    }
}
