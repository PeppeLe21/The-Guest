using UnityEngine;

/// <summary>
/// Oggetto raccoglibile che sblocca un costume.
/// Implementa IInteractable per funzionare con il sistema di interazione esistente.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CostumePickup : MonoBehaviour, IInteractable
{
    [Header("Costume da Sbloccare")]
    [Tooltip("Trascina qui l'asset CostumeData che questo oggetto sblocca.")]
    public CostumeData costumeToUnlock;
    
    [Header("Feedback")]
    [Tooltip("Se true, distrugge l'oggetto dopo il pickup.")]
    public bool destroyOnPickup = true;
    
    [Tooltip("Suono da riprodurre al pickup (opzionale).")]
    public AudioClip pickupSound;

    private bool _alreadyPickedUp = false;

    /// <summary>
    /// Chiamato quando il player preme E.
    /// </summary>
    public void Interact(PlayerController player)
    {
        if (_alreadyPickedUp) return;
        if (costumeToUnlock == null)
        {
            Debug.LogError($"[CostumePickup] {name}: Nessun costume assegnato!");
            return;
        }
        
        // Trova il CostumeManager sul player
        CostumeManager manager = player.GetComponent<CostumeManager>();
        if (manager == null)
        {
            Debug.LogError("[CostumePickup] CostumeManager non trovato sul Player!");
            return;
        }
        
        // Verifica se già sbloccato
        if (manager.IsCostumeUnlocked(costumeToUnlock))
        {
            Debug.Log($"[CostumePickup] Costume '{costumeToUnlock.displayName}' già posseduto.");
            // Potresti mostrare un messaggio "Già posseduto" nella UI
            return;
        }
        
        // Sblocca il costume
        manager.UnlockCostume(costumeToUnlock);
        _alreadyPickedUp = true;
        
        // Feedback audio
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // TODO: Mostra popup UI "Hai trovato: {costumeToUnlock.displayName}!"
        Debug.Log($"[CostumePickup] Raccolto: {costumeToUnlock.displayName}");
        
        // Distruggi o disattiva l'oggetto
        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
