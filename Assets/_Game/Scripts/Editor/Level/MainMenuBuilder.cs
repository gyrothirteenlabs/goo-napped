using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace GooNapped.EditorTools
{
    // Main menu PLACEHOLDER (12 Nov, gaya "adegan hidup gelap"): ruang lab gelap dari tile placeholder, lentera,
    // toples berisi teman yang berpendar sesuai aura, slime pemain diam bernapas, menu UI Toolkit (MainMenu.uxml/uss).
    // Semua aset visual = placeholder yang sudah ada. Tampilan final: tim desainer (ganti peta/prop di sini, gaya di MainMenu.uss).
    // Jalankan GooNapped > Blockout > Main menu (menimpa objek hasil builder di scene MainMenu).
    public static class MainMenuBuilder
    {
        const string ScenePath = "Assets/_Game/Scenes/Menus/MainMenu.unity";
        const string GridSource = "Assets/_Game/Scenes/Levels/V2/L02_Hedge.unity";
        const string P = "Assets/_Game/Prefabs/";

        // # batu, . lantai. Kamera 8 x 4.5 unit di tengah (0,0); sel (0,0) = baris/kolom tengah peta.
        static readonly string[] Map =
        {
            "########################",
            "########################",
            "########################",
            "########################",
            "########################",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
            "........................",
        };
        const int OriginX = -11, OriginY = 5;   // sel kiri-atas peta (lebih besar dari layar lebar)

        [MenuItem("GooNapped/Blockout/Main menu")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Hentikan Play dulu."); return; }
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var r in scene.GetRootGameObjects())
                if (r.name == "Grid" || r.name == "Backdrop" || r.name == "MenuUI") Object.DestroyImmediate(r);

            // Grid (tilemap + material + sorting) disalin dari level supaya pencahayaan sama
            var src = EditorSceneManager.OpenScene(GridSource, OpenSceneMode.Additive);
            GameObject grid = null;
            foreach (var r in src.GetRootGameObjects())
                if (r.name == "Grid") { grid = Object.Instantiate(r); grid.name = "Grid"; break; }
            EditorSceneManager.MoveGameObjectToScene(grid, scene);
            EditorSceneManager.CloseScene(src, true);
            EditorSceneManager.SetActiveScene(scene);

            var floor = grid.transform.Find("Floor").GetComponent<Tilemap>();
            var walls = grid.transform.Find("Walls").GetComponent<Tilemap>();
            var decor = grid.transform.Find("Decor").GetComponent<Tilemap>();
            floor.ClearAllTiles(); walls.ClearAllTiles(); decor.ClearAllTiles();
            var ground = AssetDatabase.LoadAssetAtPath<Tile>("Assets/_Game/Art/Tiles/Placeholder/Tile_Ground.asset");
            var rock = AssetDatabase.LoadAssetAtPath<Tile>("Assets/_Game/Art/Tiles/Placeholder/Tile_Rock.asset");
            for (int row = 0; row < Map.Length; row++)
                for (int col = 0; col < Map[row].Length; col++)
                {
                    var c = new Vector3Int(OriginX + col, OriginY - row, 0);
                    if (Map[row][col] == '#') walls.SetTile(c, rock); else floor.SetTile(c, ground);
                }
            var wc = grid.transform.Find("Walls").GetComponent<TilemapCollider2D>();
            if (wc != null) Object.DestroyImmediate(wc);

            var backdrop = new GameObject("Backdrop");
            Spawn(P + "Level/Lantern.prefab", backdrop.transform, new Vector3(-1.1f, 0.05f, 0), "Lantern");
            Spawn(P + "Level/Lantern.prefab", backdrop.transform, new Vector3(-5.5f, 0.05f, 0), "Lantern");
            Spawn(P + "Level/Lantern.prefab", backdrop.transform, new Vector3(3.6f, 0.05f, 0), "Lantern");

            // toples: teman yang masih dikurung (aura masing-masing), di sisi kanan
            string[] friends = { "Friend_Pip", "Friend_Lumi", "Friend_Tok" };
            float[] jx = { 1.35f, 2.35f, 3.0f };
            float[] jy = { -0.55f, -0.35f, -1.25f };
            for (int i = 0; i < friends.Length; i++)
            {
                var jar = Spawn(P + "Props/Jar.prefab", backdrop.transform, new Vector3(jx[i], jy[i], 0), "Jar_" + friends[i]);
                var rec = AssetDatabase.LoadAssetAtPath<Object>("Assets/_Game/Data/Recipes/" + friends[i] + ".asset");
                var jc = jar.GetComponent<Jar>();
                var so = new SerializedObject(jc);
                so.FindProperty("recipe").objectReferenceValue = rec;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // slime pemain: hanya tampilan (tanpa PlayerBrain = diam bernapas)
            var slime = Spawn(P + "Characters/Slime_Player.prefab", backdrop.transform, new Vector3(-0.2f, -1.15f, 0), "Slime_Menu");
            slime.AddComponent<MenuSlime>();

            // UI
            var ui = new GameObject("MenuUI");
            var doc = ui.AddComponent<UIDocument>();
            doc.panelSettings = PanelSettingsAsset();
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Game/UI/MainMenu.uxml");
            foreach (var r in scene.GetRootGameObjects())
                if (r.name == "MainMenuController")
                {
                    var mm = r.GetComponent<MainMenuController>();
                    var so = new SerializedObject(mm);
                    so.FindProperty("document").objectReferenceValue = doc;
                    so.FindProperty("subtitle").stringValue = "working title  ·  Game Off 2026: SHIFT";
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

            // kamera: diam di tengah ruang
            var cam = Camera.main;
            cam.transform.position = new Vector3(0.5f, -0.4f, -10f);
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f);

            // cahaya global redup seperti level (mood gelap)
            foreach (var r in scene.GetRootGameObjects())
                if (r.name == "Global Light 2D")
                    foreach (var comp in r.GetComponents<Component>())
                        if (comp != null && comp.GetType().Name == "Light2D")
                        {
                            var so = new SerializedObject(comp);
                            var inten = so.FindProperty("m_Intensity");
                            if (inten != null) inten.floatValue = 0.18f;
                            so.ApplyModifiedPropertiesWithoutUndo();
                        }

            foreach (var r in scene.GetRootGameObjects())
            {
                var note = r.GetComponent<SceneNote>();
                if (note == null) continue;
                var so = new SerializedObject(note);
                var p = so.FindProperty("note");
                if (p != null) p.stringValue = "MAIN MENU (placeholder, gaya adegan hidup gelap). Dibangun GooNapped > Blockout > Main menu " +
                    "(Editor/Level/MainMenuBuilder.cs). Menu: UI/MainMenu.uxml + MainMenu.uss, logika MainMenuController. Tampilan final: tim desainer.";
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Main menu dibangun.");
        }

        static PanelSettings PanelSettingsAsset()
        {
            const string path = "Assets/_Game/UI/MainMenuPanelSettings.asset";
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (ps != null) return ps;
            AssetDatabase.CopyAsset("Assets/_Game/UI/HudPanelSettings.asset", path);
            return AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
        }

        static GameObject Spawn(string prefab, Transform parent, Vector3 pos, string name)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab), parent);
            go.transform.position = pos;
            go.name = name;
            return go;
        }
    }
}
