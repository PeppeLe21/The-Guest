using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    [Header("Configurazione Tutorial")]
    [TextArea(3, 5)]
    public string[] tutorialLines = new string[] 
    {
        "Benvenuto, Agente. La situazione è critica.",
        "Il Conte è stato assassinato stanotte. Dobbiamo trovare il colpevole prima dell'alba.",
        "Parla con i sospettati in casa. Interrogali e cerca di capire chi mente.",
        "Usa il tuo TACCUINO (Tasto TAB) per rileggere gli appunti.",
        "Cerca indizi in giro per la villa. Premi 'E' per interagire con oggetti e persone.",
        "Quando sei sicuro, torna da me per formulare l'accusa. Buon lavoro."
    };

    [Header("Controllers da Bloccare")]
    public MonoBehaviour playerMovement;    
    public MonoBehaviour cameraLook;        
    public NotebookManager notebookManager; 
    public WardrobeController wardrobeController;

    private GameObject policemanNPC;

    private void Start()
    {
        // Cerca il poliziotto nella scena
        PolicemanNPC cop = FindFirstObjectByType<PolicemanNPC>();
        if (cop != null) 
        {
            policemanNPC = cop.gameObject;
        }
        else
        {
            Debug.LogError("[TutorialManager] Poliziotto non trovato! Assicurati che ci sia un PolicemanNPC nella scena.");
        }

        // Avvia la sequenza con un leggero ritardo
        StartCoroutine(StartTutorialSequence());
    }

    private IEnumerator StartTutorialSequence()
    {
        // 1. Blocca i controlli
        SetControlsActive(false);

        // 2. Aspetta un attimo che il gioco carichi
        yield return new WaitForSeconds(1.0f);

        // 3. Inizia il primo dialogo
        if (policemanNPC != null && Managers.Dialogue != null)
        {
            ShowLine(0);
        }
        else
        {
            Debug.LogWarning("[TutorialManager] Impossibile avviare il tutorial: Manca Poliziotto o DialogueManager.");
            EndTutorial(); // Sblocca tutto se fallisce
        }
    }

    private void ShowLine(int index)
    {
        if (index < tutorialLines.Length)
        {
            // Mostra la linea corrente e configura il callback per la prossima (Next)
            System.Action next = () => ShowLine(index + 1);
            // Passa EndTutorial come callback di cancellazione (ESC)
            Managers.Dialogue.StartSimpleDialogue(policemanNPC, "AGENTE", tutorialLines[index], next, () => EndTutorial());
        }
        else
        {
            // Tutte le linee finite
            EndTutorial();
        }
    }

    private void EndTutorial()
    {
        Debug.Log("[TutorialManager] Tutorial completato.");
        SetControlsActive(true);
        
        // Disattiva questo script per non consumare risorse
        this.enabled = false;
    }

    private void SetControlsActive(bool isActive)
    {
        if (playerMovement != null) playerMovement.enabled = isActive;
        if (cameraLook != null) cameraLook.enabled = isActive;
        if (notebookManager != null) notebookManager.enabled = isActive;
        if (wardrobeController != null) wardrobeController.enabled = isActive;

        Cursor.lockState = isActive ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isActive;
    }
}