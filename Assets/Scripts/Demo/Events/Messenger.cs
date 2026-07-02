using System;
using System.Collections.Generic;
using UnityEngine;

public static class Messenger {
    // Defines a delegate type for events
    public delegate void BroadcastAction();
    public delegate void BroadcastAction<T>(T arg1);
    public delegate void BroadcastAction<T, U>(T arg1, U arg2);

    // Dictionary of event types and their listeners
    private static Dictionary<string, Delegate> eventTable = new Dictionary<string, Delegate>();

    // Mark as permanent across scene loads if needed, but static persists usually.
    public static void Cleanup()
    {
        eventTable.Clear();
    }

    public static void AddListener(string eventType, BroadcastAction handler) {
        OnListenerAdding(eventType, handler);
        eventTable[eventType] = (BroadcastAction)eventTable[eventType] + handler;
    }
    public static void AddListener<T>(string eventType, BroadcastAction<T> handler) {
        OnListenerAdding(eventType, handler);
        eventTable[eventType] = (BroadcastAction<T>)eventTable[eventType] + handler;
    }

    public static void RemoveListener(string eventType, BroadcastAction handler) {
        if (!eventTable.ContainsKey(eventType)) return; // Evento mai registrato, ignora
        OnListenerRemoving(eventType, handler);
        eventTable[eventType] = (BroadcastAction)eventTable[eventType] - handler;
        OnListenerRemoved(eventType);
    }
    public static void RemoveListener<T>(string eventType, BroadcastAction<T> handler) {
        if (!eventTable.ContainsKey(eventType)) return; // Evento mai registrato, ignora
        OnListenerRemoving(eventType, handler);
        eventTable[eventType] = (BroadcastAction<T>)eventTable[eventType] - handler;
        OnListenerRemoved(eventType);
    }

    public static void Broadcast(string eventType) {
        if (eventTable.ContainsKey(eventType)) {
            Delegate d;
            if (eventTable.TryGetValue(eventType, out d)) {
                BroadcastAction callback = d as BroadcastAction;
                if (callback != null) {
                    callback();
                } else {
                    Debug.LogError($"Broadcast signature mismatch for event: {eventType}");
                }
            }
        }
    }
    public static void Broadcast<T>(string eventType, T arg1) {
        if (eventTable.ContainsKey(eventType)) {
            Delegate d;
            if (eventTable.TryGetValue(eventType, out d)) {
                BroadcastAction<T> callback = d as BroadcastAction<T>;
                if (callback != null) {
                    callback(arg1);
                } else {
                    Debug.LogError($"Broadcast signature mismatch for event: {eventType}");
                }
            }
        }
    }

    // Safety checks
    private static void OnListenerAdding(string eventType, Delegate listenerBeingAdded) {
        if (!eventTable.ContainsKey(eventType)) {
            eventTable.Add(eventType, null);
        }
        Delegate d = eventTable[eventType];
        if (d != null && d.GetType() != listenerBeingAdded.GetType()) {
            Debug.LogError($"Attempting to add listener with inconsistent signature for event type {eventType}. Current listeners have type {d.GetType().Name} and adding {listenerBeingAdded.GetType().Name}.");
        }
    }
    private static void OnListenerRemoving(string eventType, Delegate listenerBeingRemoved) {
        if (eventTable.ContainsKey(eventType)) {
            Delegate d = eventTable[eventType];
            
            if (d == null) {
            } else if (listenerBeingRemoved == null) {
            } else if (d.GetType() != listenerBeingRemoved.GetType()) {
            }
        } 
    }
    private static void OnListenerRemoved(string eventType) {
        if (eventTable[eventType] == null) {
            eventTable.Remove(eventType);
        }
    }
}
