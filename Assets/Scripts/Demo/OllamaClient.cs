using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OllamaClient : MonoBehaviour, ILLMClient
{
    public static OllamaClient Instance { get; private set; }

    [Header("Ollama Settings")]
    public string baseUrl = "http://localhost:11434/api/chat";
    // QUI DEFINIAMO IL MODELLO CENTRALE
    public string defaultModel = "gemma3:4b"; 

    [Header("Warmup")]
    public bool warmupOnStart = true;
    public int warmupTimeoutSeconds = 60;

    private bool _warmupDone = false;
    private bool _isWarmingUp = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (warmupOnStart)
            WarmUp(null); // Usa il default
    }

    public void WarmUp(string modelToUse)
    {
        if (_warmupDone || _isWarmingUp) return;
        // Se modelToUse è nullo, usa defaultModel
        string targetModel = string.IsNullOrEmpty(modelToUse) ? defaultModel : modelToUse;
        StartCoroutine(WarmUpCoroutine(targetModel));
    }

    private IEnumerator WarmUpCoroutine(string modelToUse)
    {
        _isWarmingUp = true;
        Debug.Log($"[Ollama] Inizio Warmup modello: {modelToUse}...");

        var req = new OllamaRequest
        {
            model = modelToUse,
            stream = false,
            messages = new List<OllamaMessage>
            {
                new OllamaMessage("system", "Warmup. Rispondi OK."),
                new OllamaMessage("user", "Start")
            }
        };

        string json = JsonUtility.ToJson(req);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest(baseUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = warmupTimeoutSeconds;

            yield return www.SendWebRequest();

            _isWarmingUp = false;

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Ollama Warmup] Fallito: {www.error}");
                yield break;
            }

            _warmupDone = true;
            Debug.Log($"[Ollama Warmup] COMPLETATO. Modello pronto: {modelToUse}");
        }
    }

    // Modificato per gestire il default automaticamente
    public void SendChatStream(List<OllamaMessage> chatHistory, string modelToUse, Action<string> onPartial, Action<string> onComplete)
    {
        // Se passiamo null o stringa vuota, usiamo il defaultModel definito in alto
        string targetModel = string.IsNullOrEmpty(modelToUse) ? defaultModel : modelToUse;
        StartCoroutine(SendChatStreamCoroutine(chatHistory, targetModel, onPartial, onComplete));
    }

    private IEnumerator SendChatStreamCoroutine(List<OllamaMessage> history, string modelToUse, Action<string> onPartial, Action<string> onComplete)
    {
        var req = new OllamaRequest
        {
            model = modelToUse,
            stream = true,
            messages = history
        };

        string json = JsonUtility.ToJson(req);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest(baseUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            var asyncOp = www.SendWebRequest();
            StringBuilder fullReply = new StringBuilder();
            string previousText = "";

            while (!www.isDone)
            {
                if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"[Ollama] Error: {www.error}");
                    onComplete?.Invoke($"[ERRORE DI CONNESSIONE]");
                    yield break;
                }

                string current = www.downloadHandler.text;
                if (current.Length > previousText.Length)
                {
                    string newPart = current.Substring(previousText.Length);
                    previousText = current;
                    ParseStreamChunk(newPart, fullReply, onPartial);
                }
                yield return null;
            }

            string finalText = www.downloadHandler.text;
            if (finalText.Length > previousText.Length)
            {
                ParseStreamChunk(finalText.Substring(previousText.Length), fullReply, onPartial);
            }

            onComplete?.Invoke(fullReply.ToString());
        }
    }

    private void ParseStreamChunk(string chunkText, StringBuilder fullReply, Action<string> onPartial)
    {
        string[] lines = chunkText.Split('\n');
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            try
            {
                var chunk = JsonUtility.FromJson<OllamaStreamResponse>(line);
                if (chunk != null && chunk.message != null && !string.IsNullOrEmpty(chunk.message.content))
                {
                    fullReply.Append(chunk.message.content);
                    onPartial?.Invoke(fullReply.ToString());
                }
            }
            catch { /* Ignora JSON parziali */ }
        }
    }
    public void SendChat(List<OllamaMessage> history, Action<string> onComplete)
    {
        // Chiamiamo la tua vecchia coroutine stream, ma usiamo solo onComplete
        // (Gemini REST è più semplice non-stream per ora, uniformiamo così)
        SendChatStream(history, defaultModel, null, onComplete);
    }
}