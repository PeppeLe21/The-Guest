using UnityEngine;

/// <summary>
/// Questo script deve essere attaccato al GameObject della Virtual Camera dedicata ai dialoghi (nella scena o nel prefab).
/// Il suo compito è registrarsi presso il DialogueManager (persistente) ogni volta che viene caricata la scena.
/// </summary>
public class AutoRegisterDialogueCamera : MonoBehaviour
{
    private void Start()
    {
        // Verifica che il manager esista (sicurezza per test isolati)
        if (Managers.Dialogue != null)
        {
            Managers.Dialogue.RegisterDialogueCamera(this.gameObject);
        }
        else
        {
            Debug.LogWarning("[AutoRegisterDialogueCamera] DialogueManager non trovato! Assicurati di avviare il gioco dalla scena corretta o che Managers sia presente.");
        }
    }
}
