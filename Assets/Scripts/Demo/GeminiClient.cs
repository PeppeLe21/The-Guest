using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GeminiClient : MonoBehaviour, ILLMClient
{
    [Header("Gemini Settings")]
    // ⚠️ SICUREZZA: Non inserire mai la tua API key qui prima di fare commit su GitHub!
    // Inserisci la tua chiave direttamente nell'Inspector di Unity (campo "Api Key") a runtime,
    // oppure caricala da un file di configurazione locale escluso dal .gitignore.
    public string apiKey = "";
    public string model = "gemini-1.5-flash"; // Veloce e gratis
    
    // URL base per l'API REST di Google
    private string ApiUrl => $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

    // Implementazione dell'interfaccia
    public void SendChat(List<OllamaMessage> history, Action<string> onComplete)
    {
        StartCoroutine(PostRequest(history, onComplete));
    }

    private IEnumerator PostRequest(List<OllamaMessage> history, Action<string> onComplete)
    {
        // --- FIX SICUREZZA ---
        // Pulisce spazi vuoti involontari copiati nell'Inspector
        string cleanModel = model.Trim(); 
        string cleanKey = apiKey.Trim();

        // Ricostruiamo l'URL con i dati puliti
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{cleanModel}:generateContent?key={cleanKey}";
        
        // DEBUG: Stampa l'URL per vedere se è giusto (non mostrare questo log in pubblico se fai video!)
        Debug.Log($"[Gemini URL] Sto chiamando: {url}");

        GeminiBody body = ConvertHistoryToGemini(history);
        string json = JsonUtility.ToJson(body);
        
        byte[] jsonToSend = new UTF8Encoding().GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                // Qui leggiamo il messaggio di errore completo che ci manda Google
                Debug.LogError($"[Gemini Error] {www.responseCode} {www.error}\nDETTAGLI GOOGLE: {www.downloadHandler.text}");
                
                // Mostra un messaggio più chiaro in gioco
                if (www.responseCode == 404) 
                    onComplete?.Invoke("Errore Tecnico: Modello non trovato (controlla spazi nel nome).");
                else 
                    onComplete?.Invoke($"Errore Google: {www.error}");
            }
            else
            {
                string responseJson = www.downloadHandler.text;
                string finalText = ParseGeminiResponse(responseJson);
                onComplete?.Invoke(finalText);
            }
        }
    }

    // --- CONVERSIONE DATI (Ollama -> Gemini) ---
    
    private GeminiBody ConvertHistoryToGemini(List<OllamaMessage> history)
    {
        GeminiBody body = new GeminiBody();
        body.contents = new List<GeminiContent>();

        foreach (var msg in history)
        {
            // Gemini gestisce il "system" diversamente (system_instruction), 
            // ma per semplicità (Flash) possiamo metterlo come primo messaggio user o usare il campo dedicato.
            // Qui lo uniamo al primo user o usiamo system_instruction se supportato dal wrapper.
            // Per massima compatibilità REST semplice:
            
            string role = (msg.role == "assistant") ? "model" : "user";
            
            // Se è system, lo forziamo come user (spesso funziona meglio senza complicare il JSON)
            if (msg.role == "system") role = "user"; 

            // Creiamo il contenuto
            GeminiContent content = new GeminiContent();
            content.role = role;
            content.parts = new List<GeminiPart>();
            
            GeminiPart part = new GeminiPart();
            part.text = msg.content;
            content.parts.Add(part);

            body.contents.Add(content);
        }
        
        // Configurazione per forzare JSON (importante per il tuo sistema NPC!)
        body.generationConfig = new GeminiConfig();
        body.generationConfig.responseMimeType = "application/json"; 

        return body;
    }

    private string ParseGeminiResponse(string json)
    {
        try 
        {
            // Unity JsonUtility è limitato, usiamo un wrapper root
            GeminiResponseRoot root = JsonUtility.FromJson<GeminiResponseRoot>(json);
            if (root.candidates != null && root.candidates.Count > 0)
            {
                return root.candidates[0].content.parts[0].text;
            }
        }
        catch (Exception e) { Debug.LogError("Parse Error: " + e.Message); }
        return "";
    }

    // --- STRUTTURE DATI JSON PER GEMINI ---
    [Serializable] class GeminiBody { 
        public List<GeminiContent> contents; 
        public GeminiConfig generationConfig;
    }
    [Serializable] class GeminiContent { public string role; public List<GeminiPart> parts; }
    [Serializable] class GeminiPart { public string text; }
    [Serializable] class GeminiConfig { public string responseMimeType; } // "application/json"
    
    [Serializable] class GeminiResponseRoot { public List<GeminiCandidate> candidates; }
    [Serializable] class GeminiCandidate { public GeminiContent content; }
}