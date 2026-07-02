using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestisce una singola casa nella scena.
/// Scansiona i figli per trovare i punti di spawn definiti in Blender.
/// Convenzione: gli Empty in Blender devono chiamarsi "SPAWN_TIPO_Nome"
/// Esempio: "SPAWN_NPC_Entrata", "SPAWN_ITEM_Tavolo", "SPAWN_WEAPON_Camera"
/// </summary>
public class HouseController : MonoBehaviour
{
    [Header("Identificazione")]
    [Tooltip("ID unico per questa casa. Usalo nello SpawnManager per riferirla.")]
    public string HouseID = "House_01";

    [Header("Debug")]
    [Tooltip("Mostra i punti di spawn trovati nel log.")]
    public bool debugMode = false;

    // Dizionario interno: "NPC_Entrata" -> Transform del punto
    private Dictionary<string, Transform> _spawnPoints = new Dictionary<string, Transform>();

    // Flag per evitare race conditions se altri script chiamano i metodi prima di Awake
    private bool _initialized = false;

    void Awake()
    {
        if (!_initialized)
            ScanForSpawnPoints();
    }

    /// <summary>
    /// Scansiona tutti i figli (inclusi nipoti) cercando oggetti con prefisso "SPAWN_"
    /// </summary>
    public void ScanForSpawnPoints()
    {
        _spawnPoints.Clear();

        // GetComponentsInChildren include anche se stesso e tutti i discendenti
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        if (debugMode) Debug.Log($"[House {HouseID}] Inizio scansione su '{name}'. Figli trovati: {allChildren.Length}");

        foreach (Transform child in allChildren)
        {
            // if (debugMode) Debug.Log($" -> Analizzo: '{child.name}'"); // Scommenta se vuoi vedere TUTTO
            string cleanName = "";
            bool isValid = false;

            // Caso 1: Nome inizia con SPAWN_ (Standard consigliato)
            if (child.name.StartsWith("SPAWN_"))
            {
                cleanName = child.name.Substring(6); // Rimuovi "SPAWN_"
                isValid = true;
            }
            // Caso 2: Nome inizia direttamente con NPC_, ITEM_, WEAPON_ (Compatibilità Blender import)
            else if (child.name.StartsWith("NPC_") || child.name.StartsWith("ITEM_") || child.name.StartsWith("WEAPON_"))
            {
                cleanName = child.name; // Mantieni il nome così com'è (es. "NPC_StandPoint.002")
                isValid = true;
            }
            // Caso 3: Convenzione alternativa (ItemSpawnPoint, NPCSpawnPoint, WeaponSpawnPoint)
            else if (child.name.StartsWith("ItemSpawnPoint") || 
                     child.name.StartsWith("NPCSpawnPoint") || 
                     child.name.StartsWith("WeaponSpawnPoint"))
            {
                cleanName = child.name; // Mantieni il nome originale
                isValid = true;
            }

            if (isValid)
            {
                // Rimuovi eventuali suffissi numerici di Blender/Unity (es. ".002") se danno fastidio,
                // oppure lasciali per distinguere punti multipli. Per ora li lasciamo.
                
                if (!_spawnPoints.ContainsKey(cleanName))
                {
                    _spawnPoints[cleanName] = child;

                    if (debugMode)
                    {
                        Debug.Log($"[House {HouseID}] Trovato spawn point: '{cleanName}' @ {child.position}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[House {HouseID}] Spawn point duplicato o nome sovrapposto: '{cleanName}'. Usa nomi unici!");
                }
            }
        }
        
        _initialized = true;

        if (debugMode)
        {
            Debug.Log($"[House {HouseID}] Totale spawn points trovati: {_spawnPoints.Count}");
        }
    }

    /// <summary>
    /// Ottiene un punto di spawn per nome (senza prefisso SPAWN_)
    /// </summary>
    /// <param name="pointName">Es: "NPC_Entrata" o "ITEM_Tavolo"</param>
    /// <returns>Il Transform del punto, o la radice della casa se non trovato</returns>
    public Transform GetSpawnPoint(string pointName)
    {
        if (!_initialized) ScanForSpawnPoints();

        if (_spawnPoints.TryGetValue(pointName, out Transform point))
        {
            return point;
        }

        Debug.LogWarning($"[House {HouseID}] Punto di spawn '{pointName}' non trovato! " +
                         $"Verifica che in Blender esista un Empty chiamato 'SPAWN_{pointName}'");
        return transform; // Fallback alla radice della casa
    }

    /// <summary>
    /// Verifica se un punto di spawn esiste
    /// </summary>
    public bool HasSpawnPoint(string pointName)
    {
        if (!_initialized) ScanForSpawnPoints();
        return _spawnPoints.ContainsKey(pointName);
    }

    /// <summary>
    /// Ritorna tutti i nomi dei punti di spawn disponibili (per debug/editor)
    /// </summary>
    public string[] GetAllSpawnPointNames()
    {
        if (!_initialized) ScanForSpawnPoints();
        string[] names = new string[_spawnPoints.Count];
        _spawnPoints.Keys.CopyTo(names, 0);
        return names;
    }
    
    /// <summary>
    /// Ottiene un punto di spawn RANDOM tra quelli che iniziano con il prefisso specificato.
    /// Es: GetRandomSpawnPoint("ITEM_") restituisce uno tra ITEM_Tavolo, ITEM_Scaffale, etc.
    /// </summary>
    /// <param name="prefix">Prefisso da cercare (es. "ITEM_" o "WEAPON_")</param>
    /// <returns>Transform di un punto random, o null se nessuno trovato</returns>
    public Transform GetRandomSpawnPoint(string prefix)
    {
        if (!_initialized) ScanForSpawnPoints();

        List<Transform> matchingPoints = new List<Transform>();
        
        foreach (var kvp in _spawnPoints)
        {
            if (kvp.Key.StartsWith(prefix))
            {
                matchingPoints.Add(kvp.Value);
            }
        }
        
        if (matchingPoints.Count == 0)
        {
            Debug.LogWarning($"[House {HouseID}] Nessun punto di spawn trovato con prefisso '{prefix}'");
            return null;
        }
        
        // Scegli random
        int randomIndex = Random.Range(0, matchingPoints.Count);
        Transform chosen = matchingPoints[randomIndex];
        
        if (debugMode)
        {
            Debug.Log($"[House {HouseID}] Punto random scelto: '{chosen.name}' tra {matchingPoints.Count} disponibili");
        }
        
        return chosen;
    }
    
    /// <summary>
    /// Ritorna tutti i punti di spawn che iniziano con un certo prefisso
    /// </summary>
    public List<Transform> GetAllSpawnPointsWithPrefix(string prefix)
    {
        if (!_initialized) ScanForSpawnPoints();

        List<Transform> result = new List<Transform>();
        
        foreach (var kvp in _spawnPoints)
        {
            if (kvp.Key.StartsWith(prefix))
            {
                result.Add(kvp.Value);
            }
        }
        
        return result;
    }

    // Gizmo per visualizzare i punti nell'Editor
    void OnDrawGizmosSelected()
    {
        // Riscansiona se non in play mode (per preview in Editor)
        if (!Application.isPlaying)
        {
            ScanForSpawnPoints();
        }

        foreach (var kvp in _spawnPoints)
        {
            Gizmos.color = Color.cyan;
            if (kvp.Key.StartsWith("NPC"))
                Gizmos.color = Color.green;
            else if (kvp.Key.StartsWith("ITEM"))
                Gizmos.color = Color.yellow;
            else if (kvp.Key.StartsWith("WEAPON"))
                Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(kvp.Value.position, 0.3f);
            Gizmos.DrawLine(kvp.Value.position, kvp.Value.position + kvp.Value.forward * 0.5f);
        }
    }
}
