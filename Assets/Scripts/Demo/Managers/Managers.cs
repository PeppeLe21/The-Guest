using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerManager))]
[RequireComponent(typeof(InventoryManager))]
[RequireComponent(typeof(LLMManager))]
[RequireComponent(typeof(SpawnManager))]
[RequireComponent(typeof(DialogueManager))]
[RequireComponent(typeof(GameManager))]
public class Managers : MonoBehaviour
{
    // Static Access Points
    public static GameManager Game {get; private set;}
    public static PlayerManager Player {get; private set;}
    public static InventoryManager Inventory {get; private set;}
    public static LLMManager LLM {get; private set;}
    public static SpawnManager Spawner {get; private set;}
    public static DialogueManager Dialogue {get; private set;}

    // List of managers to iterate
    private List<IGameManager> _startSequence;
    
    public static Managers Instance { get; private set; }

    void Awake()
    {
        // Singleton Check
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Link static references
        Game = GetComponent<GameManager>();
        Player = GetComponent<PlayerManager>();
        Inventory = GetComponent<InventoryManager>();
        LLM = GetComponent<LLMManager>();
        Spawner = GetComponent<SpawnManager>();
        Dialogue = GetComponent<DialogueManager>();

        _startSequence = new List<IGameManager>();
        _startSequence.Add(Game); // Priorità alta se serve, o normale
        _startSequence.Add(Player);
        _startSequence.Add(Inventory);
        _startSequence.Add(LLM);
        _startSequence.Add(Spawner);
        _startSequence.Add(Dialogue); // Dopo Spawner

        StartCoroutine(StartupManagers());
    }

    private IEnumerator StartupManagers()
    {
        NetworkService network = new NetworkService();

        foreach (IGameManager manager in _startSequence)
        {
            manager.Startup(network);
        }

        yield return null;

        int numModules = _startSequence.Count;
        int numReady = 0;

        // Loop until all are started
        while (numReady < numModules)
        {
            int lastReady = numReady;
            numReady = 0;
            
            foreach (IGameManager manager in _startSequence)
            {
                if (manager.status == ManagerStatus.Started)
                {
                    numReady++;
                }
            }

            if (numReady > lastReady)
            {
                Debug.Log($"Progress: {numReady}/{numModules}");
            }
            
            yield return null;
        }
        
        Debug.Log("All Managers Started!");
        // Messenger.Broadcast(GameEvent.MANAGERS_STARTED); // Optional
    }
    private void OnDestroy()
    {
        // Solo il vero Singleton deve pulire la tabella eventi quando viene distrutto (es. chiusura gioco)
        if (Instance == this)
        {
            Messenger.Cleanup();
        }
    }
}
