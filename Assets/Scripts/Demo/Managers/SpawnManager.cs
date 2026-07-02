using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Definisce un singolo spawn: quale prefab spawnare in quale casa/punto
/// </summary>
[System.Serializable]
public class SpawnAssignment
{
    [Tooltip("ID della casa (deve corrispondere a HouseController.HouseID)")]
    public string houseID;

    [Tooltip("Nome del punto di spawn (senza prefisso SPAWN_). Es: 'NPC_Entrata'")]
    public string spawnPointID;

    [Tooltip("Il prefab da spawnare (NPC, Item, Weapon, ecc.)")]
    public GameObject prefabToSpawn;

    [Tooltip("Offset opzionale dalla posizione dello spawn point")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("Se true, usa la rotazione dello spawn point. Altrimenti usa quella del prefab.")]
    public bool useSpawnPointRotation = true;
}

/// <summary>
/// Definisce uno spawn RANDOM: un prefab da spawnare in una casa, 
/// in uno dei punti che corrispondono a un prefisso (scelto casualmente).
/// </summary>
[System.Serializable]
public class RandomHouseSpawn
{
    [Tooltip("ID della casa dove spawnare (deve corrispondere a HouseController.HouseID)")]
    public string houseID;

    [Tooltip("Il prefab da spawnare (es. arma del delitto, oggetto chiave)")]
    public GameObject prefab;

    [Tooltip("Prefisso dei punti di spawn validi (es. 'ITEM_' o 'WEAPON_'). Il punto verrà scelto casualmente tra tutti quelli che iniziano con questo prefisso.")]
    public string spawnPointPrefix = "ITEM_";

    [Tooltip("Se true, usa la rotazione dello spawn point. Altrimenti usa quella del prefab.")]
    public bool useSpawnPointRotation = true;
}

/// <summary>
/// Manager centrale per lo spawn di NPC e oggetti nelle case.
/// Configurabile dall'Inspector: associa prefab a punti di spawn specifici.
/// </summary>
public class SpawnManager : MonoBehaviour, IGameManager
{
    public ManagerStatus status { get; private set; }

    [Header("Configurazione Spawn Manuali")]
    [Tooltip("Lista degli spawn con posizione esatta")]
    public List<SpawnAssignment> initialSpawns = new List<SpawnAssignment>();

    [Header("Configurazione Spawn Random per Casa")]
    [Tooltip("Per ogni casa, spawna un oggetto in un punto casuale")]
    public List<RandomHouseSpawn> randomHouseSpawns = new List<RandomHouseSpawn>();

    [Header("Debug")]
    public bool debugMode = false;

    // Cache delle case trovate nella scena
    private Dictionary<string, HouseController> _houses = new Dictionary<string, HouseController>();

    // Riferimento al NetworkService (per coerenza con altri Manager)
    private NetworkService _network;

    // Lista degli oggetti spawnati (per eventuale cleanup/reset)
    private List<GameObject> _spawnedObjects = new List<GameObject>();

    public void Startup(NetworkService service)
    {
        Debug.Log("SpawnManager starting...");
        _network = service;

        // 1. Trova tutte le case nella scena
        FindAllHouses();

        // 2. Esegui gli spawn configurati
        PerformInitialSpawns();

        status = ManagerStatus.Started;
    }

    /// <summary>
    /// Cerca tutti i HouseController nella scena e li indicizza per ID
    /// </summary>
    private void FindAllHouses()
    {
        _houses.Clear();

        HouseController[] housesInScene = FindObjectsByType<HouseController>(FindObjectsSortMode.None);

        foreach (HouseController house in housesInScene)
        {
            if (string.IsNullOrEmpty(house.HouseID))
            {
                Debug.LogWarning($"HouseController su '{house.gameObject.name}' non ha un HouseID! Impostane uno.");
                continue;
            }

            if (_houses.ContainsKey(house.HouseID))
            {
                Debug.LogError($"HouseID duplicato: '{house.HouseID}'! Ogni casa deve avere un ID unico.");
                continue;
            }

            _houses[house.HouseID] = house;

            if (debugMode)
            {
                Debug.Log($"[SpawnManager] Registrata casa: '{house.HouseID}'");
            }
        }

        Debug.Log($"[SpawnManager] Trovate {_houses.Count} case nella scena.");
    }

