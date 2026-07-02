using UnityEngine;

/// <summary>
/// Attacca questo script all'AudioSource che riproduce la musica.
/// Si registrerà automaticamente con AudioManager per permettere il controllo del volume.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicSource : MonoBehaviour
{
    private AudioSource _audioSource;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        AudioManager.RegisterMusicSource(_audioSource);
    }

    void OnDestroy()
    {
        // Se questa è la sorgente registrata, pulisci il riferimento
        AudioManager.UnregisterMusicSource();
    }
}
