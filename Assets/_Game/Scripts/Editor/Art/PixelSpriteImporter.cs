using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Auto-setting import untuk semua sprite di folder sprite game (spek art/CLAUDE.md):
// Sprite Multiple, slice grid 48x48, PPU 48, Filter Point, Compression None, pivot Bottom Center.
// KECUALI folder Sprites/Tiles/: pivot Center, karena Tilemap menaruh sprite di tengah sel. Dengan pivot bawah,
// gambar tile naik setengah sel dari collider-nya (dan menggeser lewat Tile.transform/anchor ikut menggeser collider).
//
// Normal map: file yang namanya mengandung "_normal" diimport linear (bukan sRGB) dan otomatis
// dipasang sebagai secondary texture "_NormalMap" ke sprite pasangannya:
//   slime_body_idle_front.png  <->  slime_normal_idle_front.png   (token _body_/_albedo_ -> _normal_)
//   crate.png                  <->  crate_normal.png              (selain itu: tambah _normal)
public class PixelSpriteImporter : AssetPostprocessor
{
    public const string Root = "Assets/_Game/Art/Sprites/";
    public const string TilesRoot = Root + "Tiles/";
    const int DefaultCell = 48;
    // Sprite besar: akhiran _c<N> di nama file = sel N px (contoh prop_tree_c96.png). PPU tetap 48.
    int Cell
    {
        get
        {
            var m = System.Text.RegularExpressions.Regex.Match(Path.GetFileNameWithoutExtension(assetPath), @"_c(\d+)(_normal)?$");
            return m.Success ? int.Parse(m.Groups[1].Value) : DefaultCell;
        }
    }

    Vector2 Pivot => assetPath.StartsWith(TilesRoot) ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f);
    SpriteAlignment Alignment => assetPath.StartsWith(TilesRoot) ? SpriteAlignment.Center : SpriteAlignment.BottomCenter;

    static bool IsNormal(string path) => Path.GetFileNameWithoutExtension(path).Contains("_normal");

    public static string NormalPathFor(string albedoPath)
    {
        string dir = Path.GetDirectoryName(albedoPath).Replace('\\', '/');
        string name = Path.GetFileNameWithoutExtension(albedoPath);
        string n = name.Contains("_body_") ? name.Replace("_body_", "_normal_")
                 : name.Contains("_albedo_") ? name.Replace("_albedo_", "_normal_")
                 : name + "_normal";
        return $"{dir}/{n}.png";
    }

    static string AlbedoPathFor(string normalPath)
    {
        string dir = Path.GetDirectoryName(normalPath).Replace('\\', '/');
        string name = Path.GetFileNameWithoutExtension(normalPath);
        if (name.EndsWith("_normal")) return $"{dir}/{name.Substring(0, name.Length - 7)}.png";
        string body = $"{dir}/{name.Replace("_normal_", "_body_")}.png";
        return File.Exists(body) ? body : $"{dir}/{name.Replace("_normal_", "_albedo_")}.png";
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root)) return;
        var ti = (TextureImporter)assetImporter;
        bool normal = IsNormal(assetPath);

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = DefaultCell;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = !normal;
        ti.sRGBTexture = !normal;
        ti.wrapMode = TextureWrapMode.Clamp;
        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; // aman untuk draw mode Tiled
        ti.SetTextureSettings(settings);

        ti.GetSourceTextureWidthAndHeight(out int w, out int h);
        int cols = w / Cell, rows = h / Cell;
        if (cols < 1 || rows < 1) return;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(ti);
        provider.InitSpriteEditorDataProvider();

        bool changed = SliceGrid(provider, cols, rows);
        if (!normal) changed |= LinkNormalMap(provider);
        if (changed) provider.Apply();
    }

    bool SliceGrid(ISpriteEditorDataProvider provider, int cols, int rows)
    {
        // Sudah di-slice grid yang benar -> biarkan, supaya spriteID tetap stabil.
        // (Unity 6 bisa auto-slice per pulau alpha, jumlahnya bisa kebetulan sama, jadi cek rect-nya juga.)
        var existing = provider.GetSpriteRects();
        bool gridOk = existing.Length == cols * rows;
        foreach (var e in existing)
            if (e.rect.width != Cell || e.rect.height != Cell || e.pivot != Pivot) { gridOk = false; break; }
        if (gridOk) return false;

        string baseName = Path.GetFileNameWithoutExtension(assetPath);
        var rects = new List<SpriteRect>();
        int i = 0;
        for (int r = rows - 1; r >= 0; r--)      // baris atas dulu
            for (int c = 0; c < cols; c++)
                rects.Add(new SpriteRect
                {
                    name = $"{baseName}_{i++}",
                    rect = new Rect(c * Cell, r * Cell, Cell, Cell),
                    alignment = Alignment,
                    pivot = Pivot,
                });

        provider.SetSpriteRects(rects.ToArray());
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (names != null)
        {
            var pairs = new List<SpriteNameFileIdPair>();
            foreach (var sr in rects) pairs.Add(new SpriteNameFileIdPair(sr.name, sr.spriteID));
            names.SetNameFileIdPairs(pairs);
        }
        return true;
    }

    bool LinkNormalMap(ISpriteEditorDataProvider provider)
    {
        var sec = provider.GetDataProvider<ISecondaryTextureDataProvider>();
        if (sec == null) return false;
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPathFor(assetPath));
        if (tex == null) return false;   // belum diimport; OnPostprocessAllAssets akan reimport nanti
        var cur = sec.textures;
        if (cur != null && cur.Length == 1 && cur[0].name == "_NormalMap" && cur[0].texture == tex) return false;
        sec.textures = new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = tex } };
        return true;
    }

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (var p in imported)
        {
            if (!p.StartsWith(Root) || !p.EndsWith(".png") || !IsNormal(p)) continue;
            string albedo = AlbedoPathFor(p);
            // reimport pasangan albedo supaya LinkNormalMap jalan (tidak memicu reimport normal lagi)
            if (File.Exists(albedo)) AssetDatabase.ImportAsset(albedo, ImportAssetOptions.ForceUpdate);
        }
    }
}
