using UnityEngine;

/// <summary>
/// Manager statico per controllare i volumi globali del gioco.
/// Accessibile da qualsiasi script tramite AudioManager.SFXVolume e AudioManager.MusicVolume
/// </summary>
public static class AudioManager
{
    // ==================== SFX ====================
    private static float _sfxVolume = 1f;
    
    public static float SFXVolume
    {
        get => _sfxVolume;
        set => _sfxVolume = Mathf.Clamp01(value);
    }
    
    /// <summary>
    /// Riproduce un suono SFX con il volume corrente.
    /// </summary>
    public static void PlaySFX(AudioSource source, AudioClip clip)
    {
        if (source != null && clip != null)
        {
            source.PlayOneShot(clip, _sfxVolume);
        }
    }
    
    /// <summary>
    /// Riproduce un suono SFX con volume personalizzato (moltiplicato per SFXVolume).
    /// </summary>
    public static void PlaySFX(AudioSource source, AudioClip clip, float volumeMultiplier)
    {
        if (source != null && clip != null)
        {
            source.PlayOneShot(clip, _sfxVolume * volumeMultiplier);
        }
    }
    
    // ==================== MUSIC ====================
    private static float _musicVolume = 1f;
    private static AudioSource _musicSource;
    
    public static float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = Mathf.Clamp01(value);
            // Applica il volume direttamente alla sorgente musicale
            if (_musicSource != null)
            {
                _musicSource.volume = _musicVolume;
            }
        }
    }
    
    /// <summary>
    /// Registra l'AudioSource della musica così che AudioManager possa controllarne il volume.
    /// Chiamalo dallo script che gestisce la musica (es. MusicManager) nel suo Awake() o Start().
    /// </summary>
    public static void RegisterMusicSource(AudioSource source)
    {
        _musicSource = source;
        if (_musicSource != null)
        {
            _musicSource.volume = _musicVolume; // Applica subito il volume salvato
        }
    }
    
    /// </summary>
    public static void UnregisterMusicSource()
    {
        _musicSource = null;
    }

    // ==================== GRAPHICS ====================
    // Salviamo gli indici per coerenza tra Menu e Gioco
    public static int ResolutionIndex = 0; // Default Full HD (dipende dalla lista in SettingsMenu)
    public static int WindowModeIndex = 0; // Default Fullscreen

    /// <summary>
    /// Applica le impostazioni grafiche salvate.
    /// Da chiamare all'avvio del gioco (es. in GameManager o MainMenuLogic).
    /// </summary>
    public static void InitializeGraphics(int[,] resolutions)
    {
        // Applica Risoluzione
        if (ResolutionIndex >= 0 && ResolutionIndex < resolutions.GetLength(0))
        {
            int width = resolutions[ResolutionIndex, 0];
            int height = resolutions[ResolutionIndex, 1];
            Screen.SetResolution(width, height, Screen.fullScreenMode);
        }

        // Applica Window Mode
        switch (WindowModeIndex)
        {
            case 0: Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen; break;
            case 1: Screen.fullScreenMode = FullScreenMode.FullScreenWindow; break;
            case 2: Screen.fullScreenMode = FullScreenMode.Windowed; break;
        }
    }
}
