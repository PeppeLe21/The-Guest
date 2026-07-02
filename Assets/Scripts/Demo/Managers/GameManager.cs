using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour, IGameManager
{
    public static GameManager Instance;
    
    // IGameManager Implementation
    public ManagerStatus status { get; private set; }
    public void Startup(NetworkService service)
    {
        // Init logic
        status = ManagerStatus.Started;
    }

    [Header("Investigazione")]
    //Lista dei nomi delle persone con cui hai parlato
    public List<string> npcInterrogati = new List<string>();

    [Tooltip("Questo numero si aggiorna da solo in base alla lista sopra")]
    public int dialoghiCompletati = 0; 

    public int dialoghiMinimiRichiesti = 4;
    
    [Header("Soluzione")]
    public string idColpevoleReale = "Chef"; 
    public bool casoRisolto = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void RegistraContatto(string nomeNPC)
    {
        // Controlliamo se questo NPC è già nella lista
        if (!npcInterrogati.Contains(nomeNPC))
        {
            npcInterrogati.Add(nomeNPC);
            
            dialoghiCompletati = npcInterrogati.Count;
            
            Debug.Log($"✅ Nuovo sospettato interrogato: {nomeNPC}. Totale: {dialoghiCompletati}/{dialoghiMinimiRichiesti}");
        }
        else
        {
            Debug.Log($"⚠️ Hai già interrogato {nomeNPC}. Non conta per il progresso.");
        }
    }

    public bool VerificaAccusa(string idSospettato)
    {
        casoRisolto = (idSospettato == idColpevoleReale);
        return casoRisolto;
    }
}