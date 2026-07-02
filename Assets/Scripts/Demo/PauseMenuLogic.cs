using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuLogic : MonoBehaviour
{
    [Header("Pannelli")]
    public GameObject pausePanel;       
    public GameObject buttonsContainer; 
    public GameObject settingsPanel;    

    [Header("Impostazioni")]
    public string mainMenuScene = "Menu_iniziale"; // Assicurati sia il nome giusto
    public bool isPaused = false;

    [Header("Audio")]
    public AudioSource uiSource;
    public AudioClip clickSound;

    void Update()
    {
        // --- MODIFICA FONDAMENTALE ---
        // Se qualsiasi UI è aperta, NON fare nulla qui.
        // Lasciamo che siano loro a gestire l'ESC per chiudersi.
        bool isAccusationOpen = (AccusationUI.Instance != null && AccusationUI.Instance.IsOpen);
        
        // Controlla anche se il menu vestiti è aperto
        WardrobeController wc = FindFirstObjectByType<WardrobeController>();
        bool isWardrobeOpen = (wc != null && wc.wardrobePanel != null && wc.wardrobePanel.activeSelf);
        
        // NUOVO: Se DialogueUI ha già gestito ESC in questo frame, non fare nulla
        if (DialogueUI.EscHandledThisFrame) return;
        
        if (DialogueUI.IsDialogueOpen || NotebookManager.IsNotebookOpen || isAccusationOpen || isWardrobeOpen) return; 
        // -----------------------------

        // Se premo ESC (e non sto parlando con nessuno)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; 
        
        pausePanel.SetActive(true);
        buttonsContainer.SetActive(true); 
        settingsPanel.SetActive(false);   
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        PlayClick(); // Suona anche quando riprendi col tasto ESC
        isPaused = false;
        Time.timeScale = 1f;

        pausePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        PlayClick();
        if (buttonsContainer != null) buttonsContainer.SetActive(false); 
        settingsPanel.SetActive(true);     
    }

    public void CloseSettings()
    {
        PlayClick();
        settingsPanel.SetActive(false);
        if (buttonsContainer != null) buttonsContainer.SetActive(true); 
    }

    public void QuitToMenu()
    {
        PlayClick();
        Time.timeScale = 1f; 
        SceneManager.LoadScene(mainMenuScene);
    }

    private void PlayClick()
    {
        AudioManager.PlaySFX(uiSource, clickSound);
    }
}