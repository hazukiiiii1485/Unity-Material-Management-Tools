using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetLibrary
{
    public class AssetLibraryWindow : EditorWindow
    {
        // ─── State ───────────────────────────────────────────────
        AssetLibraryDatabase _db;
        List<AssetEntry>     _filtered = new();
        Vector2              _scrollPos;

        // Search
        string   _nameQuery     = "";
        int      _kindIndex     = 0;   // 0=All,1=Texture,2=Audio,3=Model
        string   _tagQuery      = "";
        string   _catQuery      = "";

        // Selection / edit
        AssetEntry _selected;
        string     _editTag  = "";
        string     _editCat  = "";

        // Duplicates
        bool                     _showDupes  = false;
        List<List<AssetEntry>>   _dupeGroups = new();

        // Preview
        Texture2D _preview;

        static readonly string[] KindLabels = { "All", "Texture", "Audio", "Model" };
        static readonly Color    DupeColor  = new Color(1f, 0.4f, 0.4f, 0.3f);
        static readonly Color    SelColor   = new Color(0.3f, 0.6f, 1f, 0.4f);

        // ─── Open ─────────────────────────────────────────────────
        [MenuItem("Tools/Asset Library")]
        public static void Open()
        {
            var win = GetWindow<AssetLibraryWindow>("📦 Asset Library");
            win.minSize = new Vector2(900, 520);
            win.Load();
        }

        // ─── Load / Save ──────────────────────────────────────────
        void Load()
        {
            _db = AssetLibraryData.Load();
            ApplyFilter();
        }

        void Save() => AssetLibraryData.Save(_db);

        // ─── GUI ──────────────────────────────────────────────────
        void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.BeginHorizontal();
            {
                DrawList();         // 左カラム
                DrawDetail();       // 右カラム
            }
            EditorGUILayout.EndHorizontal();

            if (_showDupes) DrawDuplicatePanel();
        }

        // ── Toolbar ───────────────────────────────────────────────
        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("🔍 Scan Project", EditorStyles.toolbarButton, GUILayout.Width(110)))
                ScanProject();

            GUILayout.Space(8);

            GUILayout.Label("Name:", EditorStyles.miniLabel, GUILayout.Width(38));
            var newName = EditorGUILayout.TextField(_nameQuery, EditorStyles.toolbarSearchField, GUILayout.Width(140));
            if (newName != _nameQuery) { _nameQuery = newName; ApplyFilter(); }

            GUILayout.Label("  Type:", EditorStyles.miniLabel, GUILayout.Width(40));
            var newKind = EditorGUILayout.Popup(_kindIndex, KindLabels, EditorStyles.toolbarPopup, GUILayout.Width(70));
            if (newKind != _kindIndex) { _kindIndex = newKind; ApplyFilter(); }

            GUILayout.Label("  Tag:", EditorStyles.miniLabel, GUILayout.Width(34));
            var newTag = EditorGUILayout.TextField(_tagQuery, EditorStyles.toolbarSearchField, GUILayout.Width(100));
            if (newTag != _tagQuery) { _tagQuery = newTag; ApplyFilter(); }

            GUILayout.Label("  Cat:", EditorStyles.miniLabel, GUILayout.Width(32));
            var newCat = EditorGUILayout.TextField(_catQuery, EditorStyles.toolbarSearchField, GUILayout.Width(100));
            if (newCat != _catQuery) { _catQuery = newCat; ApplyFilter(); }

            GUILayout.FlexibleSpace();

            var dupeLabel = _showDupes ? "✅ Duplicates" : "⚠️ Duplicates";
            if (GUILayout.Button(dupeLabel, EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                _showDupes = !_showDupes;
                if (_showDupes) _dupeGroups = DuplicateDetector.Detect(_db.entries);
            }

            GUILayout.Label($"  {_filtered.Count} assets", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        // ── List (左) ─────────────────────────────────────────────
        void DrawList()
        {
            var dupeGuids = new HashSet<string>(
                _dupeGroups.SelectMany(g => g.Select(e => e.guid)));

            EditorGUILayout.BeginVertical(GUILayout.Width(540));
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var entry in _filtered)
            {
                bool isDupe = dupeGuids.Contains(entry.guid);
                bool isSel  = _selected?.guid == entry.guid;

                var bg = isSel ? SelColor : (isDupe ? DupeColor : Color.clear);
                var rect = EditorGUILayout.BeginHorizontal();
                EditorGUI.DrawRect(rect, bg);

                // Thumbnail
                var preview = AssetPreview.GetAssetPreview(
                    AssetDatabase.LoadAssetAtPath<Object>(entry.assetPath));
                if (preview != null)
                    GUILayout.Label(preview, GUILayout.Width(40), GUILayout.Height(40));
                else
                    GUILayout.Label(KindIcon(entry.kind), GUILayout.Width(40), GUILayout.Height(40));

                // Info
                EditorGUILayout.BeginVertical();
                GUILayout.Label(entry.name, EditorStyles.boldLabel);
                GUILayout.Label($"{entry.kind}  |  {entry.category}  |  {FormatSize(entry.fileSizeBytes)}", EditorStyles.miniLabel);
                if (entry.tags.Count > 0)
                    GUILayout.Label("🏷 " + string.Join(", ", entry.tags), EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // Select button
                if (GUILayout.Button("›", GUILayout.Width(22), GUILayout.Height(38)))
                    SelectEntry(entry);

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ── Detail (右) ───────────────────────────────────────────
        void DrawDetail()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(340), GUILayout.ExpandHeight(true));

            if (_selected == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("← Select an asset", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            var e = _selected;

            // Preview image
            if (_preview != null)
            {
                var previewRect = GUILayoutUtility.GetRect(320, 160);
                GUI.DrawTexture(previewRect, _preview, ScaleMode.ScaleToFit);
            }
            else
            {
                GUILayout.Label(KindIcon(e.kind) + " (No preview)", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(60));
            }

            EditorGUILayout.Space(4);

            // Meta
            EditorGUILayout.LabelField("Name",     e.name,    EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Path",     e.assetPath, EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Type",     e.kind.ToString());
            EditorGUILayout.LabelField("Size",     FormatSize(e.fileSizeBytes));
            EditorGUILayout.LabelField("Added",    e.addedAt);
            EditorGUILayout.LabelField("MD5",      e.md5.Length > 8 ? e.md5[..8] + "…" : e.md5, EditorStyles.miniLabel);

            EditorGUILayout.Space(6);

            // Category edit
            EditorGUILayout.LabelField("Category", EditorStyles.boldLabel);
            _editCat = EditorGUILayout.TextField(_editCat);

            // Tag edit
            EditorGUILayout.LabelField("Tags (comma-separated)", EditorStyles.boldLabel);
            _editTag = EditorGUILayout.TextField(_editTag);
            if (e.tags.Count > 0)
                EditorGUILayout.LabelField("Current: " + string.Join(", ", e.tags), EditorStyles.miniLabel);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("💾 Save Changes"))
            {
                e.category = _editCat.Trim();
                e.tags = _editTag.Split(',')
                    .Select(t => t.Trim())
                    .Where(t => t.Length > 0)
                    .ToList();
                Save();
                ApplyFilter();
            }

            EditorGUILayout.Space(6);

            if (GUILayout.Button("📂 Ping in Project"))
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(e.assetPath));

            if (GUILayout.Button("🗑 Remove from Library"))
            {
                _db.entries.RemoveAll(x => x.guid == e.guid);
                _selected = null;
                _preview  = null;
                Save();
                ApplyFilter();
            }

            EditorGUILayout.EndVertical();
        }

        // ── Duplicate Panel ───────────────────────────────────────
        void DrawDuplicatePanel()
        {
            EditorGUILayout.HelpBox(
                $"⚠️ {_dupeGroups.Count} duplicate group(s) found.", MessageType.Warning);

            foreach (var group in _dupeGroups)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"MD5: {group[0].md5[..12]}…  ({group.Count} files)", EditorStyles.boldLabel);
                foreach (var e in group)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(e.assetPath, EditorStyles.miniLabel);
                    if (GUILayout.Button("Ping", GUILayout.Width(40)))
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(e.assetPath));
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
        }

        // ─── Logic ────────────────────────────────────────────────
        void ScanProject()
        {
            var kinds = new[] { "t:Texture2D", "t:Sprite", "t:AudioClip", "t:GameObject", "t:Model" };
            var guids = new HashSet<string>();
            foreach (var filter in kinds)
                foreach (var g in AssetDatabase.FindAssets(filter))
                    guids.Add(g);

            int added = 0;
            var existingGuids = new HashSet<string>(_db.entries.Select(e => e.guid));

            foreach (var guid in guids)
            {
                if (existingGuids.Contains(guid)) continue;
                var path  = AssetDatabase.GUIDToAssetPath(guid);
                var entry = AssetScanner.ScanEntry(path);
                if (entry.kind == AssetKind.Unknown) continue;
                _db.entries.Add(entry);
                added++;
            }

            Save();
            ApplyFilter();
            Debug.Log($"[AssetLibrary] Scanned: {added} new assets added. Total: {_db.entries.Count}");
        }

        void SelectEntry(AssetEntry entry)
        {
            _selected = entry;
            _editCat  = entry.category;
            _editTag  = string.Join(", ", entry.tags);
            _preview  = AssetPreview.GetAssetPreview(
                AssetDatabase.LoadAssetAtPath<Object>(entry.assetPath));
        }

        void ApplyFilter()
        {
            AssetKind? kind = _kindIndex == 0 ? null : (AssetKind)(_kindIndex - 1);
            _filtered = AssetSearchFilter.Apply(_db.entries, _nameQuery, kind, _tagQuery, _catQuery);
            Repaint();
        }

        // ─── Helpers ──────────────────────────────────────────────
        static string KindIcon(AssetKind k) => k switch
        {
            AssetKind.Texture => "🖼",
            AssetKind.Audio   => "🔊",
            AssetKind.Model   => "🧊",
            _                 => "📄"
        };

        static string FormatSize(long bytes) =>
            bytes < 1024      ? $"{bytes} B" :
            bytes < 1048576   ? $"{bytes / 1024f:F1} KB" :
                                $"{bytes / 1048576f:F1} MB";
    }
}
