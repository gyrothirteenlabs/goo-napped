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
    //           b Serbuk Batu   l Lumut Pendar   w Air Embun   r Ramuan Api   L lentera
    //           g/G, h/H, k/K  titik awal / akhir patroli Golem 1, 2, 3 (garis lurus; huruf kecil = posisi awal)
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
            next = "C1_L03",
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

        static readonly Config C1L03 = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/Chapter1/C1_L03.unity",
            recipe = "Recipe_Debu",
            next = "C1_L04",
            note = "BLOCKOUT C1_L03 (urutan makan + 2 golem). Lorong tambang sempit.\n" +
                   "Resep Debu: abu + berkilau. Lumut Pendar (hijau + berkilau) dulu, LALU Serbuk Batu (abu menimpa hijau). Kebalik = warna salah.\n" +
                   "Golem 1 patroli lorong tengah (horizontal), Golem 2 patroli poros tambang (vertikal), jalurnya bersilangan.\n" +
                   "Lumut membuat tubuh menyala (terlihat lebih jauh) selama dibawa ke Serbuk Batu.\n" +
                   "Dibangun oleh GooNapped > Blockout > C1_L03. Layout final: tim desainer.",
            map = new[]
            {
                "########################",
                "#.....#.....h....#..J..#",
                "#..S..#.###...##.......#",
                "#.....#.#.......##..L..#",
                "###.###.#.##.#......####",
                "#g.................G#..#",
                "#.###.####.#.##.###.#..#",
                "#.#l#.....#...#...#....#",
                "#.#.#.L.b.#.H.#.b.#.####",
                "#E..#.....#...#.......##",
                "########################",
            },
        };

        static readonly Config C1L04 = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/Chapter1/C1_L04.unity",
            recipe = "Recipe_Tetua",
            next = "",
            note = "BLOCKOUT C1_L04 (ujian chapter 1). Mulut tambang menuju gerbang kastil, slime tetua.\n" +
                   "Resep Tetua: berat + dingin (Serbuk Batu + Air Embun). Kabur sambil berat = lambat.\n" +
                   "Golem 1 patroli jalan utama, Golem 2 menjaga gerbang (exit). Pancing Golem 2 dengan umpan:\n" +
                   "makan Lumut Pendar, belah, tinggalkan potongan menyala di depan pandangannya, lalu Shift dan menyelinap ke gerbang.\n" +
                   "Dibangun oleh GooNapped > Blockout > C1_L04. Layout final: tim desainer.",
            map = new[]
            {
                "##########################",
                "#S...#..........#....J...#",
                "#....#.l..####..#........#",
                "#....#....#..#..#..L.....#",
                "##.###....#b.#..####.#####",
                "#g.....................G.#",
                "#.####.###.#####.#######.#",
                "#....#.#w#.#...#.#..L..#.#",
                "#.L..#.....#.b.#.#..h..H.#",
                "#...............#.#.....E#",
                "##########################",
            },
        };

        [MenuItem("GooNapped/Blockout/C1_L03")]
        public static void BuildC1L03() => Build(C1L03);

        [MenuItem("GooNapped/Blockout/C1_L04")]
        public static void BuildC1L04() => Build(C1L04);

        [MenuItem("GooNapped/Blockout/Semua chapter 1")]
        public static void BuildChapter1()
        {
            Build(C1L01);
            Build(C1L02);
            Build(C1L03);
            Build(C1L04);
        }

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
            // Sprite proyek ini pivot-nya tengah-bawah (aturan PixelSpriteImporter untuk karakter), sedangkan Tilemap
            // menaruh pivot di tengah sel. Tanpa geser -0.5, gambar tile naik setengah sel dari collision-nya.
            var fix = Matrix4x4.Translate(new Vector3(0f, -0.5f, 0f));
            var t = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (t != null)
            {
                if (t.transform != fix) { t.transform = fix; EditorUtility.SetDirty(t); }
                return t;
            }
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Art/Tiles/Placeholder"))
                AssetDatabase.CreateFolder("Assets/_Game/Art/Tiles", "Placeholder");
            Sprite sprite = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(spritePath))
                if (o is Sprite s && (sprite == null || string.CompareOrdinal(s.name, sprite.name) < 0)) sprite = s;
            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sprite;
            t.color = color;
            t.colliderType = Tile.ColliderType.Grid;
            t.transform = fix;
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
            var floorTile = PlaceholderTile("Tile_Ground", "Assets/_Game/Art/Sprites/Environment/floor_stone.png", new Color(0.7f, 0.6f, 0.5f));
            var rockTile = PlaceholderTile("Tile_Rock", "Assets/_Game/Art/Sprites/Environment/wall_stone.png", new Color(0.4f, 0.42f, 0.5f));

            var floor = Find(scene, "Grid/Floor").GetComponent<Tilemap>();
            var walls = Find(scene, "Grid/Walls").GetComponent<Tilemap>();
            floor.ClearAllTiles();
            walls.ClearAllTiles();
            floor.GetComponent<TilemapRenderer>().sharedMaterial = envMat;
            floor.GetComponent<TilemapRenderer>().sortingOrder = -100;
            walls.GetComponent<TilemapRenderer>().sharedMaterial = envMat;
            walls.GetComponent<TilemapRenderer>().sortingOrder = -90;
            // cahaya lentera tidak tembus tembok: ShadowCaster2D dari TilemapCollider2D (tipe dicari lewat nama
            // supaya editor asmdef tidak butuh referensi ke assembly URP 2D)
            var casterType = System.Type.GetType("UnityEngine.Rendering.Universal.ShadowCaster2D, Unity.RenderPipelines.Universal.2D.Runtime");
            if (casterType != null && walls.GetComponent(casterType) == null)
            {
                var caster = walls.gameObject.AddComponent(casterType);
                Set(caster, "m_CastingOption", p => p.enumValueIndex = 1);   // CastShadow: tembok sendiri tetap terang
            }

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
            const string golemKeys = "ghk";
            var golemStart = new Dictionary<char, Vector2>();
            var golemEnd = new Dictionary<char, Vector2>();
            var ingredients = new Dictionary<char, string>
            {
                { 'b', "SerbukBatu" }, { 'l', "LumutPendar" }, { 'w', "AirEmbun" }, { 'r', "RamuanApi" },
            };
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
                        default:
                            if (golemKeys.IndexOf(ch) >= 0) golemStart[ch] = pos;
                            else if (golemKeys.IndexOf(char.ToLowerInvariant(ch)) >= 0) golemEnd[char.ToLowerInvariant(ch)] = pos;
                            else if (ingredients.TryGetValue(ch, out var id))
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

            int gi = 0;
            foreach (char key in golemKeys)
            {
                if (!golemStart.TryGetValue(key, out var a)) continue;
                var b = golemEnd.TryGetValue(key, out var e) ? e : a;
                var g = Spawn(P + "Enemies/Golem_Placeholder.prefab", guardRoot, a, "Golem_Patroli_" + (++gi));
                Set(g.GetComponent<GolemGuard>(), "patrolPoints", p =>
                {
                    p.arraySize = 2;
                    p.GetArrayElementAtIndex(0).vector2Value = a;
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
                    Set(comp, "m_Intensity", p => p.floatValue = 0.1f);

            var note = Object.FindFirstObjectByType<SceneNote>();
            if (note != null) Set(note, "note", p => p.stringValue = c.note);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Blockout selesai: " + c.scene);
        }
    }
}
