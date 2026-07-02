using UnityEngine;
using UnityEditor;
using System.IO;

public class PrefabMaker : EditorWindow
{
    [MenuItem("Tools/Converti OBJ in Prefab (con Collider)")]
    static void CreatePrefabs()
    {
        // Prende tutti gli oggetti selezionati nel Project
        Object[] selectedObjects = Selection.objects;

        // Crea una cartella per i risultati se non esiste
        string path = "Assets/ImportedAssets/GeneratedPrefabs";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        foreach (Object obj in selectedObjects)
        {
            if (obj is GameObject)
            {
                // 1. Istanzia l'oggetto in memoria
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(obj);
                
                // 2. Aggiunge il Box Collider se non c'è
                if (go.GetComponent<Collider>() == null)
                    go.AddComponent<BoxCollider>();

                // 3. Salva come nuovo Prefab
                string localPath = path + "/" + obj.name + ".prefab";
                localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
                PrefabUtility.SaveAsPrefabAsset(go, localPath);

                // 4. Distrugge l'oggetto temporaneo
                DestroyImmediate(go);
            }
        }
        Debug.Log("Fatto! Controlla la cartella 'Assets/ImportedAssets/GeneratedPrefabs'");
    }
}