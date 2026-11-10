using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GooNapped.EditorTools
{
    // Blockout level dari peta teks (BUKAN desain final). Mengisi scene level placeholder:
    // tile lantai/batu di Tilemap Floor/Walls, pemain, toples, bahan, penjaga, exit, lampu, kamera.
    // Desainer: ubah peta di bawah lalu jalankan menu GooNapped > Blockout, atau gambar ulang langsung di Tilemap.
    //
    // Legenda:  # batu (tembok)   . lantai   S spawn pemain   E exit   J toples
    //           b Serbuk Batu   l Lumut Pendar   r Ramuan Api   L lentera
    //           g / G  titik awal / akhir patroli Golem (garis lurus)
    public static class LevelBlockout
    {
        const string P = "Assets/_Game/Prefabs/";
        const string D = "Assets/_Game/Data/";
        const string TileDir = "Assets/_Game/Art/Tiles/Placeholder/";

        class Config
        {
            public string scene, recipe, next, note;
            public string[] map;
        }

        static readonly Config C1L01 = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/Chapter1/C1_L01.unity",
            recipe = "Recipe_Kerikil",
            next = "C1_L02",
            note = "BLOCKOUT C1_L01 (tutorial, tanpa penjaga). Jalan setapak tebing tambang, malam.\n" +
                   "Mengajarkan: gerak, intip toples, makan 1 bahan (Serbuk Batu = abu + berat), belah, tukar, telan, exit.\n" +
                   "Resep Kerikil: abu + berat. Dibangun oleh GooNapped > Blockout > C1_L01 (Editor/Level/LevelBlockout.cs). Layout final: tim desainer.",
            map = new[]
            {
                "####################",
                "#..........#########",
                "#.S........#########",
                "#......#..L#.....J.#",
                "#......#...........#",
                "####...#####...L...#",
                "####.......####....#",
                "####.b.....b...E...#",
                "####################",
            },
        };

        static readonly Config C1L02 = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/Chapter1/C1_L02.unity",
            recipe = "Recipe_Pendar",
            next = "",
            note = "BLOCKOUT C1_L02 (kenalan Golem). Kamp tambang, malam.\n" +
                   "Golem patroli di lorong tengah, melihat lurus. Lewati di belakangnya, atau tinggalkan potongan nyamar untuk menghadangnya (golem berhenti lalu putar balik).\n" +
                   "Resep Pendar: berat + berkilau. Lumut Pendar membuat tubuh menyala: terlihat dari lebih jauh.\n" +
                   "Dibangun oleh GooNapped > Blockout > C1_L02. Layout final: tim desainer.",
            map = new[]
            {
                "######################",
                "#S...#.......b.......#",
                "#....#..####...####..#",
                "#....#g.............G#",
                "#.......####...####..#",
                "######..#.........J..#",
                "#.l.....#....L.......#",
                "#...L...#....b.......#",
                "#E......#............#",
                "######################",
            },
        };

        [MenuItem("GooNapped/Blockout/C1_L01")]
        public static void BuildC1L01() => Build(C1L01);

        [MenuItem("GooNapped/Blockout/C1_L02")]
        public static void BuildC1L02() => Build(C1L02);

        static T Load<T>(string path) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) throw new System.Exception("Aset tidak ada: " + path);
            return a;
        }

        static Tile PlaceholderTile(string name, string spritePath, Color color)
        {
            string path = TileDir + name + ".asset";
            var t = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (t != null) return t;
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Art/Tiles/Placeholder"))
                AssetDatabase.CreateFolder("Assets/_Game/Art/Tiles", "Placeholder");
            Sprite sprite = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(spritePath))
                if (o is Sprite s && (sprite == null || string.CompareOrdinal(s.name, sprite.name) < 0)) sprite = s;
            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sprite;
            t.color = color;
            t.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.CreateAsset(t, path);
            return t;
        }

        static GameObject Spawn(string prefabPath, Transform parent, Vector3 pos, string name = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(prefabPath), parent);
            go.transform.position = pos;
            if (name != null) go.name = name;
            return go;
        }

        static void Set(Object target, string prop, System.Action<SerializedProperty> set)
        {
            var so = new SerializedObject(target);
            set(so.FindProperty(prop));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.DestroyImmediate(t.GetChild(i).gameObject);
        }

        static Transform Find(UnityEngine.SceneManagement.Scene s, string path)
        {
            var parts = path.Split('/');
            foreach (var r in s.GetRootGameObjects())
                if (r.name == parts[0])
                    return parts.Length == 1 ? r.transform : r.transform.Find(string.Join("/", parts, 1, parts.Length - 1));
            return null;
        }

        static void Build(Config c)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Stop Play mode dulu."); return; }
            var scene = EditorSceneManager.OpenScene(c.scene, OpenSceneMode.Single);
            var envMat = Load<Material>("Assets/_Game/Art/Materials/Env_Lit.mat");
            var floorTile = PlaceholderTile("Tile_Ground", "Assets/_Game/Art/Sprites/Environment/floor_stone.png", new Color(0.62f, 0.56f, 0.5f));
            var rockTile = PlaceholderTile("Tile_Rock", "Assets/_Game/Art/Sprites/Environment/wall_stone.png", new Color(0.55f, 0.52f, 0.5f));

            var floor = Find(scene, "Grid/Floor").GetComponent<Tilemap>();
            var walls = Find(scene, "Grid/Walls").GetComponent<Tilemap>();
            floor.ClearAllTiles();
            walls.ClearAllTiles();
            floor.GetComponent<TilemapRenderer>().sharedMaterial = envMat;
            floor.GetComponent<TilemapRenderer>().sortingOrder = -100;
            walls.GetComponent<TilemapRenderer>().sharedMaterial = envMat;
            walls.GetComponent<TilemapRenderer>().sortingOrder = -90;

            var actors = Find(scene, "Actors");
            var spawn = Find(scene, "Actors/PlayerSpawn");
            var exitRoot = Find(scene, "Actors/Exit");
            var jarRoot = Find(scene, "Actors/Jars");
            var ingRoot = Find(scene, "Actors/Ingredients");
            var guardRoot = Find(scene, "Actors/Guards");
            var lights = Find(scene, "Lights");
            foreach (var t in new[] { spawn, exitRoot, jarRoot, ingRoot, guardRoot, lights }) Clear(t);
            foreach (var n in new[] { "LevelSystems", "CameraBounds" })
            {
                var old = GameObject.Find(n);
                if (old != null) Object.DestroyImmediate(old);
            }

            int h = c.map.Length, w = c.map[0].Length;
            Vector2? golemA = null, golemB = null;
            var ingredients = new Dictionary<char, string> { { 'b', "SerbukBatu" }, { 'l', "LumutPendar" }, { 'r', "RamuanApi" } };
            for (int row = 0; row < h; row++)
            {
                for (int x = 0; x < w; x++)
                {
                    char ch = x < c.map[row].Length ? c.map[row][x] : '#';
                    int y = h - 1 - row;
                    var cell = new Vector3Int(x, y, 0);
                    var pos = new Vector3(x + 0.5f, y + 0.05f, 0f);   // pivot sprite = tengah bawah tile
                    if (ch == '#') { walls.SetTile(cell, rockTile); continue; }
                    floor.SetTile(cell, floorTile);
                    switch (ch)
                    {
                        case 'S':
                            spawn.position = pos;
                            Spawn(P + "Characters/Slime_Player.prefab", spawn, pos, "Slime_Player");
                            break;
                        case 'E':
                        {
                            var e = new GameObject("Exit_Zone");
                            e.transform.SetParent(exitRoot, false);
                            e.transform.position = new Vector3(x + 0.5f, y, 0f);
                            var sr = e.AddComponent<SpriteRenderer>();
                            sr.sprite = floorTile.sprite;
                            sr.sharedMaterial = envMat;
                            sr.color = new Color(0.45f, 0.95f, 0.55f);
                            sr.sortingOrder = -80;
                            var bc = e.AddComponent<BoxCollider2D>();
                            bc.isTrigger = true;
                            bc.size = new Vector2(0.8f, 0.8f);
                            bc.offset = new Vector2(0f, 0.5f);
                            e.AddComponent<LevelExit>();
                            break;
                        }
                        case 'J':
                        {
                            var j = Spawn(P + "Props/Jar.prefab", jarRoot, pos, "Jar_" + c.recipe.Replace("Recipe_", ""));
                            var recipe = Load<RecipeData>(D + "Recipes/" + c.recipe + ".asset");
                            Set(j.GetComponent<Jar>(), "recipe", p => p.objectReferenceValue = recipe);
                            break;
                        }
                        case 'L':
                            Spawn(P + "Level/Lantern.prefab", lights, pos, "Lantern");
                            break;
                        case 'g': golemA = pos; break;
                        case 'G': golemB = pos; break;
                        default:
                            if (ingredients.TryGetValue(ch, out var id))
                            {
                                var data = Load<IngredientData>(D + "Ingredients/Ingredient_" + id + ".asset");
                                var p = Spawn(P + "Props/IngredientPickup.prefab", ingRoot, pos, "Pickup_" + id);
                                Set(p.GetComponent<IngredientPickup>(), "ingredient", q => q.objectReferenceValue = data);
                                p.GetComponent<SpriteRenderer>().color = data.PlaceholderTint;
                            }
                            break;
                    }
                }
            }

            if (golemA.HasValue)
            {
                var g = Spawn(P + "Enemies/Golem_Placeholder.prefab", guardRoot, golemA.Value, "Golem_Patroli");
                var b = golemB ?? golemA.Value;
                Set(g.GetComponent<GolemGuard>(), "patrolPoints", p =>
                {
                    p.arraySize = 2;
                    p.GetArrayElementAtIndex(0).vector2Value = golemA.Value;
                    p.GetArrayElementAtIndex(1).vector2Value = b;
                });
            }

            var sys = Spawn(P + "Systems/LevelSystems.prefab", null, Vector3.zero, "LevelSystems");
            Set(sys.GetComponent<LevelGoal>(), "nextScene", p => p.stringValue = c.next);

            var boundsGo = new GameObject("CameraBounds");
            var bounds = boundsGo.AddComponent<BoxCollider2D>();
            bounds.isTrigger = true;
            bounds.offset = new Vector2(w * 0.5f, h * 0.5f);
            bounds.size = new Vector2(w, h);

            var cam = Camera.main;
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            Set(follow, "bounds", p => p.objectReferenceValue = bounds);
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f);

            // mood gelap (disetujui 10 Nov): yang terbaca hanya sumber cahaya. Light2D lewat SerializedObject
            // supaya editor asmdef tidak perlu referensi ke assembly URP 2D.
            foreach (var comp in Find(scene, "Global Light 2D").GetComponents<Component>())
                if (comp != null && comp.GetType().Name == "Light2D")
                    Set(comp, "m_Intensity", p => p.floatValue = 0.16f);

            var note = Object.FindFirstObjectByType<SceneNote>();
            if (note != null) Set(note, "note", p => p.stringValue = c.note);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Blockout selesai: " + c.scene);
        }
    }
}
