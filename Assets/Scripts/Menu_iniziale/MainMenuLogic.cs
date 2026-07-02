using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuLogic : MonoBehaviour
{
    [Header("Riferimenti Pannelli")]
    public GameObject menuContainer;      // Il gruppo coi tasti Play/Exit
    public GameObject settingsContainer;  // Il gruppo coi Settings
    public GameObject creditsContainer;   // Il gruppo coi Credits (NUOVO)
    
    [Header("Audio")]
    public AudioSource uiSource;
    public AudioClip clickSound;

    // --- START ---
    private void Start()
    {
        // Per sicurezza, all'avvio assicuriamoci che si veda solo il Menu Principale
        // (Utile se per sbaglio hai lasciato i Settings accesi nell'Editor)
        ShowMainMenu();
    }

    // --- LOGICA DI NAVIGAZIONE ---

    public void OpenSettings()
    {
        PlayClick();
        CloseAll(); // Spegne tutto prima di aprire
        settingsContainer.SetActive(true);
    }

    public void OpenCredits() // NUOVO: Apre i Credits
    {
        PlayClick();
        CloseAll(); // Spegne tutto prima di aprire
        creditsContainer.SetActive(true);
    }

    public void ShowMainMenu() // Funzione per il tasto "Back" (comune a Settings e Credits)
    {
        // Nota: Ho rimosso il PlayClick() qui SE lo hai già messo sul bottone Back nell'Inspector.
        // Se il bottone Back non ha il suono nell'evento OnClick, togli il commento alla riga sotto:
        // PlayClick(); 
        
        CloseAll(); // Spegne i pannelli aperti
        menuContainer.SetActive(true); // Riaccende il menu
    }

    // Funzione privata per "pulire" lo schermo
    private void CloseAll()
    {
        if(menuContainer != null) menuContainer.SetActive(false);
        if(settingsContainer != null) settingsContainer.SetActive(false);
        if(creditsContainer != null) creditsContainer.SetActive(false);
    }

    // --- LOGICA DI GIOCO ---
    
    public void StartGame()
    {
        PlayClick();
        SceneManager.LoadScene("IntroScene");
    }

    public void QuitGame()
    {
        PlayClick();
        Application.Quit();
    }

    public void PlayClick()
    {
        AudioManager.PlaySFX(uiSource, clickSound);
    }
}