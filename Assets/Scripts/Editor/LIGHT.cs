using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GIDetectiveWindow : EditorWindow
{
    private Vector2 _scroll;
    private List<Entry> _entries = new List<Entry>();
    private bool _onlySuspects = true;
    private int _triThreshold = 200_000;
    private float _boundsThreshold = 500f; // dimensione bounds (metri) oltre cui segnare come sospetto

    [MenuItem("Tools/GI Detective")]
    public static void Open() => GetWindow<GIDetectiveWindow>("GI Detective");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Scansiona la scena e trova oggetti sospetti per Lightmapping (AddGeometry).", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(8);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Scan Scene", GUILayout.Height(28)))
                Scan();

            if (GUILayout.Button("Select Suspects", GUILayout.Height(28)))
                SelectSuspects();

            if (GUILayout.Button("Disable Suspects", GUILayout.Height(28)))
                SetSuspectsEnabled(false);

            if (GUILayout.Button("Enable All Found", GUILayout.Height(28)))
                SetAllFoundEnabled(true);
        }

        EditorGUILayout.Space(8);
        _onlySuspects = EditorGUILayout.ToggleLeft("Mostra solo sospetti", _onlySuspects);
        _triThreshold = EditorGUILayout.IntField("Soglia triangoli (sospetto se sopra)", _triThreshold);
        _boundsThreshold = EditorGUILayout.FloatField("Soglia Bounds size (sospetto se sopra)", _boundsThreshold);

        EditorGUILayout.Space(8);

        if (_entries.Count == 0)
        {
            EditorGUILayout.HelpBox("Premi 'Scan Scene' per analizzare la scena.", MessageType.Info);
            return;
        }

        var list = _onlySuspects ? _entries.Where(e => e.IsSuspect).ToList() : _entries;

        EditorGUILayout.LabelField($"Trovati: {_entries.Count} | Visualizzati: {list.Count} | Sospetti: {_entries.Count(e => e.IsSuspect)}");

        EditorGUILayout.Space(6);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (var e in list.OrderByDescending(x => x.SeverityScore))
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Ping", GUILayout.Width(50)))
                        EditorGUIUtility.PingObject(e.Go);

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                        Selection.activeObject = e.Go;

                    GUILayout.FlexibleSpace();

                    var prev = GUI.color;
                    GUI.color = e.IsSuspect ? new Color(1f, 0.7f, 0.2f) : Color.white;
                    GUILayout.Label(e.IsSuspect ? "SUSPECT" : "OK", EditorStyles.boldLabel);
                    GUI.color = prev;
                }

                EditorGUILayout.LabelField(e.Path, EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Triangles: {e.Triangles:n0} | BoundsSize: {e.BoundsSize:F2} | Scale: {e.Scale}");

                if (!string.IsNullOrEmpty(e.Reasons))
                    EditorGUILayout.HelpBox(e.Reasons, e.IsSuspect ? MessageType.Warning : MessageType.None);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(e.Go.activeSelf ? "Disable" : "Enable"))
                        e.Go.SetActive(!e.Go.activeSelf);

                    if (GUILayout.Button("Toggle Contribute GI"))
                        ToggleContributeGI(e.Go);
                }
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Nota: Unity non espone il nome dell’oggetto che fa fallire AddGeometry. Questo tool ti aiuta a trovarlo evidenziando mesh tipicamente problematiche (OBJ, UV2 mancanti, scale negative, bounds enormi, troppi triangoli, skinned, ecc.) e a isolarlo disabilitando a blocchi.",
            MessageType.Info
        );
    }

    private void Scan()
    {
        _entries.Clear();

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return;

        var roots = scene.GetRootGameObjects();
        foreach (var r in roots)
            ScanGo(r, r.name);

        Repaint();
    }

    private void ScanGo(GameObject go, string path)
    {
        // consideriamo solo oggetti con renderer
        var mr = go.GetComponent<MeshRenderer>();
        var mf = go.GetComponent<MeshFilter>();
        var smr = go.GetComponent<SkinnedMeshRenderer>();

        bool hasAnyRenderer = (mr != null && mr.enabled) || (smr != null && smr.enabled);
        if (hasAnyRenderer)
        {
            var e = new Entry { Go = go, Path = path };
            e.Scale = go.transform.lossyScale;

            // Scale sospetta
            if (e.Scale.x <= 0f || e.Scale.y <= 0f || e.Scale.z <= 0f)
                e.AddReason("Scale <= 0 (negativa o zero) nella gerarchia: può rompere AddGeometry.");

            // bounds
            Bounds b;
            if (mr != null) b = mr.bounds;
            else b = smr.bounds;

            e.BoundsSize = b.size.magnitude;
            if (e.BoundsSize > _boundsThreshold)
                e.AddReason($"Bounds enorme (size magnitude {e.BoundsSize:F2} > {_boundsThreshold}). Possibili coordinate lontane/origine, mesh enorme o scala sbagliata.");

            // triangles + UV2
            Mesh mesh = null;
            if (mf != null) mesh = mf.sharedMesh;
            if (mesh == null && smr != null) mesh = smr.sharedMesh;

            if (mesh != null)
            {
                e.Triangles = mesh.triangles != null ? mesh.triangles.Length / 3 : 0;

                if (e.Triangles > _triThreshold)
                    e.AddReason($"Molti triangoli ({e.Triangles:n0} > {_triThreshold:n0}).");

                // UV2 check
                // uv2 in Unity è uv (channel 1) o uv2 proprietà; mesh.uv2 in editor restituisce array
                var uv2 = mesh.uv2;
                if (uv2 == null || uv2.Length == 0)
                    e.AddReason("UV2 (Lightmap UVs) assenti. Se è OBJ/mesh importata, attiva 'Generate Lightmap UVs'.");

                // degenerate bounds (mesh local)
                var lb = mesh.bounds;
                if (float.IsNaN(lb.size.x) || float.IsInfinity(lb.size.x))
                    e.AddReason("Mesh bounds NaN/Infinity: mesh corrotta.");
            }
            else
            {
                e.AddReason("Renderer senza Mesh valida (mesh null). Possibile problema import/renderer.");
            }

            // contribute GI check
            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            bool contributeGI = (flags & StaticEditorFlags.ContributeGI) != 0;
            bool isStatic = flags != 0;
            if (contributeGI == false)
                e.AddReason("Non è Contribute GI (ok se non vuoi che venga bakeata).");

            // skinned renderer è spesso problematico per lightmap
            if (smr != null)
                e.AddReason("SkinnedMeshRenderer: di solito NON va incluso nel bake (togli Contribute GI).");

            // severity
            e.ComputeSeverity(_triThreshold, _boundsThreshold);
            _entries.Add(e);
        }

        foreach (Transform c in go.transform)
            ScanGo(c.gameObject, path + "/" + c.name);
    }

    private void SelectSuspects()
    {
        var suspects = _entries.Where(e => e.IsSuspect).Select(e => e.Go).ToArray();
        Selection.objects = suspects;
        if (suspects.Length == 0)
            Debug.Log("GI Detective: nessun sospetto trovato (con i criteri attuali).");
        else
            Debug.Log($"GI Detective: selezionati {suspects.Length} sospetti.");
    }

    private void SetSuspectsEnabled(bool enabled)
    {
        var suspects = _entries.Where(e => e.IsSuspect).Select(e => e.Go).Distinct().ToList();
        foreach (var go in suspects)
            go.SetActive(enabled);

        Debug.Log($"GI Detective: {(enabled ? "abilitati" : "disabilitati")} {suspects.Count} sospetti.");
    }

    private void SetAllFoundEnabled(bool enabled)
    {
        foreach (var e in _entries.Select(x => x.Go).Distinct())
            e.SetActive(enabled);

        Debug.Log($"GI Detective: {(enabled ? "abilitati" : "disabilitati")} tutti gli oggetti trovati.");
    }

    private void ToggleContributeGI(GameObject go)
    {
        var flags = GameObjectUtility.GetStaticEditorFlags(go);
        bool has = (flags & StaticEditorFlags.ContributeGI) != 0;
        if (has) flags &= ~StaticEditorFlags.ContributeGI;
        else flags |= StaticEditorFlags.ContributeGI;

        GameObjectUtility.SetStaticEditorFlags(go, flags);
        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    [Serializable]
    private class Entry
    {
        public GameObject Go;
        public string Path;
        public int Triangles;
        public float BoundsSize;
        public Vector3 Scale;
        public string Reasons = "";
        public bool IsSuspect;
        public int SeverityScore;

        public void AddReason(string r)
        {
            if (!string.IsNullOrEmpty(Reasons)) Reasons += "\n";
            Reasons += "• " + r;
        }

        public void ComputeSeverity(int triThresh, float boundsThresh)
        {
            int s = 0;
            if (Scale.x <= 0f || Scale.y <= 0f || Scale.z <= 0f) s += 5;
            if (BoundsSize > boundsThresh) s += 4;
            if (Triangles > triThresh) s += 3;
            if (Reasons.Contains("UV2")) s += 3;
            if (Reasons.Contains("NaN/Infinity")) s += 10;

            SeverityScore = s;
            IsSuspect = s >= 6; // soglia: regolabile
        }
    }
}
