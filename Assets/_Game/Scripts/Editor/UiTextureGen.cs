using System.IO;
using UnityEditor;
using UnityEngine;

namespace GooNapped.EditorTools
{
    // Membuat tekstur pixel kecil untuk UI bertema dungeon (PLACEHOLDER sampai ada art dari tim desainer).
    // Hasil: Assets/_Game/Art/UI/Generated/*.png, dipakai MainMenu.uss / Options.uss / Hud.uss lewat background-image.
    //   ui_brick.png   80x40  bata gelap (diulang di slab)
    //   ui_iron.png    16x40  batang besi + paku kuningan (diulang vertikal)
    //   ui_ribbon.png  64x16  pita kain merah darah, ujung kanan sobek (9-slice)
    //   ui_glow.png    64x64  cahaya bulat lembut (obor, halo)
    //   ui_paper.png   64x32  kertas tua bernoda (stiker judul, 9-slice)
    public static class UiTextureGen
    {
        private const string Dir = "Assets/_Game/Art/UI/Generated";

        [MenuItem("GooNapped/UI/Generate Dungeon Textures")]
        public static void Generate()
        {
            Directory.CreateDirectory(Dir);
            Save("ui_brick", Brick(), TextureWrapMode.Repeat);
            Save("ui_iron", Iron(), TextureWrapMode.Repeat);
            Save("ui_ribbon", Ribbon(), TextureWrapMode.Clamp);
            Save("ui_glow", Glow(), TextureWrapMode.Clamp, FilterMode.Bilinear);
            Save("ui_paper", Paper(), TextureWrapMode.Clamp);
            AssetDatabase.Refresh();
            Debug.Log("UiTextureGen: tekstur dungeon dibuat di " + Dir);
        }

        private static Color32 C(int r, int g, int b, int a = 255) => new Color32((byte)r, (byte)g, (byte)b, (byte)a);

        private static Texture2D New(int w, int h, Color32 fill)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = fill;
            t.SetPixels32(px);
            return t;
        }

        // bata 2 baris, nat gelap, sedikit noda/retak
        private static Texture2D Brick()
        {
            int w = 80, h = 40;
            var mortar = C(12, 10, 11);
            var t = New(w, h, mortar);
            var rng = new System.Random(5);
            for (int row = 0; row < 2; row++)
            {
                int off = row == 0 ? 0 : 20;
                for (int bx = -40; bx < w; bx += 40)
                {
                    int x0 = bx + off, y0 = row * 20;
                    int shade = rng.Next(0, 4);
                    var baseC = shade == 0 ? C(29, 24, 26) : shade == 1 ? C(25, 21, 23) : C(22, 19, 21);
                    for (int y = y0 + 1; y < y0 + 19; y++)
                        for (int x = x0 + 1; x < x0 + 39; x++)
                        {
                            int xx = ((x % w) + w) % w;
                            var c = baseC;
                            if (y == y0 + 1) c = C(baseC.r + 6, baseC.g + 5, baseC.b + 5);       // tepi atas terang
                            if (y == y0 + 18) c = C(baseC.r - 6, baseC.g - 5, baseC.b - 5);      // tepi bawah gelap
                            if (rng.Next(0, 23) == 0) c = C(baseC.r + 4, baseC.g + 3, baseC.b + 3); // butir
                            t.SetPixel(xx, h - 1 - y, c);
                        }
                }
            }
            // retak kecil
            for (int i = 0; i < 6; i++) { int x = rng.Next(2, w - 2), y = rng.Next(2, h - 2); t.SetPixel(x, y, mortar); t.SetPixel(x + 1, y - 1, mortar); }
            return t;
        }

        // batang besi vertikal dengan paku kuningan di tengah
        private static Texture2D Iron()
        {
            int w = 16, h = 40;
            var t = New(w, h, C(0, 0, 0, 0));
            for (int y = 0; y < h; y++)
                for (int x = 2; x < 14; x++)
                {
                    var c = x == 2 || x == 13 ? C(20, 18, 18) : x == 3 ? C(96, 84, 86) : x < 8 ? C(66, 58, 56) : C(52, 46, 46);
                    t.SetPixel(x, y, c);
                }
            // paku
            int cy = 20;
            for (int y = cy - 3; y <= cy + 2; y++)
                for (int x = 5; x <= 10; x++)
                {
                    bool corner = (x == 5 || x == 10) && (y == cy - 3 || y == cy + 2);
                    if (corner) continue;
                    var c = C(201, 160, 82);
                    if (x <= 6 && y >= cy) c = C(236, 204, 128);     // kilap
                    if (x >= 9 || y <= cy - 2) c = C(132, 98, 44);   // bayang
                    t.SetPixel(x, y, c);
                }
            return t;
        }

        // pita kain: badan merah darah, garis lipat, ujung kanan sobek bergerigi
        private static Texture2D Ribbon()
        {
            int w = 64, h = 16;
            var t = New(w, h, C(0, 0, 0, 0));
            int[] tear = { 0, 3, 1, 5, 2, 6, 1, 4, 0, 3, 2, 5, 1, 4, 2, 0 };
            for (int y = 0; y < h; y++)
            {
                int end = w - 8 + tear[y];
                for (int x = 0; x < end; x++)
                {
                    var c = C(122, 24, 36);
                    if (y == h - 2) c = C(150, 40, 48);              // tepi atas terang
                    if (y <= 1) c = C(74, 14, 20);                    // tepi bawah gelap
                    if (y == 6 && x % 7 < 4) c = C(104, 20, 30);      // serat kain
                    if (x >= end - 2) c = C(92, 18, 26);              // serat sobek
                    t.SetPixel(x, y, c);
                }
            }
            return t;
        }

        private static Texture2D Glow()
        {
            int s = 64;
            var t = New(s, s, C(0, 0, 0, 0));
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s / 2f, s / 2f)) / (s / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            return t;
        }

        // kertas tua: krem bernoda, tepi agak gelap
        private static Texture2D Paper()
        {
            int w = 64, h = 32;
            var t = New(w, h, C(203, 189, 152));
            var rng = new System.Random(9);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = C(203, 189, 152);
                    if (x < 2 || x > w - 3 || y < 2 || y > h - 3) c = C(176, 160, 122);
                    else if (rng.Next(0, 17) == 0) c = C(190, 175, 138);
                    t.SetPixel(x, y, c);
                }
            for (int i = 0; i < 3; i++)
            {
                int cx = rng.Next(8, w - 8), cy = rng.Next(6, h - 6);
                for (int y = -2; y <= 2; y++) for (int x = -3; x <= 3; x++) t.SetPixel(cx + x, cy + y, C(186, 168, 128));
            }
            return t;
        }

        private static void Save(string name, Texture2D tex, TextureWrapMode wrap, FilterMode filter = FilterMode.Point)
        {
            tex.Apply();
            string path = Dir + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Default;
                ti.filterMode = filter;
                ti.wrapMode = wrap;
                ti.mipmapEnabled = false;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
        }
    }
}
