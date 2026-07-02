using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public class NPCResponseData
{
    public string reply;       
    public int faith_delta;    
    public string action; // SMALL_TALK, REFUSE, INVITE_IN
}

public class DialogueUI : MonoBehaviour
{
    // --- STATIC STATE ---
    // Manteniamo questi flag statici per compatibilità con il resto del sistema (es. PlayerController)
    public static bool IsDialogueOpen = false;
    public static bool EscHandledThisFrame = false;

    // --- UI REFERENCES (SERIALIZED) ---
    [Header("UI References")]
    public GameObject dialoguePanel;      
    public TextMeshProUGUI dialogueText;  
    public GameObject inputBubble; // Il "fumetto" dove il giocatore scrive
    public GameObject escPrompt;   // Suggerimento "Premi ESC"
    public GameObject placeholderObject; // Placeholder visivo dell'input
    public TextMeshProUGUI npcNameText;   
    
    [Header("External Systems")]
    public NotebookManager notebookManager; 

    [Header("Input Controls")]
    public TMP_InputField inputField;
    public Button sendButton;
    public ScrollRect scrollRect; 

    [Header("Pickup Popup")]
    public GameObject pickupPanel;      
    public TextMeshProUGUI pickupText; 

    [Header("Trust System")]
    public Slider trustSlider;
    public Image trustFillImage;
    [Header("Trust Feedback Icons")]
    public GameObject positiveFeedbackIcon; 
    public GameObject negativeFeedbackIcon; 
    public float feedbackDuration = 2.0f;   
    public GameObject continuePrompt; // Icona "Premi Spazio"

    [Header("Typing Effect Settings")]
    public float typingSpeed = 0.02f; 

    [Header("Audio Settings")]
    public AudioSource audioSource;       
    public AudioClip typingSound;         
    [Range(0.5f, 3f)] public float minPitch = 0.9f; 
    [Range(0.5f, 3f)] public float maxPitch = 1.1f; 
    public int audioFrequency = 2;        

    // --- INTERNAL ENUMS ---
    private enum DialogueMode 
    { 
        None, 
        Standard,   // Dialogo LLM classica
        Simple,     // Dialogo scriptato (Poliziotto, ecc) con "Continua"
        Pickup      // Popup oggetto raccolto
    }

    // --- RUNTIME STATE ---
    private DialogueMode _currentMode = DialogueMode.None;
    private NPCMemory _currentMemory; 
    private NPCProfile _currentProfile;
    private NPCInteraction _currentInteraction; 
    
    private Coroutine _typingCoroutine;
    private Coroutine _feedbackCoroutine;
    
    // Callbacks per dialoghi semplici/pickup
    private System.Action _onCompleteCallback;
    private System.Action _onCancelCallback;
    
    private string _fullTargetText = ""; // Il testo completo che stiamo scrivendo
    private bool _isTyping = false;      // Se stiamo ancora scrivendo a macchina
    private bool _isConversationRefused = false; // Se l'NPC ci ha cacciato

    private const string GAME_RULES_PROMPT = 
    @"Sei un NPC in un videogioco investigativo. Rispondi SOLO in JSON con questo schema:
{""reply"":""la tua risposta"",""delta"":0,""action"":""SMALL_TALK""}
AZIONI: SMALL_TALK (conversazione normale), REFUSE (caccia via/termina), INVITE_IN (invita ad entrare in casa - USA SOLO SE IL GIOCATORE È FUORI)";

    // --- UNITY MESSAGE METHODS ---

    private void Awake()
    {
        Debug.Log($"[DialogueUI V2] Awake() on '{gameObject.name}'");

        // Event Listeners
        Messenger.AddListener(GameEvent.SHOW_DIALOGUE_UI, OnRequestDialogue);
        Messenger.AddListener(GameEvent.SHOW_SIMPLE_DIALOGUE, OnRequestSimpleDialogue);
        Messenger.AddListener(GameEvent.SHOW_PICKUP_POPUP, OnRequestPickupPopup);
        
        if (sendButton != null) sendButton.onClick.AddListener(OnSendClicked);

        // Assicuriamoci che tutto sia chiuso all'avvio
        HideAllPanels();
    }
    
