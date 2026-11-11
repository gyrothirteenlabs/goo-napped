using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GooNapped.Tests
{
    // QC aset: tidak ada script hilang ("Missing (Mono Script)"), referensi rusak (aset terhapus), dan field wajib
    // komponen game tidak kosong. Mencakup semua prefab game dan semua scene di alur game.
    public class AssetIntegrityTests
    {
        // Field yang WAJIB terisi (kosong = fitur diam-diam tidak jalan).
        static readonly Dictionary<System.Type, string[]> Required = new Dictionary<System.Type, string[]>
        {
            { typeof(PlayerBrain), new[] { "actions", "blobSprite" } },
            { typeof(PauseMenu), new[] { "actions" } },
            { typeof(StoryPlayer), new[] { "actions" } },
            { typeof(Jar), new[] { "recipe", "friendPrefab" } },
            { typeof(SlimePiece), new[] { "pickupPrefab" } },
            { typeof(SlimeTraitFx), new[] { "particleMaterial" } },
            { typeof(Guard), new[] { "coneMaterial" } },
            { typeof(IngredientPickup), new[] { "ingredient" } },
            { typeof(Door), new[] { "spriteRenderer", "closedSprite", "openSprite" } },
            { typeof(Lever), new[] { "spriteRenderer", "offSprite", "onSprite" } },
            { typeof(PressurePlate), new[] { "spriteRenderer", "upSprite", "downSprite" } },
            { typeof(StoryScene), new[] { "sequence" } },
        };

        static IEnumerable<string> PrefabPaths() =>
            AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game/Prefabs" }).Select(AssetDatabase.GUIDToAssetPath);

        static IEnumerable<string> ScenePaths()
        {
            var names = new HashSet<string>(GameFlow.Levels.Concat(new[] { "Boot", GameFlow.MainMenu, GameFlow.Intro, GameFlow.Ending }));
            return EditorBuildSettings.scenes.Select(s => s.path)
                .Where(p => names.Contains(System.IO.Path.GetFileNameWithoutExtension(p)));
        }

        static void CheckObject(GameObject root, string where, List<string> problems, bool isPrefabAsset)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var go = t.gameObject;
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missing > 0) problems.Add($"{where}/{go.name}: {missing} script hilang.");
                foreach (var c in go.GetComponents<Component>())
                {
                    if (c == null) continue;
                    var so = new SerializedObject(c);
                    var it = so.GetIterator();
                    while (it.NextVisible(true))
                    {
                        if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                        if (it.objectReferenceValue == null && it.objectReferenceInstanceIDValue != 0)
                            problems.Add($"{where}/{go.name} ({c.GetType().Name}.{it.propertyPath}): referensi rusak (aset terhapus?).");
                    }
                    if (Required.TryGetValue(c.GetType(), out var fields))
                        foreach (var f in fields)
                        {
                            // diisi per objek di scene, bukan di prefab
                            if (isPrefabAsset && (f == "recipe" || f == "ingredient" || f == "sequence")) continue;
                            var p = so.FindProperty(f);
                            if (p == null) problems.Add($"{c.GetType().Name}.{f}: field tidak ada (nama berubah? perbarui test).");
                            else if (p.objectReferenceValue == null) problems.Add($"{where}/{go.name}: {c.GetType().Name}.{f} kosong.");
                        }
                }
            }
        }

        [Test]
        public void Prefab_TanpaScriptHilangDanFieldWajibTerisi()
        {
            var problems = new List<string>();
            foreach (var path in PrefabPaths())
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try { CheckObject(root, System.IO.Path.GetFileNameWithoutExtension(path), problems, true); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [TestCaseSource(nameof(ScenePaths))]
        public void Scene_TanpaScriptHilangDanFieldWajibTerisi(string path)
        {
            var already = SceneManager.GetSceneByPath(path);
            bool openedHere = !already.isLoaded;
            var scene = openedHere ? EditorSceneManager.OpenScene(path, OpenSceneMode.Additive) : already;
            try
            {
                var problems = new List<string>();
                foreach (var r in scene.GetRootGameObjects()) CheckObject(r, scene.name, problems, false);
                Assert.IsEmpty(problems, string.Join("\n", problems));
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void DataBahanDanResep_Lengkap()
        {
            var problems = new List<string>();
            foreach (var g in AssetDatabase.FindAssets("t:IngredientData", new[] { "Assets/_Game/Data" }))
            {
                var d = AssetDatabase.LoadAssetAtPath<IngredientData>(AssetDatabase.GUIDToAssetPath(g));
                if (string.IsNullOrWhiteSpace(d.DisplayName)) problems.Add(d.name + ": nama kosong.");
                if (d.Traits.Count == 0) problems.Add(d.name + ": tanpa ciri.");
                if (d.Traits.Any(t => string.IsNullOrWhiteSpace(t.value))) problems.Add(d.name + ": ada ciri bernilai kosong.");
                if (d.Traits.GroupBy(t => t.kind).Any(x => x.Count() > 1)) problems.Add(d.name + ": jenis ciri dobel.");
            }
            foreach (var g in AssetDatabase.FindAssets("t:RecipeData", new[] { "Assets/_Game/Data" }))
            {
                var r = AssetDatabase.LoadAssetAtPath<RecipeData>(AssetDatabase.GUIDToAssetPath(g));
                if (r.name.StartsWith("Friend_"))
                {
                    // v2: tepat 1 ciri warna (= aura) dan ukuran 1..MaxMass-1 (tiruan + sisa pemain minimal 1 unit)
                    if (r.Traits.Count != 1 || r.Traits[0].kind != TraitKind.Color) problems.Add(r.name + ": teman v2 harus punya tepat 1 ciri Warna (aura).");
                    if (r.Size < 1 || r.Size > SlimeBodyState.MaxMass - 1) problems.Add(r.name + ": ukuran " + r.Size + " tidak bisa dibuat (maks " + (SlimeBodyState.MaxMass - 1) + ").");
                    if (string.IsNullOrWhiteSpace(r.FriendName)) problems.Add(r.name + ": nama kosong.");
                }
                else if (r.Traits.Count < 2 || r.Traits.Count > 3) problems.Add(r.name + ": " + r.Traits.Count + " ciri (aturan v1 2–3).");
                if (r.Traits.GroupBy(t => t.kind).Any(x => x.Count() > 1)) problems.Add(r.name + ": jenis ciri dobel (tidak mungkin dibuat).");
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
