using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour, IGameManager
{
    public ManagerStatus status { get; private set; }
    
    // --- Game State Data ---
    // Encapsulated data using properties
    [SerializeField] private int _health = 100;
    public int Health => _health;

    [SerializeField] private CostumeType _currentCostume = CostumeType.Poliziotto;
    public CostumeType CurrentCostume {
        get { return _currentCostume; }
        set { _currentCostume = value; } // Add validation or events here if needed
    }

    [SerializeField] private bool _hasEvidence = false;
    public bool HasEvidence {
        get { return _hasEvidence; }
        set { _hasEvidence = value; }
    }

    private NetworkService _network;

    public void Startup(NetworkService service)
    {
        Debug.Log("PlayerManager starting...");
        _network = service;
        
        // Simulation of a long setup (e.g. loading save file)
        status = ManagerStatus.Initializing;
        
        // Defaults
        _health = 100;
        
        // Done
        status = ManagerStatus.Started;
    }

    public void ChangeCostume(CostumeType newCostume) {
        _currentCostume = newCostume;
        Debug.Log($"[PlayerManager] Changed costume to {newCostume}");
    }
}
