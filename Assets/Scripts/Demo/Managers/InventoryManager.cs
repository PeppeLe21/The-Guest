using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour, IGameManager
{
    public ManagerStatus status { get; private set; }
    
    private Dictionary<string, int> _items; 
    private NetworkService _network;

    public void Startup(NetworkService service)
    {
        Debug.Log("InventoryManager starting...");
        _network = service;
        
        _items = new Dictionary<string, int>();

        // Async init simulation if needed
        status = ManagerStatus.Started;
    }

    public void AddItem(string itemName) {
        if (_items.ContainsKey(itemName)) {
            _items[itemName] += 1;
        } else {
            _items[itemName] = 1;
        }
        Debug.Log($"Items: {itemName} added.");
    }

    public bool HasItem(string itemName) {
        return _items.ContainsKey(itemName);
    }
}
