using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// Utility class for raw network operations, independent of game logic
public class NetworkService {
    // Constant or config for valid JSON handling if needed

    // Example generic GET
    public IEnumerator GetRequest(string url, Action<string> callback) {
        using (UnityWebRequest result = UnityWebRequest.Get(url)) {
            yield return result.SendWebRequest();

            if (result.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"Network Error: {result.error}");
            } else {
                callback(result.downloadHandler.text);
            }
        }
    }

    // Example generic POST
    public IEnumerator PostJsonRequest(string url, string jsonData, Action<string> callback, Dictionary<string, string> headers = null) {
        using (UnityWebRequest request = new UnityWebRequest(url, "POST")) {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            if (headers != null) {
                foreach(var header in headers) {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"Network Error: {request.error} | Response: {request.downloadHandler.text}");
                // Optionally handle specific errors
                if (request.responseCode == 404) callback("Error 404: Not Found");
                else callback($"Error: {request.error}");
            } else {
                callback(request.downloadHandler.text);
            }
        }
    }
}
