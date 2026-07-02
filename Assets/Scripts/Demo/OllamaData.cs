using System;
using System.Collections.Generic;

[Serializable]
public class OllamaMessage
{
    public string role; // "system", "user", o "assistant"
    public string content;

    public OllamaMessage(string _role, string _content)
    {
        role = _role;
        content = _content;
    }
}

// Usiamo questo per inviare la richiesta
[Serializable]
public class OllamaRequest
{
    public string model;
    public List<OllamaMessage> messages;
    public bool stream;
}

// Classe di supporto per leggere la risposta in streaming
[Serializable]
public class OllamaStreamResponse
{
    public OllamaMessage message;
    public bool done;
}