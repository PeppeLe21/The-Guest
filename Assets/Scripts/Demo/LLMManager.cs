using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(OllamaClient))]
[RequireComponent(typeof(GeminiClient))]
[RequireComponent(typeof(GroqClient))]
public class LLMManager : MonoBehaviour, IGameManager
{
    // NOTA: Accessibile globalmente via Managers.LLM
    
    public ManagerStatus status { get; private set; }

    [Header("Impostazioni Provider")]
    [Tooltip("Scegli qui quale cervello usare per questa partita")]
    public AIProvider currentProvider = AIProvider.GroqFast; 
    
    // References
    private OllamaClient _ollamaClient;
    private GeminiClient _geminiClient; // In future refactor, these could use NetworkService
    private GroqClient _groqClient;

    private NetworkService _network; // Injected service

    public void Startup(NetworkService service)
    {
         Debug.Log("LLMManager starting...");
         _network = service;

         // Get Dependencies
         _ollamaClient = GetComponent<OllamaClient>();
         _geminiClient = GetComponent<GeminiClient>();
         _groqClient = GetComponent<GroqClient>();
         
         status = ManagerStatus.Started;
    }

    public void SendChat(List<OllamaMessage> history, System.Action<string> onComplete)
    {
        if (status != ManagerStatus.Started) {
             Debug.LogError("LLMManager not ready!");
             return;
        }

        ILLMClient activeClient = null;

        switch (currentProvider)
        {
            case AIProvider.OllamaLocal:
                activeClient = _ollamaClient;
                break;
            case AIProvider.GeminiGoogle:
                activeClient = _geminiClient;
                break;
            case AIProvider.GroqFast:
                activeClient = _groqClient;
                break;
        }
        
        if (activeClient == null)
        {
            Debug.LogError($"ERRORE CRITICO: Hai selezionato {currentProvider} ma manca lo script componente sul GameObject!");
            onComplete?.Invoke("Errore Sistema: Modello IA non trovato.");
            return;
        }

        Debug.Log($"[LLM Manager] Invio richiesta a: {currentProvider}");
        activeClient.SendChat(history, onComplete);
    }
    public void SendSummaryChat(List<OllamaMessage> history, System.Action<string> onComplete)
    {
        if (currentProvider == AIProvider.GroqFast && _groqClient != null)
        {
            // Usa il client Groq ottimizzato per i riassunti
            _groqClient.SendSummaryChat(history, onComplete);
        }
        else
        {
            // Fallback sugli altri provider (usano il metodo standard)
            SendChat(history, onComplete);
        }
    }
}