    private void OnDestroy()
    {
        Messenger.RemoveListener(GameEvent.SHOW_DIALOGUE_UI, OnRequestDialogue);
        Messenger.RemoveListener(GameEvent.SHOW_SIMPLE_DIALOGUE, OnRequestSimpleDialogue);
        Messenger.RemoveListener(GameEvent.SHOW_PICKUP_POPUP, OnRequestPickupPopup);
    }

    private void Update()
    {
        // Reset flag frame
        EscHandledThisFrame = false;
        
        if (!IsDialogueOpen) return;

        // 1. Gestione Skip Scrittura (Spazio/Invio)
        bool typingSkipped = HandleTypingSkip();

        if (typingSkipped) return; // Se abbiamo skippato la scrittura, NON processare l'input di avanzamento nello stesso frame

        // 2. Gestione Input vari (ESC, Invio Messaggio)
        HandleInput();
    }

    private void LateUpdate()
    {
        // Forza il cursore visibile se il dialogo è aperto
        if (IsDialogueOpen && (!Cursor.visible || Cursor.lockState != CursorLockMode.None))
        {
            EnsureCursorVisible();
        }
    }

    // --- EVENT RESPONSES ---

    private void OnRequestDialogue()
    {
        var manager = Managers.Dialogue;
        if (manager != null)
            StartStandardConversation(manager.CurrentProfile, manager.CurrentMemory, manager.CurrentInteraction, manager.IsKnocking);
    }

    private void OnRequestSimpleDialogue()
    {
        var manager = Managers.Dialogue;
        if (manager != null)
            ShowSimpleDialogue(manager.SimpleName, manager.SimpleText, manager.SimpleOnEnd, manager.SimpleOnCancel);
    }

    private void OnRequestPickupPopup()
    {
        var manager = Managers.Dialogue;
        if (manager != null)
            ShowPickupPopup(manager.SimpleText, manager.SimpleOnEnd);
    }

    // --- PUBLIC API ---

    /// <summary>
    /// Avvia una conversazione standard con LLM
    /// </summary>
    public void StartStandardConversation(NPCProfile profile, NPCMemory memory, NPCInteraction interaction, bool isKnocking)
    {
        _currentMode = DialogueMode.Standard;
        _currentProfile = profile;
        _currentMemory = memory;
        _currentInteraction = interaction;
        _isConversationRefused = false;

        ConfigureUI(DialogueMode.Standard, profile.characterName);
        InitializeLLMContext(profile, memory, isKnocking);
    }

    /// <summary>
    /// Mostra un dialogo semplice (es. Poliziotto) senza input text
    /// </summary>
    public void ShowSimpleDialogue(string speakerName, string content, System.Action onEnd = null, System.Action onCancel = null)
    {
        _currentMode = DialogueMode.Simple;
        _onCompleteCallback = onEnd;
        _onCancelCallback = onCancel;
        _currentMemory = null; 

        ConfigureUI(DialogueMode.Simple, speakerName);
        StartTyping(content, dialogueText);
    }
    
    /// <summary>
    /// Metodo wrapper per compatibilità
    /// </summary>
    public void ShowFixedMessage(string name, string text)
    {
        ShowSimpleDialogue(name, text, null, null);
    }

    /// <summary>
    /// Mostra popup oggetto raccolto
    /// </summary>
    public void ShowPickupPopup(string message, System.Action onEnd = null)
    {
        _currentMode = DialogueMode.Pickup;
        _onCompleteCallback = onEnd;
        _onCancelCallback = null;

        ConfigureUI(DialogueMode.Pickup, ""); 
        
        if (pickupText != null) pickupText.text = "";
        StartTyping(message, pickupText);
    }

