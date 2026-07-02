using UnityEngine;

/// <summary>
/// Trigger da posizionare fuori dalla porta della casa.
/// Quando il player lo attraversa, fa tornare l'NPC alla porta e chiude la porta.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HouseExitTrigger : MonoBehaviour
{
    [Header("Riferimenti")]
    [Tooltip("L'NPC di questa casa.")]
    public NPCInteraction npc;
    
    [Tooltip("La porta da chiudere.")]
    public DoorController door;

    [Header("Impostazioni")]
    [Tooltip("Ritardo prima di chiudere la porta (per far passare il player).")]
    public float doorCloseDelay = 1.5f;

    private void Awake()
    {
        // Assicurati che il collider sia un trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo il player attiva questo trigger
        if (!other.CompareTag("Player")) return;
        
        // Solo se il player era stato invitato
        if (npc == null || !npc.HasInvitedPlayer) return;
        
        Debug.Log("[HouseExitTrigger] Il player sta uscendo dalla casa.");
        
        // L'NPC torna alla porta
        npc.ReturnToDoor();
        npc.ResetInvitation();
        
        // Chiudi la porta dopo un ritardo
        if (door != null)
        {
            StartCoroutine(CloseDoorAfterDelay());
        }
    }

    private System.Collections.IEnumerator CloseDoorAfterDelay()
    {
        yield return new WaitForSeconds(doorCloseDelay);
        
        if (door != null && door.IsOpen)
        {
            door.ForceClose();
            Debug.Log("[HouseExitTrigger] Porta chiusa.");
        }
    }
}
