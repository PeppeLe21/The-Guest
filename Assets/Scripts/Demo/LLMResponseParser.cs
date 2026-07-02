using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Parser dedicato per le risposte LLM in formato JSON.
/// Estrae reply, delta e action dalle risposte dell'IA.
/// </summary>
public static class LLMResponseParser
{
    /// <summary>
    /// Effettua il parsing di una risposta LLM in formato JSON.
    /// Schema atteso: {"reply":"...", "delta":0, "action":"SMALL_TALK"}
    /// </summary>
    public static NPCResponseData Parse(string rawResponse)
    {
        // 1. Pulisci la risposta dai pensieri interni (<think>)
        string cleanResponse = RemoveThinkBlocks(rawResponse);
        
        var data = new NPCResponseData
        {
            action = "SMALL_TALK",
            reply = cleanResponse, // Fallback se il parsing fallisce
            faith_delta = 0
        };

        if (string.IsNullOrWhiteSpace(cleanResponse))
        {
            return data;
        }

        // Rimuovi eventuali blocchi di codice markdown
        string cleanJson = cleanResponse
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();

        try
        {
            // PARSING JSON con Regex (robusto per vari formati)
            
            // 1. Estrai "reply" - cerca il valore tra virgolette dopo "reply":
            var matchReply = Regex.Match(cleanJson, 
                @"""reply""\s*:\s*""((?:[^""\\]|\\.)*)""", 
                RegexOptions.Singleline);
            
            if (matchReply.Success && matchReply.Groups.Count > 1)
            {
                // Decodifica escape sequences
                data.reply = UnescapeJsonString(matchReply.Groups[1].Value);
            }

            // 2. Estrai "action"
            var matchAction = Regex.Match(cleanJson, 
                @"""action""\s*:\s*""(\w+)""", 
                RegexOptions.IgnoreCase);
            
            if (matchAction.Success && matchAction.Groups.Count > 1)
            {
                data.action = matchAction.Groups[1].Value.ToUpper().Trim();
            }

            // 3. Estrai "delta" (nuovo nome) o "faith_delta" (vecchio nome per retrocompatibilità)
            var matchDelta = Regex.Match(cleanJson, 
                @"""(?:delta|faith_delta)""\s*:\s*(-?\d+)");
            
            if (matchDelta.Success && matchDelta.Groups.Count > 1)
            {
                int.TryParse(matchDelta.Groups[1].Value, out data.faith_delta);
            }
            
            // Debug: logga il parsing
            Debug.Log($"[Parser] reply={data.reply.Substring(0, Mathf.Min(30, data.reply.Length))}... delta={data.faith_delta} action={data.action}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LLMResponseParser] Errore parsing JSON: {e.Message}\nInput: {cleanJson}");
        }

        return data;
    }
    
    /// <summary>
    /// Decodifica le escape sequences JSON (\n, \", etc.)
    /// </summary>
    private static string UnescapeJsonString(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        
        return s
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t")
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\");
    }

    /// <summary>
    /// Rimuove i blocchi <think>...</think> generati da alcuni modelli LLM.
    /// </summary>
    public static string RemoveThinkBlocks(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // 1. Rimuovi blocchi completi <think>...</think> (anche su più righe)
        string patternComplete = @"<think>.*?</think>";
        string result = Regex.Replace(text, patternComplete, "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // 2. Rimuovi blocchi <think> aperti ma NON chiusi (caso troncato o errore)
        string patternUnclosed = @"<think>.*";
        result = Regex.Replace(result, patternUnclosed, "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // 3. Rimuovi eventuali residui di chiusura orfani (caso raro)
        result = result.Replace("</think>", "");

        return result.Trim();
    }

    /// <summary>
    /// Pulisce completamente una risposta LLM rimuovendo tutti gli artefatti.
    /// </summary>
    public static string CleanResponse(string rawResponse)
    {
        string cleaned = RemoveThinkBlocks(rawResponse);
        cleaned = cleaned.TrimStart('-', ' ', '\n', '\r');
        return cleaned;
    }
}
