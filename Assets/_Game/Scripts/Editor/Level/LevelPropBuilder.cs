using System.IO;
using UnityEditor;
using UnityEngine;

namespace GooNapped.EditorTools
{
    // Sprite placeholder + prefab untuk props level: pintu, tuas, pelat tekan (biasa & berat), celah (lorong tikus).
    // PLACEHOLDER: gambar kotak-kotak sederhana sampai ada art dari tim desainer. Ganti PNG di Art/Sprites/Props/
    // (nama dan urutan frame sama) dan prefab otomatis ikut.
    //   door_placeholder.png        frame 0 tertutup, 1 terbuka
    //   lever_placeholder.png       frame 0 mati, 1 ditarik
    //   plate_placeholder.png       frame 0 naik, 1 tertekan        (plate_heavy_placeholder.png: sama, versi berat)
    //   crawlspace_placeholder.png  frame 0
    //   barrel_placeholder.png      frame 0 (tempat sembunyi muat 1)
    //   crate_placeholder.png       frame 0 (tempat sembunyi muat 2)
    // Menu: GooNapped > Placeholder > Props level (pintu, tuas, pelat, celah, tempat sembunyi)
    public static class LevelPropBuilder
    {
        const string SpriteDir = "Assets/_Game/Art/Sprites/Props/";
        const string PrefabDir = "Assets/_Game/Prefabs/Level/";
        const int C = 48;

        [MenuItem("GooNapped/Placeholder/Props level (pintu, tuas, pelat, celah, tempat sembunyi)")]
        public static void BuildAll()
        {
            MakeSprites();
            AssetDatabase.Refresh();
            MakePrefabs();
            AssetDatabase.SaveAssets();
            Debug.Log("Props level placeholder selesai: " + PrefabDir);
        }

        // ---------- gambar ----------
        static Color32[] px;
        static int W;

