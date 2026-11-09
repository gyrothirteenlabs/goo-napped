using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Auto-setting import untuk semua sprite di Assets/Sprites/ (spek art/CLAUDE.md):
// Sprite Multiple, slice grid 48x48, PPU 48, Filter Point, Compression None, pivot Bottom Center.
public class PixelSpriteImporter : AssetPostprocessor
{
    const int Cell = 48;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Sprites/")) return;
        var ti = (TextureImporter)assetImporter;

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = Cell;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
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

        // Sudah di-slice grid yang benar → biarkan, supaya spriteID tetap stabil.
        // (Unity 6 bisa auto-slice per pulau alpha, jumlahnya bisa kebetulan sama, jadi cek rect-nya juga.)
        var existing = provider.GetSpriteRects();
        bool gridOk = existing.Length == cols * rows;
        foreach (var e in existing)
            if (e.rect.width != Cell || e.rect.height != Cell || e.pivot != new Vector2(0.5f, 0f)) { gridOk = false; break; }
        if (gridOk) return;

        string baseName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        var rects = new List<SpriteRect>();
        int i = 0;
        for (int r = rows - 1; r >= 0; r--)      // baris atas dulu
            for (int c = 0; c < cols; c++)
                rects.Add(new SpriteRect
                {
                    name = $"{baseName}_{i++}",
                    rect = new Rect(c * Cell, r * Cell, Cell, Cell),
                    alignment = SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                });

        provider.SetSpriteRects(rects.ToArray());
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (names != null)
        {
            var pairs = new List<SpriteNameFileIdPair>();
            foreach (var sr in rects) pairs.Add(new SpriteNameFileIdPair(sr.name, sr.spriteID));
            names.SetNameFileIdPairs(pairs);
        }
        provider.Apply();
    }
}
