using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class EndGameUI : MonoBehaviour
{
    public static EndGameUI Instance;

    [Header("Pannello Principale")]
    public GameObject mainPanel;

    [Header("Immagini Risultato")]
    public GameObject winImage;
    public GameObject loseImage;

    [Header("Scene Settings")]
    public string menuSceneName = "Menu_iniziale";

    [Header("Audio Settings")]
    public AudioSource audioSource;       // Trascina qui l'AudioManager
    public AudioClip clickSound;          // Trascina qui "UI-sound-8"

    private void Awake()
    {
        if (Instance == null) Instance = this;
        
        if (mainPanel != null) mainPanel.SetActive(false);
        if (winImage != null) winImage.SetActive(false);
        if (loseImage != null) loseImage.SetActive(false);
    }

    public void ShowEnding(bool hasWon)
    {
        mainPanel.SetActive(true);

        if (hasWon)
        {
            if (winImage != null) winImage.SetActive(true);
            if (loseImage != null) loseImage.SetActive(false);
        }
        else
        {
            if (winImage != null) winImage.SetActive(false);
            if (loseImage != null) loseImage.SetActive(true);
        }

        // --- DISABILITA INTERFACCE DI GIOCO ---
        if (InteractionUI.Instance != null) InteractionUI.Instance.Hide();
        
        NotebookManager nm = FindFirstObjectByType<NotebookManager>();
        if (nm != null) 
        {
            if (nm.notebookPromptUI != null) nm.notebookPromptUI.SetActive(false);
            nm.enabled = false;
        }

        WardrobeController wc = FindFirstObjectByType<WardrobeController>();
        if (wc != null) 
        {
            if (wc.travestimentoPromptUI != null) wc.travestimentoPromptUI.SetActive(false);
            wc.enabled = false;
        }
        
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;
        // --------------------------------------

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Questa funzione è collegata al bottone Btn_Back
    public void TornaAlMenu()
    {
        // 1. Riproduci il suono (con volume SFX controllato)
        AudioManager.PlaySFX(audioSource, clickSound);

        // 2. Riattiva il tempo e carica la scena
        Time.timeScale = 1f; 
        SceneManager.LoadScene(menuSceneName);
    }
}