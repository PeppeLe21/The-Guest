using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Cinemachine;

public class WardrobeController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject wardrobePanel; 
    
    // Riferimento alla scritta fissa "TAB Travestimento"
    public GameObject travestimentoPromptUI;   

    [Header("Managers")]
    public CostumeManager costumeManager; 

    [System.Serializable]
    public struct CostumeButtonPair
    {
        public string costumeID;        // Es. "BadBoy"
        public GameObject outlineObj;   // L'oggetto "Selected_Outline" rosso dentro il bottone
    }

    [Header("Buttons Setup")]
    public List<CostumeButtonPair> costumeButtons; // Lista da riempire nell'Inspector
    public GameObject resetOutline; // Opzionale: contorno per il bottone "Abiti Civili"

    // --- NUOVO: SEZIONE AUDIO ---
    [Header("Audio Settings")]
    public AudioSource audioSource;       // Trascina qui l'AudioManager
    public AudioClip selectionSound;      // Trascina qui "UI-sound-8"
    // ----------------------------

    // Static property per accesso facile
    public static bool IsWardrobeOpen = false;

    private bool isMenuOpen = false; // Manteniamo questo per logica interna, o usiamo solo la static?
    // Usiamo la static per coerenza + un campo locale se necessario, ma sincronizziamoli.
    
    private CinemachineBrain _brain;

    // --- AUTOMATISMO VISIBILITÀ ---
    private void OnEnable()
    {
        if (travestimentoPromptUI != null) travestimentoPromptUI.SetActive(true);
    }

    private void OnDisable()
    {
        if (travestimentoPromptUI != null) travestimentoPromptUI.SetActive(false);
    }

    void Start()
    {
        if (wardrobePanel != null) wardrobePanel.SetActive(false);
        isMenuOpen = false;
        IsWardrobeOpen = false;
        
        // Trova la brain per bloccare la camera
        _brain = Transform.FindFirstObjectByType<CinemachineBrain>();

        if (travestimentoPromptUI != null) travestimentoPromptUI.SetActive(true);
    }

    void Update()
    {
        // GESTIONE VISIBILITÀ SCRITTA
        if (travestimentoPromptUI != null)
        {
            bool shouldBeHidden = DialogueUI.IsDialogueOpen || isMenuOpen || NotebookManager.IsNotebookOpen;
            
            if (travestimentoPromptUI.activeSelf == shouldBeHidden)
            {
                travestimentoPromptUI.SetActive(!shouldBeHidden);
            }
        }

        // ESC per chiudere il menu vestiti
        if (Input.GetKeyDown(KeyCode.Escape) && isMenuOpen)
        {
            ToggleMenu(); 
            return; 
        }

        if (Input.GetKeyDown(KeyCode.Tab) && !DialogueUI.IsDialogueOpen && !NotebookManager.IsNotebookOpen)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        IsWardrobeOpen = isMenuOpen; // Aggiorna static
        
        // Blocca/Sblocca Camera
        if (_brain != null) _brain.enabled = !isMenuOpen;

        if (wardrobePanel != null) wardrobePanel.SetActive(isMenuOpen);

        if (travestimentoPromptUI != null) travestimentoPromptUI.SetActive(!isMenuOpen);

        if (isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UpdateOutlines();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // --- FUNZIONE PER IL SUONO ---
    private void PlaySelectionSound()
    {
        AudioManager.PlaySFX(audioSource, selectionSound);
    }

    public void OnCostumeClicked(string costumeName)
    {
        // 1. Riproduci suono
        PlaySelectionSound();

        // 2. Logica costume
        bool success = costumeManager.EquipCostumeByName(costumeName);

        if (success)
        {
            UpdateOutlines();
        }
    }
    
    public void OnResetClicked()
    {
        // 1. Riproduci suono
        PlaySelectionSound();

        // 2. Logica reset
        costumeManager.EquipBase();
        UpdateOutlines();
    }

    private void UpdateOutlines()
    {
        string currentID = "";

        if (costumeManager.IsWearingBase)
        {
            currentID = "BASE"; 
        }
        else if (costumeManager.CurrentCostume != null)
        {
            currentID = costumeManager.CurrentCostume.gameObjectName;
        }

        foreach (var pair in costumeButtons)
        {
            if (pair.outlineObj != null)
            {
                bool isActive = (pair.costumeID == currentID);
                pair.outlineObj.SetActive(isActive);
            }
        }

        if (resetOutline != null)
        {
            resetOutline.SetActive(currentID == "BASE");
        }
    }
}