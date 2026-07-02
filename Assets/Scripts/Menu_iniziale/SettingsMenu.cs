using UnityEngine;
using UnityEngine.UI;
using TMPro; // Fondamentale per i nuovi Dropdown
using System.Collections.Generic;

public class SettingsMenu : MonoBehaviour
{
    [Header("Riferimenti UI")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown windowModeDropdown;
    public Slider musicSlider;
    public Slider sfxSlider;

    // Lista fissa di risoluzioni supportate
    private readonly int[,] availableResolutions = new int[,]
    {
        { 1920, 1080 }, // Full HD
        { 1600, 900 },  // HD+
        { 1366, 768 },  // HD
        { 1280, 720 }   // HD Standard
    };
    
    private int defaultResolutionIndex = 0; // Default: 1920x1080

    [Header("Audio Feedback")]
    public AudioSource uiSource;        // Trascina qui il tuo AudioSource (GameManager o MenuManager)
    public AudioClip clickSound;        // Suono per Click, Toggle e Dropdown
    public AudioClip sliderTickSound;   // Suono breve per lo scorrimento (o usa lo stesso del click)

    private float lastSliderTime = 0f;  // Serve per non far impazzire l'audio dello slider

    void Start()
    {
        // --- 1. CONFIGURA RISOLUZIONI (LISTA FISSA) ---
        if (resolutionDropdown != null) 
        {
            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>
            {
                "1920 x 1080 (Full HD)",
                "1600 x 900 (HD+)",
                "1366 x 768",
                "1280 x 720 (HD Standard)"
            };
            
            // Trova la risoluzione corrente nella lista e allinea AudioManager
            int currentWidth = Screen.width;
            int currentHeight = Screen.height;
            
            // Cerca se corrisponde a quella salvata in AudioManager (se siamo tornati al menu)
            // Se AudioManager ha un indice valido che corrisponde alla risoluzione attuale, usalo.
            // Altrimenti cerca nella lista.
            
            for (int i = 0; i < availableResolutions.GetLength(0); i++)
            {
                if (availableResolutions[i, 0] == currentWidth && 
                    availableResolutions[i, 1] == currentHeight)
                {
                    defaultResolutionIndex = i;
                    break;
                }
            }
            
            // Se AudioManager ha un valore diverso da 0 (default) o se corrisponde, potremmo usarlo.
            // Ma per sicurezza, fidiamoci di Screen.width/height all'avvio del menu.
            AudioManager.ResolutionIndex = defaultResolutionIndex;

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = defaultResolutionIndex; 
            resolutionDropdown.RefreshShownValue();
        }

        if (windowModeDropdown != null)
        {
            // Determina l'indice basato sulla modalità attuale
            int currentModeIndex = 0;
            switch (Screen.fullScreenMode)
            {
                case FullScreenMode.ExclusiveFullScreen: currentModeIndex = 0; break;
                case FullScreenMode.FullScreenWindow: currentModeIndex = 1; break;
                case FullScreenMode.Windowed: currentModeIndex = 2; break;
            }
            
            AudioManager.WindowModeIndex = currentModeIndex;
            windowModeDropdown.value = currentModeIndex;
            windowModeDropdown.RefreshShownValue();
        }

        // --- 2. INIZIALIZZA VALORI ATTUALI ---
        
        // Controlliamo l'esistenza degli slider prima di usarli
        if (musicSlider != null) musicSlider.value = AudioManager.MusicVolume; 
        if (sfxSlider != null) sfxSlider.value = AudioManager.SFXVolume;
    }

    // --- FUNZIONI COLLEGATE AI DROPDOWN ---

    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex >= 0 && resolutionIndex < availableResolutions.GetLength(0))
        {
            int width = availableResolutions[resolutionIndex, 0];
            int height = availableResolutions[resolutionIndex, 1];
            
            Screen.SetResolution(width, height, Screen.fullScreenMode);
            
            // Salva nel manager statico
            AudioManager.ResolutionIndex = resolutionIndex;
            
            Debug.Log($"[Settings] Risoluzione impostata a: {width}x{height}");
        }
    }

    public void SetWindowMode(int index)
    {
        // L'ordine deve combaciare con quello che hai scritto nel Dropdown (Fullscreen, Borderless, Windowed)
        string modeName = "";
        switch (index)
        {
            case 0: 
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen; 
                modeName = "Exclusive FullScreen";
                break; 
            case 1: 
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow; 
                modeName = "Borderless Window";
                break;    
            case 2: 
                Screen.fullScreenMode = FullScreenMode.Windowed; 
                modeName = "Windowed";
                break;            
        }
        
        // Salva nel manager statico
        AudioManager.WindowModeIndex = index;
        
        Debug.Log($"[Settings] Modalità Finestra impostata a: {modeName} (Indice: {index})");
    }

    // --- FUNZIONI AUDIO ---
    
    public void SetMusicVolume(float volume)
    {
        // Usa l'AudioManager per controllare il volume della musica
        AudioManager.MusicVolume = volume;
        Debug.Log($"[Settings] Volume Musica impostato a: {volume:F2}");
    }

    public void SetSFXVolume(float volume)
    {
        // Usa l'AudioManager per controllare il volume degli SFX
        AudioManager.SFXVolume = volume;
        Debug.Log($"[Settings] Volume SFX impostato a: {volume:F2}");
    }

    // --- RESET ---
    public void ResetSettings()
    {
        PlayGeneralInteraction();
        Debug.Log("[Settings] Inizio Reset Impostazioni...");
        
        // RESET GRAFICO (Window Mode)
        if (windowModeDropdown != null) { SetWindowMode(0); windowModeDropdown.value = 0; }

        // RESET RISOLUZIONE
        if (resolutionDropdown != null)
        {
            SetResolution(defaultResolutionIndex);
            resolutionDropdown.value = defaultResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }

        // RESET AUDIO (Forziamo valore e slider)
        if (musicSlider != null)
        {
            musicSlider.value = 1f;
            SetMusicVolume(1f);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = 1f;
            SetSFXVolume(1f);
        }
        
        Debug.Log("[Settings] Impostazioni Resettate con successo (Default: FullScreen, 1920x1080, Vol 100%)");
    }

    public void PlayGeneralInteraction()
    {
        AudioManager.PlaySFX(uiSource, clickSound);
    }

    // Specifica per gli Slider (con limitatore di frequenza)
    public void PlaySliderTick()
    {
        // Suona solo se sono passati almeno 0.05 secondi dall'ultimo tick
        if (Time.unscaledTime - lastSliderTime > 0.05f)
        {
            AudioManager.PlaySFX(uiSource, sliderTickSound);
            lastSliderTime = Time.unscaledTime;
        }
    }
}