    /// <summary>
    /// Chiude la conversazione corrente
    /// </summary>
    /// <param name="completed">True se l'azione è conclusa (Invio/Spazio), False se annullata (ESC)</param>
    public void CloseConversation(bool completed = true)
    {
        // 1. Genera riassunto (solo Standard)
        if (_currentMode == DialogueMode.Standard && _currentMemory != null)
        {
             if (_currentMemory.chatHistory.Count > 2)
             {
                GenerateAutoSummary(_currentProfile.characterName, _currentMemory);
             }
        }

        // 2. Reset stato fisico (porte, ecc.)
        if (_isConversationRefused && _currentInteraction != null)
        {
            _currentInteraction.ReturnToDoor();
        }

        // 3. Salva callback da eseguire
        // Se Standard: nessuna callback particolare
        // Se Simple/Pickup: esegui onComplete o onCancel
        System.Action callbackToRun = null;
        if (_currentMode == DialogueMode.Simple || _currentMode == DialogueMode.Pickup)
        {
            callbackToRun = completed ? _onCompleteCallback : _onCancelCallback;
        }

        // 4. Chiudi UI
        ResetRuntimeState();
        HideAllPanels();
        
        IsDialogueOpen = false;
        Messenger.Broadcast(GameEvent.STOP_DIALOGUE); 

        // 5. Esegui callback (dopo aver chiuso tutto)
        callbackToRun?.Invoke();
    }

    // --- PRIVATE LOGIC: SETUP & INPUT ---

    private void ConfigureUI(DialogueMode mode, string title)
    {
        IsDialogueOpen = true;
        Messenger.Broadcast(GameEvent.START_DIALOGUE);
        EnsureCursorVisible();

        // Setup base pannelli
        if (dialoguePanel) dialoguePanel.SetActive(mode == DialogueMode.Standard || mode == DialogueMode.Simple);
        if (pickupPanel) pickupPanel.SetActive(mode == DialogueMode.Pickup);
        
        // Pannello Dialogo Standard/Semplice
        if (mode != DialogueMode.Pickup)
        {
            if (npcNameText) npcNameText.text = title;
            bool isInputActive = (mode == DialogueMode.Standard) && !_isConversationRefused;
            
            if (inputBubble) inputBubble.SetActive(isInputActive);
            if (inputField) inputField.gameObject.SetActive(isInputActive);
            if (sendButton) sendButton.gameObject.SetActive(isInputActive);
            
            // ESC sempre visibile
            if (escPrompt) 
            {
                escPrompt.SetActive(true);
                escPrompt.transform.SetAsLastSibling();
            }
            
            if (continuePrompt) continuePrompt.SetActive(false); // Nascondilo finché non finisce di scrivere
            
            // Visuals
            if (mode == DialogueMode.Standard) UpdateTrustVisuals();
            
            // Reset Input Field
            if (inputField) 
            {
                inputField.text = "";
                inputField.interactable = true;
                if (placeholderObject) placeholderObject.SetActive(true);
            }
        }
        else // Pickup Mode
        {
             if (pickupPanel) pickupPanel.transform.SetAsLastSibling();
             if (escPrompt) escPrompt.SetActive(true);
        }
    }

    private bool HandleTypingSkip()
    {
        // Tasti di conferma/skip
        bool skipRequested = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

        if (skipRequested && _isTyping)
        {
            // Salta l'animazione di scrittura
            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            
            // Determina quale text component aggiornare
            TextMeshProUGUI activeText = (_currentMode == DialogueMode.Pickup) ? pickupText : dialogueText;
            if (activeText != null) activeText.text = _fullTargetText;
            
            _isTyping = false;
            OnTypingCompleted();
            return true;
        }
        return false;
    }

    private void HandleInput()
    {
        // ESC -> Chiudi (Annulla)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EscHandledThisFrame = true;
            CloseConversation(completed: false);
            return;
        }

        // Se stiamo scrivendo, ignora altri input (lo skip è gestito sopra)
        if (_isTyping) return;

