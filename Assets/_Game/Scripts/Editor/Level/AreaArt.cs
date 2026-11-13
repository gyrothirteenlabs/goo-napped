using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GooNapped.EditorTools
{
    // Art FONDASI per area (AI, 13 Nov): tile + dekor + props dari art/tools/gen_area_art.py. SEMENTARA, bukan desain final.
    // Dipakai LevelBlockout saat Config.area diisi ("garden", "dungeon"). Area lain tetap tile placeholder lama.
    //   - Lantai: beberapa variasi, dipilih per sel (acak tetap/seeded, jadi build ulang = hasil sama).
    //   - Tembok: RuleTile. Sel tembok yang di bawahnya bukan tembok = sisi MUKA (variasi acak), selain itu = ATAS.
    //   - Dekor: tile tanpa collider di Grid/Decor (sebagian kecil sel lantai).
    //   - Taman: pohon di tengah massa pagar tanaman (sel tembok yang dikelilingi tembok).
    // Menu: GooNapped > Art > Pasang sprite props fondasi (menukar sprite placeholder di prefab props).
    public static class AreaArt
    {
        const string TileOut = "Assets/_Game/Art/Tiles/Area/";
        const string Spr = "Assets/_Game/Art/Sprites/";

        public class Set
        {
            public TileBase[] floor;
            public TileBase wall;
            public TileBase[] decor;
            public float decorChance;
            public Sprite mud, water;
            public Sprite[] trees;     // taman saja
            public Sprite[] hideSmall, hideBig;   // tampilan tempat sembunyi T (muat 1) / P (muat 2); null = prefab bawaan
            public TileBase carpet;    // kastil: karpet di bagian dalam ruangan/koridor
            public Sprite[] wallProps; // dekor berdiri di depan tembok, tanpa collider/cahaya: kandelabra (kastil, puncak), rak ramuan (penjara)
            public TileBase[] path;    // taman: jalan tanah dari spawn ke ventilasi tujuan
            public Sprite machine;     // puncak: mesin penyadap 96 px di tembok belakang toples raja
        }

        static readonly Dictionary<string, Set> cache = new Dictionary<string, Set>();

        public static Set Get(string area)
        {
            if (string.IsNullOrEmpty(area)) return null;
            if (cache.TryGetValue(area, out var s) && s.wall != null) return s;
            s = area == "garden" ? Build("Garden", "gdn_grass", "gdn_hedge", "gdn_decor", 0.10f)
              : area == "dungeon" ? Build("Dungeon", "dng_floor", "dng_wall", "dng_decor", 0.12f)
              : area == "castle" ? Build("Castle", "cst_floor", "cst_wall", "cst_decor", 0.08f)
              : area == "summit" ? Build("Summit", "smt_floor", "smt_wall", "smt_decor", 0.10f)
              : null;
            if (s == null) { Debug.LogWarning("AreaArt: area '" + area + "' belum punya art fondasi."); return null; }
            var mw = Frames(Spr + "Tiles/Garden/env_mud_water.png");
            if (mw.Length >= 2) { s.mud = mw[0]; s.water = mw[1]; }
            if (area == "garden")
            {
                s.trees = Frames(Spr + "Props/Garden/prop_tree_c96.png");
                var bush = Frames(Spr + "Props/Garden/prop_bush.png");
                if (bush.Length > 0) s.hideSmall = s.hideBig = new[] { bush[0] };
                var dirt = Frames(Spr + "Tiles/Garden/gdn_dirt.png");
                s.path = new TileBase[dirt.Length];
                for (int i = 0; i < dirt.Length; i++) s.path[i] = SimpleTile("gdn_dirt_" + i, dirt[i], Tile.ColliderType.None);
            }
            if (area == "castle")
            {
                var small = new List<Sprite>();
                small.AddRange(First(Spr + "Props/Castle/prop_cloche.png"));
                small.AddRange(First(Spr + "Props/Castle/prop_pot.png"));
                small.AddRange(First(Spr + "Props/Castle/prop_curtain.png"));
                s.hideSmall = small.ToArray();
                s.hideBig = First(Spr + "Props/Castle/prop_wardrobe.png");
                var carpet = Frames(Spr + "Tiles/Castle/cst_carpet.png");
                if (carpet.Length > 0) s.carpet = SimpleTile("cst_carpet_0", carpet[0], Tile.ColliderType.None);
            }
            if (area == "castle" || area == "summit")
                s.wallProps = First(Spr + "Props/Castle/prop_candelabra.png");
            if (area == "dungeon")
                s.wallProps = First(Spr + "Props/Dungeon/prop_shelf.png");
            if (area == "summit")
            {
                var m = First(Spr + "Props/Summit/prop_drainmachine_c96.png");
                if (m.Length > 0) s.machine = m[0];
            }
            cache[area] = s;
            return s;
        }

        static Set Build(string folder, string floorName, string wallName, string decorName, float decorChance)
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Art/Tiles/Area")) AssetDatabase.CreateFolder("Assets/_Game/Art/Tiles", "Area");
            var floorFrames = Frames(Spr + "Tiles/" + folder + "/" + floorName + ".png");
            var wallFrames = Frames(Spr + "Tiles/" + folder + "/" + wallName + ".png");
            var decorFrames = Frames(Spr + "Tiles/" + folder + "/" + decorName + ".png");
            if (floorFrames.Length == 0 || wallFrames.Length < 2) { Debug.LogError("AreaArt: sprite " + folder + " belum ada. Jalankan python art/tools/gen_area_art.py."); return null; }

            var s = new Set { decorChance = decorChance };
            s.floor = new TileBase[floorFrames.Length];
            for (int i = 0; i < floorFrames.Length; i++) s.floor[i] = SimpleTile(floorName + "_" + i, floorFrames[i], Tile.ColliderType.None);
            s.decor = new TileBase[decorFrames.Length];
            for (int i = 0; i < decorFrames.Length; i++) s.decor[i] = SimpleTile(decorName + "_" + i, decorFrames[i], Tile.ColliderType.None);

            // tembok: frame 0 = atas, sisanya = muka
            string path = TileOut + wallName + "_Rule.asset";
            var rt = AssetDatabase.LoadAssetAtPath<RuleTile>(path);
            if (rt == null) { rt = ScriptableObject.CreateInstance<RuleTile>(); AssetDatabase.CreateAsset(rt, path); }
            rt.m_DefaultSprite = wallFrames[0];
            rt.m_DefaultColliderType = Tile.ColliderType.Grid;
            var faces = new Sprite[wallFrames.Length - 1];
            System.Array.Copy(wallFrames, 1, faces, 0, faces.Length);
            var rule = new RuleTile.TilingRule
            {
                m_Sprites = faces,
                m_Output = RuleTile.TilingRuleOutput.OutputSprite.Random,
                m_RandomTransform = RuleTile.TilingRuleOutput.Transform.Fixed,
                m_PerlinScale = 0.9f,
                m_ColliderType = Tile.ColliderType.Grid,
                m_NeighborPositions = new List<Vector3Int> { new Vector3Int(0, -1, 0) },
                m_Neighbors = new List<int> { RuleTile.TilingRuleOutput.Neighbor.NotThis },
            };
            rt.m_TilingRules = new List<RuleTile.TilingRule> { rule };
            EditorUtility.SetDirty(rt);
            s.wall = rt;
            return s;
        }

        static Tile SimpleTile(string name, Sprite sprite, Tile.ColliderType collider)
        {
            string path = TileOut + name + ".asset";
            var t = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (t == null) { t = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(t, path); }
            t.sprite = sprite; t.color = Color.white; t.colliderType = collider; t.transform = Matrix4x4.identity;
            EditorUtility.SetDirty(t);
            return t;
        }

        public static Sprite[] Frames(string pngPath)
        {
            var list = new List<Sprite>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(pngPath)) if (o is Sprite sp) list.Add(sp);
            list.Sort((a, b) => FrameIndex(a.name).CompareTo(FrameIndex(b.name)));
            return list.ToArray();
        }

        static int FrameIndex(string n)
        {
            int u = n.LastIndexOf('_');
            return u >= 0 && int.TryParse(n.Substring(u + 1), out int i) ? i : 0;
        }

        // Acak tetap per sel (sama tiap build).
        public static int Hash(int x, int y, int salt = 0)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663 ^ salt * 83492791;
                h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                return h & 0x7fffffff;
            }
        }

        static Sprite[] First(string png)
        {
            var f = Frames(png);
            return f.Length > 0 ? new[] { f[0] } : new Sprite[0];
        }

        public static Sprite HidingLook(Set s, bool big, int x, int y)
        {
            var arr = big ? s.hideBig : s.hideSmall;
            if (arr == null || arr.Length == 0) return null;
            return arr[Hash(x, y, 5) % arr.Length];
        }

        public static TileBase FloorAt(Set s, int x, int y)
        {
            // variasi pertama paling sering supaya tidak ramai
            int r = Hash(x, y, 1) % 10;
            int i = r < 6 ? 0 : 1 + (r % (s.floor.Length - 1));
            return s.floor[Mathf.Clamp(i, 0, s.floor.Length - 1)];
        }

        public static TileBase DecorAt(Set s, int x, int y)
        {
            if (s.decor.Length == 0 || (Hash(x, y, 2) % 1000) / 1000f >= s.decorChance) return null;
            return s.decor[Hash(x, y, 3) % s.decor.Length];
        }

        // ---------------- props ----------------

        [MenuItem("GooNapped/Art/Pasang sprite props fondasi")]
        public static void ApplyPropSprites()
        {
            const string P = "Assets/_Game/Prefabs/";
            var barrel = Frames(Spr + "Props/Dungeon/prop_barrel.png");
            var crate = Frames(Spr + "Props/Dungeon/prop_crate.png");
            var vent = Frames(Spr + "Props/Dungeon/prop_vent.png");
            var jar = Frames(Spr + "Props/Dungeon/prop_jar.png");
            var door = Frames(Spr + "Props/Dungeon/prop_celldoor.png");
            var torch = Frames(Spr + "Props/Dungeon/prop_torch.png");
            int n = 0;
            n += SwapRenderer(P + "Level/HidingSpot_Barrel.prefab", barrel, 0);
            n += SwapRenderer(P + "Level/HidingSpot_Crate.prefab", crate, 0);
            n += SwapRenderer(P + "Level/Vent.prefab", vent, 0);
            n += SwapRenderer(P + "Level/Vent_Exit.prefab", vent, 1);
            n += SwapRenderer(P + "Props/Jar.prefab", jar, 0, "Jar");
            n += SwapRenderer(P + "Level/Lantern.prefab", torch, 0);
            if (door.Length >= 2)
            {
                var root = PrefabUtility.LoadPrefabContents(P + "Level/Door.prefab");
                var d = root.GetComponent<Door>();
                if (d != null)
                {
                    var so = new SerializedObject(d);
                    so.FindProperty("closedSprite").objectReferenceValue = door[0];
                    so.FindProperty("openSprite").objectReferenceValue = door[1];
                    so.ApplyModifiedPropertiesWithoutUndo();
                    var sr = root.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sprite = door[0];
                    PrefabUtility.SaveAsPrefabAsset(root, P + "Level/Door.prefab");
                    n++;
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
            // vas rapuh (utuh/pecah), katup aura (tertutup/terbalik), toples raja 96 px
            var vase = Frames(Spr + "Props/Castle/prop_vase.png");
            if (vase.Length >= 2) n += SetSprites(P + "Level/FragileVase.prefab", vase[0], new[] { ("brokenSprite", vase[1]) }, 1f);
            var valve = Frames(Spr + "Props/Summit/prop_valve.png");
            if (valve.Length >= 2) n += SetSprites(P + "Level/AuraValve.prefab", valve[0], new[] { ("closedSprite", valve[0]), ("reversedSprite", valve[1]) }, 1f);
            var king = Frames(Spr + "Props/Summit/prop_kingjar_c96.png");
            if (king.Length >= 1) n += SetSprites(P + "Level/KingJar.prefab", king[0], new (string, Sprite)[0], 1f);
            Debug.Log("AreaArt: sprite props fondasi dipasang di " + n + " prefab.");
        }

        // Sprite renderer pertama + field Sprite di komponen mana pun di prefab. visualScale: skala anak renderer (toples raja 96 px = 1).
        static int SetSprites(string prefab, Sprite main, (string field, Sprite sprite)[] fields, float visualScale)
        {
            var root = PrefabUtility.LoadPrefabContents(prefab);
            var sr = root.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                sr.sprite = main;
                if (sr.transform != root.transform) sr.transform.localScale = Vector3.one * visualScale;
            }
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var so = new SerializedObject(mb);
                bool changed = false;
                foreach (var (field, sprite) in fields)
                {
                    var p = so.FindProperty(field);
                    if (p != null && p.propertyType == SerializedPropertyType.ObjectReference) { p.objectReferenceValue = sprite; changed = true; }
                }
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, prefab);
            PrefabUtility.UnloadPrefabContents(root);
            return 1;
        }

        static int SwapRenderer(string prefab, Sprite[] frames, int frame, string rendererName = null)
        {
            if (frames.Length <= frame) { Debug.LogWarning("AreaArt: sprite untuk " + prefab + " belum ada."); return 0; }
            var root = PrefabUtility.LoadPrefabContents(prefab);
            int done = 0;
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (rendererName != null ? sr.name != rendererName : (sr.sprite == null || !sr.sprite.name.Contains("placeholder") && !sr.sprite.name.StartsWith("torch") && !sr.sprite.name.StartsWith("prop_"))) continue;
                sr.sprite = frames[frame];
                done++;
                break;
            }
            if (done > 0) PrefabUtility.SaveAsPrefabAsset(root, prefab);
            PrefabUtility.UnloadPrefabContents(root);
            return done;
        }
    }
}
