using UnityEngine;
using UnityEngine.UI;

public class AccusationUI : MonoBehaviour
{
    public static AccusationUI Instance;

    [Header("UI References")]
    public GameObject mainPanel;
    public Button confirmButton;

    [Header("Audio Settings")]
    public AudioSource audioSource;       // Trascina qui l'AudioManager o la MainCamera
    public AudioClip selectionSound;      // Trascina qui "UI-sound-8"

    // Variabili interne
    private string _idSospettatoSelezionato = "";
    private GameObject _currentOutline;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        Hide();
    }

    // Proprietà pubblica per sapere se è aperto
    public bool IsOpen => mainPanel != null && mainPanel.activeSelf;

    public void Show()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        _idSospettatoSelezionato = "";
        
        // Resetta la selezione grafica
        if (_currentOutline != null) _currentOutline.SetActive(false);
        
        // Disabilita il tasto conferma finché non scegli qualcuno
        if (confirmButton != null) confirmButton.interactable = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Se il pannello è aperto e premo ESC, chiudi SOLO questo pannello
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    // --- FUNZIONE PER IL SUONO ---
    public void PlaySelectionSound()
    {
        AudioManager.PlaySFX(audioSource, selectionSound);
    }

    // --- SELEZIONE (Aggiornata con Audio) ---
    public void SelezionaSospettato(string id, GameObject outlineObj)
    {
        // 1. Riproduci il suono quando clicchi
        PlaySelectionSound();

        // 2. Logica di selezione
        _idSospettatoSelezionato = id;

        if (_currentOutline != null) _currentOutline.SetActive(false);
        _currentOutline = outlineObj;
        if (_currentOutline != null) _currentOutline.SetActive(true);

        if (confirmButton != null) confirmButton.interactable = true;
    }

    // --- CONFERMA ---
    public void ConfermaAccusa()
    {
        if (string.IsNullOrEmpty(_idSospettatoSelezionato)) return;

        // 1. Registra l'esito nel GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.VerificaAccusa(_idSospettatoSelezionato);
        }

        // 2. Chiudi l'interfaccia
        Hide();

        // 3. Dì al Poliziotto di parlare!
        PolicemanNPC poliziotto = FindFirstObjectByType<PolicemanNPC>();
        if (poliziotto != null)
        {
            poliziotto.ShowVerdictDialogue();
        }
        else
        {
            Debug.LogError("Nessun PolicemanNPC trovato nella scena per mostrare il verdetto!");
        }
    }
    
    public void EsciDalMenu()
    {
        Hide();
    }
}