        // Gestione INVIO/SPAZIO (Solo per Dialoghi Semplici/Pickup -> "Continua")
        // Nei dialoghi Standard, Invio serve per INVIARE il messaggio, non per chiudere.
        bool confirmPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        
        if (confirmPressed)
        {
            if (_currentMode == DialogueMode.Simple || _currentMode == DialogueMode.Pickup)
            {
                CloseConversation(completed: true);
            }
            else if (_currentMode == DialogueMode.Standard)
            {
                // In Standard, Invio invia il messaggio (se focus su input o se siamo liberi)
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    OnSendClicked();
                }
            }
        }
    }

    private void OnTypingCompleted()
    {
        // Mostra prompt "Continua" se in modalità semplice
        if ((_currentMode == DialogueMode.Simple || _currentMode == DialogueMode.Pickup) && continuePrompt != null)
        {
            continuePrompt.SetActive(true);
        }
        
        // Scroll to bottom
        StartCoroutine(ScrollToBottomCoroutine());
    }

    // --- LLM LOGIC ---

    private void InitializeLLMContext(NPCProfile profile, NPCMemory memory, bool isKnocking)
    {
        // Logica memoria costumi
        string currentCostumeName = GetCurrentCostumeName();
        memory.SetCurrentCostume(currentCostumeName);
        
        bool isFirstMeeting = !memory.HasMetWithCurrentCostume();
        string costumeDesc = GetCostumeDescription();

        // Costruzione Prompt Sistema
        string characterPrompt = profile.ConstructSystemPromptWithCostume(costumeDesc, memory.trustScore, currentCostumeName);
        string fullContext = $"{GAME_RULES_PROMPT}\n\n{characterPrompt}";

        if (!isFirstMeeting)
        {
            string prevSummary = memory.GetSummaryForCurrentCostume();
            fullContext += $"\n[RICORDI PRECEDENTI]\nHai già parlato con questa persona. Ecco cosa ricordi: \"{prevSummary}\"\n";
        }
        else
        {
            // Applica trust iniziale se primo incontro
            memory.trustScore = profile.GetInitialTrustForCostume(currentCostumeName);
        }

        memory.ClearChatHistory();
        memory.AddMessage("system", fullContext);

        // Visuals update
        UpdateTrustVisuals();

        // Trigger message
        string triggerMsg = isFirstMeeting 
            ? (isKnocking ? "[AZIONE: Il giocatore bussa...]" : "[AZIONE: Il giocatore si avvicina...]")
            : "[AZIONE: Il giocatore torna a parlarti...]";

        dialogueText.text = "...";
        memory.AddMessage("user", triggerMsg);
        
        Managers.LLM.SendChat(BuildHistoryForRequest(memory), OnLLMCompleteReply);
    }

    private void OnSendClicked()
    {
        if (_currentMode != DialogueMode.Standard) return;
        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text)) return;
        
        string playerMessage = inputField.text.Trim();
        string trustNote = $"[Fiducia attuale: {_currentMemory.GetTrustLevelDescription()}]";
        string messageWithTrust = $"{trustNote}\n{playerMessage}";
        
        _currentMemory.AddMessage("user", messageWithTrust);
        
        // UI Feedback
        inputField.text = ""; 
        if (placeholderObject != null) placeholderObject.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        dialogueText.text = "..."; 
        
        Managers.LLM.SendChat(BuildHistoryForRequest(_currentMemory), OnLLMCompleteReply);
    }

    private void OnLLMCompleteReply(string fullText)
    {
        if (!IsDialogueOpen || _currentMemory == null) return;
        
        NPCResponseData data = LLMResponseParser.Parse(fullText);
        if (data.faith_delta == 0) data.faith_delta = 1; // Feedback visivo minimo

        ShowTrustVisualFeedback(data.faith_delta);
        _currentMemory.UpdateTrust(data.faith_delta);
        UpdateTrustVisuals();

        string processedReply = ProcessNPCAction(data);
        _currentMemory.AddMessage("assistant", data.reply);
        
        StartTyping(processedReply, dialogueText);
        
        Messenger.Broadcast(GameEvent.NPC_RESPONSE_RECEIVED);
    }

    private string ProcessNPCAction(NPCResponseData data)
    {
        string finalString = data.reply;
        string action = data.action?.ToUpper() ?? "SMALL_TALK";

        switch (action)
        {
            case "INVITE_IN":
                finalString += "\n\n<color=green>*** PREGO, ENTRATE ***</color>";
                if (_currentInteraction != null) _currentInteraction.InvitePlayer();
                Messenger.Broadcast(GameEvent.DOOR_UNLOCK);
                break;
                
            case "REFUSE":
                finalString += "\n\n<color=red>[L'NPC ti guarda con disapprovazione. Premi ESC per andartene.]</color>";
                DisableInputElements();
                _isConversationRefused = true;
                break;
        }
        return finalString;
    }

    // --- TYPING & UI HELPERS ---

    private IEnumerator TypeWriterRoutine(string text, TextMeshProUGUI target)
    {
        _isTyping = true;
        _fullTargetText = text;
        if (continuePrompt) continuePrompt.SetActive(false);
        
        target.text = "";
        string displayed = "";
        
        // Audio setup
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        int charCount = 0;
        foreach (char c in text)
        {
            displayed += c;
            target.text = displayed;

            if (!char.IsWhiteSpace(c) && audioSource && typingSound)
            {
                charCount++;
                if (charCount % audioFrequency == 0)
                {
                    audioSource.pitch = Random.Range(minPitch, maxPitch);
                    audioSource.PlayOneShot(typingSound);
                }
            }
            
            // Auto-scroll sempe in basso durante la scrittura
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
            
            yield return new WaitForSeconds(typingSpeed);
        }

        _isTyping = false;
        OnTypingCompleted();
    }

    private void StartTyping(string text, TextMeshProUGUI target)
    {
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypeWriterRoutine(text, target));
    }

    private void ShowTrustVisualFeedback(int delta)
    {
        if (delta == 0) return;
        
        if (positiveFeedbackIcon) positiveFeedbackIcon.SetActive(false);
        if (negativeFeedbackIcon) negativeFeedbackIcon.SetActive(false); // Reset

        GameObject icon = (delta > 0) ? positiveFeedbackIcon : negativeFeedbackIcon;
        if (icon)
        {
            icon.SetActive(true);
            if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);
            _feedbackCoroutine = StartCoroutine(HideFeedbackInternal());
        }
    }

    private IEnumerator HideFeedbackInternal()
    {
        yield return new WaitForSeconds(feedbackDuration);
        if (positiveFeedbackIcon) positiveFeedbackIcon.SetActive(false);
        if (negativeFeedbackIcon) negativeFeedbackIcon.SetActive(false);
    }

    private void UpdateTrustVisuals()
    {
        if (_currentMemory == null) return;
        int score = _currentMemory.trustScore;
        if (trustSlider) trustSlider.value = score;
        if (trustFillImage)
        {
            trustFillImage.color = (score <= 30) ? Color.red : (score >= 70 ? Color.green : Color.yellow);
        }
    }

    private void GenerateAutoSummary(string npcName, NPCMemory memory)
    {
        var history = memory.chatHistory;
        System.Text.StringBuilder transcript = new System.Text.StringBuilder();
        foreach (var msg in history)
        {
            if (msg.role != "system") transcript.AppendLine($"{msg.role}: {msg.content}");
        }

        string contextPrompt = 
            $"Sei il taccuino di un detective. Leggi la conversazione e annota SOLO le dichiarazioni dell'NPC (assistant) utili all'indagine. " +
            $"ANNOTA: alibi, orari, luoghi, accuse verso altri, confessioni, indizi sul caso. " +
            $"NON ANNOTARE: quello che dice 'user', livelli di fiducia, tono, atteggiamento, saluti, chiacchiere. " +
            $"Se l'NPC non rivela nulla di utile, scrivi esattamente: 'Nessuna informazione rilevante.' " +
            $"Stile: frasi brevi in Italiano, max 15 parole. Esempio: 'Dichiara di essere stato al bar alle 22. Accusa il giardiniere.' " +
            $"Solo testo semplice, no markdown.";
        List<OllamaMessage> summaryReq = new List<OllamaMessage>();
        summaryReq.Add(new OllamaMessage("system", contextPrompt));
        summaryReq.Add(new OllamaMessage("user", "TRASCRIZIONE:\n" + transcript.ToString()));

        // Capture context variables for closure
        string capturedName = npcName;
        NPCMemory capturedMem = memory;

        Managers.LLM.SendSummaryChat(summaryReq, (result) => 
        {
            string clean = LLMResponseParser.RemoveThinkBlocks(result);
            if (capturedMem != null)
            {
                capturedMem.SetSummaryForCurrentCostume(clean);
                capturedMem.AppendToGlobalSummary(clean);
                if (notebookManager != null) notebookManager.UpdateEntry(capturedName, capturedMem.GetGlobalSummary());
            }
        });
    }

    // --- MISC UTILITIES ---

    private void ResetRuntimeState()
    {
        _currentMemory = null;
        _currentProfile = null;
        _currentInteraction = null;
        _onCompleteCallback = null;
        _onCancelCallback = null;
        _fullTargetText = "";
        
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _isTyping = false;
        
        if (inputField) inputField.interactable = true;
        if (sendButton) sendButton.interactable = true;
    }

    private void HideAllPanels()
    {
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (pickupPanel) pickupPanel.SetActive(false);
        if (positiveFeedbackIcon) positiveFeedbackIcon.SetActive(false);
        if (negativeFeedbackIcon) negativeFeedbackIcon.SetActive(false);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void EnsureCursorVisible()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void DisableInputElements()
    {
        if (inputField) inputField.interactable = false;
        if (sendButton) sendButton.interactable = false;
    }

    private List<OllamaMessage> BuildHistoryForRequest(NPCMemory memory)
    {
        if (memory == null) return new List<OllamaMessage>();
        var temp = new List<OllamaMessage>(memory.chatHistory);
        
        // Add dynamic context
        int trust = memory.trustScore;
        string proveStatus = (Managers.Player != null && Managers.Player.HasEvidence) ? "SI" : "NO";
        CostumeType costume = (Managers.Player != null) ? Managers.Player.CurrentCostume : CostumeType.Poliziotto;
        
        string locationContext = "";
        if (_currentInteraction != null && _currentInteraction.HasInvitedPlayer)
        {
            locationContext = "\nCONTESTO LUOGO: Il giocatore è ORA DENTRO casa tua. Non usare l'azione INVITE_IN perchè è già dentro.";
        }

        string dynamicInfo = $"--- AGGIORNAMENTO STATO ---\nLIVELLO FIDUCIA: {trust}/100{locationContext}";
        
        temp.Add(new OllamaMessage("system", dynamicInfo));
        return temp;
    }

    // --- COSTUME HELPERS ---
    
    private string GetCostumeDescription()
    {
        // Recupera descrizione LLM del costume
        if (Managers.Player != null)
        {
             var player = GameObject.FindWithTag("Player");
             if (player != null)
             {
                 var cm = player.GetComponent<CostumeManager>();
                 if (cm != null) return cm.GetCurrentCostumeLLMDescription();
             }
        }
        return "Indossa abiti normali.";
    }

    private string GetCurrentCostumeName()
    {
        if (Managers.Player != null)
        {
             var player = GameObject.FindWithTag("Player");
             if (player != null)
             {
                 var cm = player.GetComponent<CostumeManager>();
                 if (cm != null)
                 {
                     if (cm.IsWearingBase) return "Base";
                     if (cm.CurrentCostume != null) return cm.CurrentCostume.gameObjectName;
                 }
             }
        }
        return "Base";
    }
    
    // Helper per scrollare in fondo manualmente
    public void ScrollToBottomNow() 
    { 
        if(gameObject.activeInHierarchy) StartCoroutine(ScrollToBottomCoroutine()); 
    }
    
    private IEnumerator ScrollToBottomCoroutine() 
    { 
        yield return new WaitForEndOfFrame(); 
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f; 
    }
}