    /// <summary>
    /// Esegue tutti gli spawn configurati nella lista initialSpawns
    /// </summary>
    private void PerformInitialSpawns()
    {
        int successCount = 0;
        int failCount = 0;

        foreach (SpawnAssignment assignment in initialSpawns)
        {
            if (TrySpawn(assignment))
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        Debug.Log($"[SpawnManager] Spawn manuali completati: {successCount} successi, {failCount} fallimenti.");
        
        // 3. Esegui spawn random per casa
        PerformRandomHouseSpawns();
    }

    /// <summary>
    /// Esegue gli spawn random: per ogni configurazione, sceglie un punto casuale nella casa
    /// </summary>
    private void PerformRandomHouseSpawns()
    {
        int successCount = 0;
        int failCount = 0;

        foreach (RandomHouseSpawn rhs in randomHouseSpawns)
        {
            // Validazione
            if (string.IsNullOrEmpty(rhs.houseID))
            {
                Debug.LogError("[SpawnManager] RandomHouseSpawn ha houseID vuoto!");
                failCount++;
                continue;
            }

            if (rhs.prefab == null)
            {
                Debug.LogError($"[SpawnManager] RandomHouseSpawn per casa '{rhs.houseID}' non ha prefab!");
                failCount++;
                continue;
            }

            // Trova la casa
            if (!_houses.TryGetValue(rhs.houseID, out HouseController house))
            {
                Debug.LogError($"[SpawnManager] Casa '{rhs.houseID}' non trovata per spawn random!");
                failCount++;
                continue;
            }

            // Ottieni un punto random con il prefisso specificato
            Transform randomPoint = house.GetRandomSpawnPoint(rhs.spawnPointPrefix);
            if (randomPoint == null)
            {
                Debug.LogError($"[SpawnManager] Nessun punto '{rhs.spawnPointPrefix}*' trovato in casa '{rhs.houseID}'!");
                failCount++;
                continue;
            }

            // Calcola posizione e rotazione
            Vector3 spawnPosition = randomPoint.position;
            Quaternion spawnRotation = rhs.useSpawnPointRotation
                ? randomPoint.rotation
                : rhs.prefab.transform.rotation;

            // Spawn!
            GameObject spawned = Instantiate(rhs.prefab, spawnPosition, spawnRotation);
            _spawnedObjects.Add(spawned);

            if (debugMode)
            {
                Debug.Log($"[SpawnManager] ✓ Spawn RANDOM: '{rhs.prefab.name}' in '{rhs.houseID}' @ punto '{randomPoint.name}'");
            }

            successCount++;
        }

        if (randomHouseSpawns.Count > 0)
        {
            Debug.Log($"[SpawnManager] Spawn random completati: {successCount} successi, {failCount} fallimenti.");
        }
    }

    /// <summary>
    /// Tenta di eseguire uno spawn. Ritorna true se riuscito.
    /// </summary>
    private bool TrySpawn(SpawnAssignment assignment)
    {
        // Validazione
        if (string.IsNullOrEmpty(assignment.houseID))
        {
            Debug.LogError("[SpawnManager] SpawnAssignment ha houseID vuoto!");
            return false;
        }

        if (string.IsNullOrEmpty(assignment.spawnPointID))
        {
            Debug.LogError($"[SpawnManager] SpawnAssignment per casa '{assignment.houseID}' ha spawnPointID vuoto!");
            return false;
        }

        if (assignment.prefabToSpawn == null)
        {
            Debug.LogError($"[SpawnManager] SpawnAssignment per '{assignment.houseID}/{assignment.spawnPointID}' non ha prefab!");
            return false;
        }

        // Trova la casa
        if (!_houses.TryGetValue(assignment.houseID, out HouseController house))
        {
            Debug.LogError($"[SpawnManager] Casa '{assignment.houseID}' non trovata nella scena!");
            return false;
        }

        // Trova il punto di spawn
        Transform spawnPoint = house.GetSpawnPoint(assignment.spawnPointID);
        if (spawnPoint == null)
        {
            return false; // HouseController già logga l'errore
        }

        // Calcola posizione e rotazione
        Vector3 spawnPosition = spawnPoint.position + assignment.positionOffset;
        Quaternion spawnRotation = assignment.useSpawnPointRotation
            ? spawnPoint.rotation
            : assignment.prefabToSpawn.transform.rotation;

        // Spawn!
        GameObject spawned = Instantiate(assignment.prefabToSpawn, spawnPosition, spawnRotation);
        _spawnedObjects.Add(spawned);

        if (debugMode)
        {
            Debug.Log($"[SpawnManager] ✓ Spawnato '{assignment.prefabToSpawn.name}' in " +
                      $"'{assignment.houseID}/{assignment.spawnPointID}' @ {spawnPosition}");
        }

        return true;
    }

    /// <summary>
    /// Spawn dinamico a runtime (per eventi di gioco)
    /// </summary>
    public GameObject SpawnAt(string houseID, string spawnPointID, GameObject prefab)
    {
        SpawnAssignment dynamicAssignment = new SpawnAssignment
        {
            houseID = houseID,
            spawnPointID = spawnPointID,
            prefabToSpawn = prefab,
            useSpawnPointRotation = true
        };

        if (TrySpawn(dynamicAssignment))
        {
            return _spawnedObjects[_spawnedObjects.Count - 1];
        }

        return null;
    }

    /// <summary>
    /// Ottiene una casa per ID (utile per altri sistemi)
    /// </summary>
    public HouseController GetHouse(string houseID)
    {
        _houses.TryGetValue(houseID, out HouseController house);
        return house;
    }

    /// <summary>
    /// Pulisce tutti gli oggetti spawnati (per reset livello)
    /// </summary>
    public void ClearAllSpawned()
    {
        foreach (GameObject obj in _spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        _spawnedObjects.Clear();
        Debug.Log("[SpawnManager] Tutti gli spawn sono stati rimossi.");
    }
}
