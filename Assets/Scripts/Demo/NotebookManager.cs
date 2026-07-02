using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;

public class NotebookManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject notebookPanel;      
    public GameObject pageContentHolder;
    
    // Riferimento alla scritta fissa "Q Agenda"
    public GameObject notebookPromptUI;   
    
    public Transform leftPageContainer;   
    public TextMeshProUGUI rightPageText; 
    
    public Animator bookAnimator; 
    
    [Header("Settings")]
    public float animationDuration = 0.8f; 
    public float closingDelay = 1.0f;      

    [Header("Correction Settings (Anti-Stretch)")]
    public float portraitScaleX = 1.0f; 
    public float textScaleX = 1.0f;

    [Header("Prefabs")]
    public GameObject buttonPrefab;       

    // --- NUOVO: SEZIONE AUDIO ---
    [Header("Audio Settings")]
    public AudioSource audioSource;       // Trascina qui l'AudioManager
    public AudioClip clickSound;          // Trascina qui "UI-sound-8"
    // ----------------------------

    [System.Serializable]
    public class NotebookEntry
    {
        public string npcID;        
        public Sprite portrait;     
        [TextArea(3, 10)]           
        public string currentNote;  
    }

    public List<NotebookEntry> entries = new List<NotebookEntry>();
    public static bool IsNotebookOpen = false;
    private bool _isAnimating = false; 

    // --- AUTOMATISMO VISIBILITÀ ---
    private void OnEnable()
    {
        if (notebookPromptUI != null) notebookPromptUI.SetActive(true);
    }

    private void OnDisable()
    {
        if (notebookPromptUI != null) notebookPromptUI.SetActive(false);
    }

    // Reference to Brain
    private CinemachineBrain _brain;

    private void Start()
    {
        if (pageContentHolder != null) pageContentHolder.SetActive(false);
        notebookPanel.SetActive(false); 
        
        IsNotebookOpen = false;
        _isAnimating = false;

        // Trova la brain per bloccare la camera
        _brain = Transform.FindFirstObjectByType<CinemachineBrain>();

        if (bookAnimator != null) bookAnimator.SetBool("IsOpen", false);

        if (notebookPromptUI != null) notebookPromptUI.SetActive(true);
    }

    private void Update()
    {
        // GESTIONE VISIBILITÀ SCRITTA
        if (notebookPromptUI != null)
        {
            WardrobeController wc = FindFirstObjectByType<WardrobeController>();
            bool isWardrobeOpen = (wc != null && wc.wardrobePanel.activeSelf);

            bool shouldBeHidden = DialogueUI.IsDialogueOpen || IsNotebookOpen || isWardrobeOpen;
            
            if (notebookPromptUI.activeSelf == shouldBeHidden)
            {
                notebookPromptUI.SetActive(!shouldBeHidden);
            }
        }

        // ESC per chiudere l'agenda
        if (Input.GetKeyDown(KeyCode.Escape) && IsNotebookOpen && !_isAnimating)
        {
            StartCoroutine(CloseBookSequence());
            return; 
        }

        // Q per aprire/chiudere
        WardrobeController _wc = FindFirstObjectByType<WardrobeController>();
        bool _iso = (_wc != null && _wc.wardrobePanel.activeSelf);

        if (Input.GetKeyDown(KeyCode.Q) && !_isAnimating && !DialogueUI.IsDialogueOpen && !_iso)
        {
            if (!IsNotebookOpen) StartCoroutine(OpenBookSequence());
            else StartCoroutine(CloseBookSequence());
        }
    }

    // --- FUNZIONE PER IL SUONO ---
    private void PlayClickSound()
    {
        AudioManager.PlaySFX(audioSource, clickSound);
    }

    private IEnumerator OpenBookSequence()
    {
        _isAnimating = true;
        IsNotebookOpen = true;

        // Blocca la camera (Cinemachine)
        if (_brain != null) _brain.enabled = false;

        if (notebookPromptUI != null) notebookPromptUI.SetActive(false);

        notebookPanel.SetActive(true);
        if (pageContentHolder != null) pageContentHolder.SetActive(false);

        if (bookAnimator != null) bookAnimator.SetBool("IsOpen", true);

        yield return new WaitForSeconds(animationDuration);

        RefreshLeftPage(); 
        rightPageText.text = "Seleziona un dossier...";
        if (pageContentHolder != null) pageContentHolder.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _isAnimating = false;
    }

    private IEnumerator CloseBookSequence()
    {
        _isAnimating = true;
        IsNotebookOpen = false;

        if (pageContentHolder != null) pageContentHolder.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (bookAnimator != null) bookAnimator.SetBool("IsOpen", false);

        yield return new WaitForSeconds(animationDuration);
        yield return new WaitForSeconds(closingDelay);

        notebookPanel.SetActive(false);
        
        // Sblocca la camera
        if (_brain != null) _brain.enabled = true;

        if (this.enabled && notebookPromptUI != null) notebookPromptUI.SetActive(true);

        _isAnimating = false;
    }

    public void UpdateEntry(string id, string newText)
    {
        Debug.Log($"[Notebook] UpdateEntry chiamato con ID: '{id}'");
        
        // Log di tutte le entry esistenti per confronto
        foreach (var e in entries)
        {
            Debug.Log($"[Notebook]   - Entry esistente: '{e.npcID}' (match: {e.npcID == id})");
        }
        
        NotebookEntry existing = entries.Find(x => x.npcID == id);
        if (existing != null)
        {
            existing.currentNote = newText;
            Debug.Log($"[Notebook] ✅ Entry trovata e aggiornata per '{id}'");
            if (IsNotebookOpen && rightPageText.text.Contains(id)) ShowNoteWithTitle(id, existing.currentNote);
        }
        else
        {
            Debug.LogWarning($"[Notebook] ❌ Nessuna entry trovata con ID '{id}'! Controlla che corrisponda.");
        }
    }

    void RefreshLeftPage()
    {
        foreach (Transform child in leftPageContainer) Destroy(child.gameObject);
        foreach (NotebookEntry entry in entries)
        {
            GameObject newBtn = Instantiate(buttonPrefab, leftPageContainer);
            
            TextMeshProUGUI txt = newBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) 
            {
                txt.text = entry.npcID;
                txt.rectTransform.localScale = new Vector3(textScaleX, 1f, 1f);
            }

            Transform imgTransform = newBtn.transform.Find("PortraitImage");
            if (imgTransform != null)
            {
                Image icon = imgTransform.GetComponent<Image>();
                if (entry.portrait != null) 
                { 
                    icon.sprite = entry.portrait; 
                    icon.color = Color.white; 
                }
                else 
                { 
                    icon.color = Color.clear; 
                }
                imgTransform.localScale = new Vector3(portraitScaleX, 1f, 1f);
            }

            string t = entry.currentNote; 
            string n = entry.npcID;
            
            // --- MODIFICA QUI: Aggiungiamo il suono al click ---
            newBtn.GetComponent<Button>().onClick.AddListener(() => 
            {
                PlayClickSound(); // 1. Suono
                ShowNoteWithTitle(n, t); // 2. Azione
            });
            // ---------------------------------------------------
        }
    }

    void ShowNoteWithTitle(string name, string text)
    {
        rightPageText.text = text;
    }
}