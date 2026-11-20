using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GooNapped.EditorTools
{
    // Cahaya lilin dekorasi (20 Nov 2026): tiap api lilin yang TERGAMBAR di sprite perabot / dekor tembok / tile tembok
    // diberi Light2D kecil yang berkedip (prefab Prefabs/Level/CandleLight, komponen DecorLight).
    //   - Api dideteksi otomatis dari pola api lilin generator (art/tools): pixel inti api ffc861 dengan pixel oranye f08a2e
    //     TEPAT di bawahnya. Warna saja tidak cukup: botol ramuan kuning di rak dan bunga taman juga ffc861.
    //     Dibaca dari file PNG, jadi sprite lilin baru otomatis ikut. Titik api berdekatan (<= 8 px) jadi satu cahaya.
    //   - Obor/lentera (Extinguishable) dan esens dilewati: obor sudah punya cahaya sendiri (TorchFlicker).
    //   - Tile: hanya tilemap "Walls". Satu cahaya per tile tembok berlilin (WallSpacing), supaya tidak ada ceruk yang
    //     apinya tergambar tapi gelap.
    //   - DecorLight tidak dihitung LightExposure: murni visual, aturan "cahaya = bahaya" tidak berubah.
    // Dipanggil LevelBlockout.Build setelah semua props terpasang. Menu manual: GooNapped > Art > Pasang cahaya lilin.
    public static class CandleLights
    {
        const string PrefabPath = "Assets/_Game/Prefabs/Level/CandleLight.prefab";
        static readonly Color32 Flame = new Color32(0xff, 0xc8, 0x61, 0xff);
        static readonly Color32 FlameBase = new Color32(0xf0, 0x8a, 0x2e, 0xff);
        const int Tolerance = 10, MergePx = 8;
        const float MinSpacing = 0.9f;       // unit: dua cahaya lilin perabot tidak lebih rapat dari ini
        const float WallSpacing = 1.2f;      // unit: lilin di tile tembok: tiap ceruk dapat cahaya, tapi tidak dobel per ceruk

        static readonly Dictionary<string, Color32[]> pixelCache = new Dictionary<string, Color32[]>();
        static readonly Dictionary<string, int> widthCache = new Dictionary<string, int>();
        static readonly Dictionary<Sprite, List<Vector2>> spotCache = new Dictionary<Sprite, List<Vector2>>();

        [MenuItem("GooNapped/Art/Pasang cahaya lilin (scene aktif)")]
        static void Menu()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var props = GameObject.Find("LevelProps");
            int n = Place(props != null ? props.transform : null);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("CandleLights: " + n + " cahaya lilin di " + scene.name);
        }

        // Pasang cahaya di bawah parent "CandleLights" (dibuat ulang tiap dipanggil). Kembalikan jumlah cahaya.
        public static int Place(Transform propRoot)
        {
            pixelCache.Clear(); widthCache.Clear(); spotCache.Clear();
            var prefab = EnsurePrefab();
            if (prefab == null) return 0;
            var old = GameObject.Find("CandleLights");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("CandleLights").transform;
            if (propRoot != null) root.SetParent(propRoot, false);

            var placed = new List<Vector2>();
            void Add(Vector2 p, float spacing)
            {
                foreach (var q in placed) if ((q - p).sqrMagnitude < spacing * spacing) return;
                placed.Add(p);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
                go.transform.position = new Vector3(p.x, p.y, 0f);
            }

            // sprite renderer di props (perabot, dekor tembok, mesin, serakan), kecuali obor dan esens
            if (propRoot != null)
                foreach (var sr in propRoot.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.sprite == null || sr.GetComponentInParent<Extinguishable>() != null || sr.GetComponentInParent<IngredientPickup>() != null) continue;
                    foreach (var s in Spots(sr.sprite))
                    {
                        var local = new Vector2(sr.flipX ? -s.x : s.x, sr.flipY ? -s.y : s.y);
                        Add(sr.transform.TransformPoint(local), MinSpacing);
                    }
                }

            // tile tembok (ceruk lilin, lilin dinding)
            foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                if (tm.name != "Walls") continue;
                var b = tm.cellBounds;
                foreach (var cell in b.allPositionsWithin)
                {
                    var spr = tm.GetSprite(cell);
                    if (spr == null) continue;
                    var spots = Spots(spr);
                    if (spots.Count == 0) continue;
                    var m = tm.GetTransformMatrix(cell);
                    Vector3 center = tm.GetCellCenterWorld(cell);
                    foreach (var s in spots) Add(center + tm.transform.TransformVector(m.MultiplyVector(s)), WallSpacing);
                }
            }
            if (placed.Count == 0) Object.DestroyImmediate(root.gameObject);
            return placed.Count;
        }

        // Titik api di sprite, dalam unit relatif pivot sprite.
        public static List<Vector2> Spots(Sprite s)
        {
            if (spotCache.TryGetValue(s, out var cached)) return cached;
            var list = new List<Vector2>();
            spotCache[s] = list;
            string path = AssetDatabase.GetAssetPath(s.texture);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".png")) return list;
            if (!pixelCache.TryGetValue(path, out var px))
            {
                var tex = new Texture2D(2, 2);
                px = tex.LoadImage(File.ReadAllBytes(path)) ? tex.GetPixels32() : new Color32[0];
                widthCache[path] = tex.width;
                Object.DestroyImmediate(tex);
                pixelCache[path] = px;
            }
            if (px.Length == 0) return list;
            int w = widthCache[path];
            var r = s.rect;
            var clusters = new List<(Vector2 sum, int n)>();
            for (int y = (int)r.y; y < (int)(r.y + r.height); y++)
                for (int x = (int)r.x; x < (int)(r.x + r.width); x++)
                {
                    // tekstur dibaca dari bawah ke atas: pixel "di bawah" pada gambar = baris y - 1
                    if (y - 1 < (int)r.y || !Near(px[y * w + x], Flame) || !Near(px[(y - 1) * w + x], FlameBase)) continue;
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    int hit = -1;
                    for (int i = 0; i < clusters.Count && hit < 0; i++)
                        if ((clusters[i].sum / clusters[i].n - p).sqrMagnitude <= MergePx * MergePx) hit = i;
                    if (hit < 0) clusters.Add((p, 1));
                    else clusters[hit] = (clusters[hit].sum + p, clusters[hit].n + 1);
                }
            float ppu = s.pixelsPerUnit;
            foreach (var (sum, n) in clusters)
            {
                var c = sum / n;
                // texture (kiri bawah) -> relatif pivot (pivot dalam pixel, relatif rect)
                list.Add(new Vector2((c.x - r.x - s.pivot.x) / ppu, (c.y - r.y - s.pivot.y) / ppu));
            }
            return list;
        }

        static bool Near(Color32 c, Color32 t) =>
            c.a >= 200 && Mathf.Abs(c.r - t.r) <= Tolerance && Mathf.Abs(c.g - t.g) <= Tolerance && Mathf.Abs(c.b - t.b) <= Tolerance;

        // Prefab cahaya lilin: salinan Light2D lentera (setelan sorting layer, blend style sama), lalu diperkecil.
        public static GameObject EnsurePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null) return existing;
            var lantern = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Level/Lantern.prefab");
            if (lantern == null) { Debug.LogWarning("CandleLights: prefab Lantern tidak ditemukan."); return null; }
            Component src = null;
            foreach (var comp in lantern.GetComponentsInChildren<Component>(true))
                if (comp != null && comp.GetType().Name == "Light2D") { src = comp; break; }
            if (src == null) { Debug.LogWarning("CandleLights: Light2D lentera tidak ditemukan."); return null; }
            var go = Object.Instantiate(src.gameObject);
            go.name = "CandleLight";
            go.transform.localPosition = Vector3.zero;
            Component l2d = null;
            foreach (var comp in go.GetComponents<Component>()) if (comp != null && comp.GetType().Name == "Light2D") l2d = comp;
            var so = new SerializedObject(l2d);
            so.FindProperty("m_Color").colorValue = new Color(1f, 0.72f, 0.4f, 1f);
            so.FindProperty("m_Intensity").floatValue = 0.7f;
            so.FindProperty("m_PointLightInnerRadius").floatValue = 0.05f;
            so.FindProperty("m_PointLightOuterRadius").floatValue = 1.1f;
            so.FindProperty("m_FalloffIntensity").floatValue = 0.7f;
            so.FindProperty("m_ShadowsEnabled").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.AddComponent<DecorLight>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