        static void Begin(int frames) { W = C * frames; px = new Color32[W * C]; }
        static void Rect(int x, int y, int w, int h, Color32 c)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    if (i >= 0 && i < W && j >= 0 && j < C) px[j * W + i] = c;
        }
        static void Save(string name)
        {
            var t = new Texture2D(W, C, TextureFormat.RGBA32, false);
            t.SetPixels32(px);
            t.Apply();
            File.WriteAllBytes(SpriteDir + name + ".png", t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }

        static readonly Color32 Wood = new Color32(120, 80, 48, 255), WoodDark = new Color32(84, 54, 32, 255),
            Iron = new Color32(70, 72, 80, 255), IronLight = new Color32(130, 134, 145, 255),
            Stone = new Color32(96, 98, 110, 255), StoneDark = new Color32(62, 64, 74, 255),
            Hole = new Color32(10, 10, 14, 255), Moss = new Color32(70, 110, 60, 255);

        static void MakeSprites()
        {
            // pintu: x 0..47 tertutup, 48..95 terbuka (hanya kusen)
            Begin(2);
            Rect(0, 0, 48, 48, WoodDark);
            for (int i = 0; i < 6; i++) Rect(2 + i * 8, 2, 6, 44, Wood);
            Rect(0, 10, 48, 4, Iron); Rect(0, 34, 48, 4, Iron);
            Rect(38, 22, 4, 4, IronLight);
            Rect(48, 0, 6, 48, WoodDark); Rect(90, 0, 6, 48, WoodDark);
            Rect(48, 44, 48, 4, WoodDark);
            Save("door_placeholder");

            // tuas: alas + gagang miring kiri (mati) / kanan (ditarik)
            Begin(2);
            for (int f = 0; f < 2; f++)
            {
                int ox = f * C;
                Rect(ox + 16, 4, 16, 6, Iron);
                Rect(ox + 18, 10, 12, 3, IronLight);
                for (int k = 0; k < 16; k++)
                {
                    int x = ox + 24 + (f == 0 ? -k / 2 : k / 2);
                    Rect(x - 1, 12 + k, 3, 1, WoodDark);
                }
                int kx = ox + 24 + (f == 0 ? -8 : 8);
                Rect(kx - 3, 27, 6, 5, f == 0 ? new Color32(160, 60, 50, 255) : new Color32(80, 170, 90, 255));
            }
            Save("lever_placeholder");

            // pelat tekan: lempeng batu di tengah tile (pivot bawah tile), tertekan = lebih rendah & gelap
            foreach (bool heavy in new[] { false, true })
            {
                Begin(2);
                for (int f = 0; f < 2; f++)
                {
                    int ox = f * C;
                    var top = f == 0 ? Stone : StoneDark;
                    Rect(ox + 8, 14, 32, 20, StoneDark);
                    Rect(ox + 9, f == 0 ? 18 : 16, 30, f == 0 ? 15 : 14, top);
                    if (heavy)
                    {
                        // tanda "berat": tiga balok kecil
                        Rect(ox + 16, 22, 5, 5, Iron); Rect(ox + 22, 22, 5, 5, Iron); Rect(ox + 19, 27, 5, 4, Iron);
                    }
                    else Rect(ox + 21, 23, 6, 6, Moss);
                }
                Save(heavy ? "plate_heavy_placeholder" : "plate_placeholder");
            }

            // celah: dinding batu dengan lubang gelap kecil di bawah
            Begin(1);
            Rect(0, 0, 48, 48, StoneDark);
            Rect(2, 2, 44, 44, Stone);
            Rect(14, 0, 20, 16, Hole);
            Rect(16, 16, 16, 3, Hole);
            Rect(10, 0, 4, 18, StoneDark); Rect(34, 0, 4, 18, StoneDark);
            Save("crawlspace_placeholder");

            // tong (sembunyi, muat 1): bilah kayu + dua pita besi, tutup gelap di atas
            Begin(1);
            Rect(12, 2, 24, 30, WoodDark);
            for (int i = 0; i < 4; i++) Rect(13 + i * 6, 3, 5, 28, Wood);
            Rect(11, 8, 26, 3, Iron); Rect(11, 23, 26, 3, Iron);
            Rect(13, 32, 22, 4, WoodDark); Rect(15, 33, 18, 2, Hole);
            Save("barrel_placeholder");

            // peti (sembunyi, muat 2): kotak lebar dengan silang penguat
            Begin(1);
            Rect(5, 2, 38, 30, WoodDark);
            Rect(7, 4, 34, 26, Wood);
            for (int k = 0; k < 26; k++) { Rect(7 + k * 34 / 26, 4 + k, 3, 1, WoodDark); Rect(38 - k * 34 / 26, 4 + k, 3, 1, WoodDark); }
            Rect(5, 30, 38, 4, WoodDark);
            Save("crate_placeholder");
        }

        // ---------- prefab ----------
        static Sprite Frame(string sheet, int i)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(SpriteDir + sheet + ".png"))
                if (o is Sprite s && s.name == sheet + "_" + i) return s;
            throw new System.Exception("Sprite tidak ada: " + sheet + "_" + i);
        }

        static void Set(Object target, string prop, System.Action<SerializedProperty> set)
        {
            var so = new SerializedObject(target);
            set(so.FindProperty(prop));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject NewProp(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Env_Lit.mat");
            sr.sortingOrder = order;
            return go;
        }

        static void SavePrefab(GameObject go, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + name + ".prefab");
            Object.DestroyImmediate(go);
        }

        // Semua props ini 1 tile: pivot di tepi bawah tile, collider di tengah tile (offset 0.5).
        static void MakePrefabs()
        {
            {
                var go = NewProp("Door", Frame("door_placeholder", 0), -85);
                var box = go.AddComponent<BoxCollider2D>();
                box.offset = new Vector2(0f, 0.5f);
                box.size = Vector2.one;
                var d = go.AddComponent<Door>();
                Set(d, "spriteRenderer", p => p.objectReferenceValue = go.GetComponent<SpriteRenderer>());
                Set(d, "closedSprite", p => p.objectReferenceValue = Frame("door_placeholder", 0));
                Set(d, "openSprite", p => p.objectReferenceValue = Frame("door_placeholder", 1));
                SavePrefab(go, "Door");
            }
            {
                var go = NewProp("Lever", Frame("lever_placeholder", 0), 0);
                var c = go.AddComponent<CircleCollider2D>();
                c.isTrigger = true;
                c.offset = new Vector2(0f, 0.35f);
                c.radius = 0.3f;
                var l = go.AddComponent<Lever>();
                Set(l, "spriteRenderer", p => p.objectReferenceValue = go.GetComponent<SpriteRenderer>());
                Set(l, "offSprite", p => p.objectReferenceValue = Frame("lever_placeholder", 0));
                Set(l, "onSprite", p => p.objectReferenceValue = Frame("lever_placeholder", 1));
                SavePrefab(go, "Lever");
            }
            foreach (bool heavy in new[] { false, true })
            {
                string sheet = heavy ? "plate_heavy_placeholder" : "plate_placeholder";
                var go = NewProp(heavy ? "PressurePlate_Heavy" : "PressurePlate", Frame(sheet, 0), -80);
                var pp = go.AddComponent<PressurePlate>();
                Set(pp, "spriteRenderer", p => p.objectReferenceValue = go.GetComponent<SpriteRenderer>());
                Set(pp, "upSprite", p => p.objectReferenceValue = Frame(sheet, 0));
                Set(pp, "downSprite", p => p.objectReferenceValue = Frame(sheet, 1));
                Set(pp, "requireHeavy", p => p.boolValue = heavy);
                SavePrefab(go, heavy ? "PressurePlate_Heavy" : "PressurePlate");
            }
            {
                var go = NewProp("Crawlspace", Frame("crawlspace_placeholder", 0), -85);
                var box = go.AddComponent<BoxCollider2D>();
                box.offset = new Vector2(0f, 0.5f);
                box.size = Vector2.one;
                go.AddComponent<Crawlspace>();
                SavePrefab(go, "Crawlspace");
            }
            // tempat sembunyi: root = collider padat + HidingSpot, anak "Visual" = sprite yang bergoyang saat dicurigai
            foreach (bool crate in new[] { false, true })
            {
                string sheet = crate ? "crate_placeholder" : "barrel_placeholder";
                string name = crate ? "HidingSpot_Crate" : "HidingSpot_Barrel";
                var go = new GameObject(name);
                var vis = NewProp("Visual", Frame(sheet, 0), 0);
                vis.transform.SetParent(go.transform, false);
                var box = go.AddComponent<BoxCollider2D>();
                box.offset = new Vector2(0f, crate ? 0.36f : 0.38f);
                box.size = crate ? new Vector2(0.82f, 0.62f) : new Vector2(0.56f, 0.66f);
                var hs = go.AddComponent<HidingSpot>();
                Set(hs, "capacity", p => p.intValue = crate ? 2 : 1);
                Set(hs, "visual", p => p.objectReferenceValue = vis.transform);
                SavePrefab(go, name);
            }
        }
    }
}
