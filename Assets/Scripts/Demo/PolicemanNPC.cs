using UnityEngine;

public class PolicemanNPC : MonoBehaviour, IInteractable
{
    [Header("Dialoghi")]
    [TextArea] public string notReadyDialogue = "Agente, non hai ancora abbastanza prove. Torna quando avrai interrogato tutti.";
    [TextArea] public string readyDialogue = "Bene Agente. Abbiamo raccolto i dati. Sei pronto per dirmi chi è l'assassino?";
    
    [Header("Dialoghi Finali")]
    [TextArea] public string winDialogue = "ECCELLENTE! Le prove confermano tutto. Lo portiamo via. Ottimo lavoro Agente!";
    [TextArea] public string loseDialogue = "SBAGLIATO! Ha un alibi di ferro. Il vero colpevole è scappato... Sei licenziato!";

    public void Interact(PlayerController player)
    {
        // Caso 1: ABBIAMO ABBASTANZA PROVE -> PARLA IL POLIZIOTTO -> POI APRIAMO UI ACCUSA
        if (GameManager.Instance != null && GameManager.Instance.dialoghiCompletati >= GameManager.Instance.dialoghiMinimiRichiesti)
        {
            if (Managers.Dialogue != null)
            {
                // Mostra il dialogo. QUANDO FINISCE (callback), apri la UI di Accusa
                Managers.Dialogue.StartSimpleDialogue(this.gameObject, "AGENTE", readyDialogue, () => 
                {
                    if (AccusationUI.Instance != null) AccusationUI.Instance.Show();
                });
            }
        }
        // Caso 2: NON SIAMO PRONTI -> PARLA IL POLIZIOTTO
        else
        {
            if (Managers.Dialogue != null)
            {
                Managers.Dialogue.StartSimpleDialogue(this.gameObject, "AGENTE", notReadyDialogue);
            }
        }
    }

    // --- CHIAMATA DALLA UI DOPO L'ACCUSA ---
    // --- CHIAMATA DALLA UI DOPO L'ACCUSA ---
    public void ShowVerdictDialogue()
    {
        if (GameManager.Instance == null) return;

        bool hoVinto = GameManager.Instance.casoRisolto;
        string fraseFinale = hoVinto ? winDialogue : loseDialogue;

        // Fai partire il dialogo finale
        if (Managers.Dialogue != null)
        {
            // --- MODIFICA QUI ---
            // Aggiungiamo la parentesi graffa finale '() => { ... }'
            // Questo codice viene eseguito sia su INVIO (End) che su ESC (Cancel) per evitare blocchi
            System.Action endingLogic = () => 
            {
                // Ora che il dialogo è chiuso, apriamo il pannello finale
                if (EndGameUI.Instance != null)
                {
                    EndGameUI.Instance.ShowEnding(hoVinto);
                }
                else
                {
                    Debug.LogError("EndGameUI non trovato! Hai collegato lo script a Canvas_EndGame?");
                }
            };

            Managers.Dialogue.StartSimpleDialogue(this.gameObject, "AGENTE", fraseFinale, endingLogic, endingLogic);
            // -------------------
        }
        
        Debug.Log($"VERDETTO: {(hoVinto ? "VITTORIA" : "SCONFITTA")}");
    }
}