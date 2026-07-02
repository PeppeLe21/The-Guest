using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GroqClient : MonoBehaviour, ILLMClient
{
    [Header("Groq Settings")]
    // ⚠️ SICUREZZA: Non inserire mai la tua API key qui prima di fare commit su GitHub!
    // Inserisci la chiave Groq (inizia con gsk_) direttamente nell'Inspector di Unity.
    public string apiKey = ""; 
    
    // ID Ufficiale Groq per Qwen 3 (aggiornato 2025)
    public string model = "moonshotai/kimi-k2-instruct"; 
    
    private string apiUrl = "https://api.groq.com/openai/v1/chat/completions";

    // Modello secondario per riassunti (più veloce/economico/affidabile per testi semplici)
    public string summaryModel = "llama-3.3-70b-versatile"; 

    // --- Implementazione Interfaccia ---
    public void SendChat(List<OllamaMessage> history, Action<string> onComplete)
    {
        StartCoroutine(PostRequest(history, this.model, onComplete));
    }

    // --- NUOVA FUNZIONE PER RIASSUNTI ---
    /// <summary>
    /// Usa un modello specifico (Llama 3) per generare i riassunti del taccuino,
    /// separando la logica dal modello principale (Qwen/Deepseek).
    /// </summary>
    public void SendSummaryChat(List<OllamaMessage> history, Action<string> onComplete)
    {
        Debug.Log($"[GroqClient] Generazione riassunto con modello: {summaryModel}");
        StartCoroutine(PostRequest(history, summaryModel, onComplete));
    }

    private IEnumerator PostRequest(List<OllamaMessage> history, string modelToUse, Action<string> onComplete)
    {
        // 1. Pulizia e Setup
        string cleanKey = apiKey.Trim();
        if (string.IsNullOrEmpty(cleanKey))
        {
            Debug.LogError("❌ Manca la API Key di Groq!");
            onComplete?.Invoke("Errore: Configura la API Key nell'Inspector.");
            yield break;
        }

        // 2. Costruzione del JSON (Formato OpenAI-compatibile usato da Groq)
        StringBuilder jsonBuilder = new StringBuilder();
        jsonBuilder.Append("{");
        jsonBuilder.Append($"\"model\":\"{modelToUse}\","); // Usa il modello passato come argomento
        jsonBuilder.Append("\"messages\":[");
        
        for (int i = 0; i < history.Count; i++)
        {
            jsonBuilder.Append("{");
            jsonBuilder.Append($"\"role\":\"{history[i].role}\",");
            // Nota: EscapeJsonString aggiunge già le virgolette all'inizio e alla fine
            jsonBuilder.Append($"\"content\":{EscapeJsonString(history[i].content)}"); 
            jsonBuilder.Append("}");
            if (i < history.Count - 1) jsonBuilder.Append(",");
        }
        
        jsonBuilder.Append("],");
        jsonBuilder.Append("\"temperature\":0.5,"); // Temperature un po' più bassa per riassunti precisi
        jsonBuilder.Append("\"max_tokens\":512");
        jsonBuilder.Append("}");

        string json = jsonBuilder.ToString();
        
        // LOGGA LA RICHIESTA 
        // Debug.Log($"[Groq -> {modelToUse}]: {json}");
        
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        // 3. Invio Richiesta
        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();

            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + cleanKey);

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                // Gestione Errori Specifica
                if (www.responseCode == 401) Debug.LogError("❌ Errore 401: API Key non valida o scaduta.");
                else if (www.responseCode == 429) Debug.LogError("⚠️ Errore 429: Troppe richieste (Rate Limit). Rallenta.");
                else Debug.LogError($"❌ Groq Error {www.responseCode}: {www.error}\nResponse: {www.downloadHandler.text}");

                onComplete?.Invoke($"Errore NPC: {www.error}");
            }
            else
            {
                // 4. Parsing Risposta
                string responseJson = www.downloadHandler.text;
                string content = ParseResponse(responseJson); // Usiamo il metodo esistente
                onComplete?.Invoke(content);
            }
        }
    }

    // --- Helpers di Conversione ---

    private List<GroqMessage> ConvertHistory(List<OllamaMessage> history)
    {
        // Convertiamo la tua classe history in quella che piace a Groq
        List<GroqMessage> converted = new List<GroqMessage>();
        foreach(var msg in history)
        {
            converted.Add(new GroqMessage 
            { 
                role = msg.role, 
                content = msg.content 
            });
        }
        return converted;
    }
    
    /// <summary>
    /// Escapa una stringa per l'uso all'interno di JSON.
    /// </summary>
    private string EscapeJsonString(string s)
    {
        if (string.IsNullOrEmpty(s)) return "\"\"";
        
        StringBuilder sb = new StringBuilder();
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }

    private string ParseResponse(string json)
    {
        try 
        {
            GroqResponseRoot root = JsonUtility.FromJson<GroqResponseRoot>(json);
            if (root.choices != null && root.choices.Count > 0)
            {
                return root.choices[0].message.content;
            }
        }
        catch (Exception e) 
        { 
            Debug.LogError($"JSON Parse Error: {e.Message}"); 
        }
        return "";
    }

    // --- Strutture Dati JSON (Groq/OpenAI Standard) ---

    [Serializable]
    private class RequestPacket
    {
        public string model;
        public List<GroqMessage> messages;
        public float temperature;
        public int max_tokens;
    }

    [Serializable]
    private class GroqMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class GroqResponseRoot
    {
        public List<GroqChoice> choices;
    }

    [Serializable]
    private class GroqChoice
    {
        public GroqMessage message;
    }
}