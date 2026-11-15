using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Potret slime untuk HUD: ambil frame "idle front" dari SlimeSpriteAnimator lalu warnai seperti shader
    // PixelToonLit (gradient-map: abu < 0.2 = outline, sisanya band 1..4 dari material aura sekarang).
    // Sprite tidak perlu Read/Write: pikselnya disalin lewat GPU (Blit + ReadPixels). Hasil di-cache per sprite+material.
    public static class SlimePortrait
    {
        private static readonly Dictionary<(int, int), Texture2D> cache = new Dictionary<(int, int), Texture2D>();

        public struct Frames { public Sprite body, face; }

        public static Frames IdleFront(SlimePiece piece)
        {
            var f = new Frames();
            if (piece == null) return f;
            var anim = piece.GetComponentInChildren<SlimeSpriteAnimator>();
            if (anim != null)
            {
                foreach (var c in anim.clips)
                    if (c != null && c.anim == "idle" && c.dir == "front")
                    {
                        if (c.body != null && c.body.Length > 0) f.body = c.body[0];
                        if (c.face != null && c.face.Length > 0) f.face = c.face[0];
                        break;
                    }
                if (f.body == null && anim.bodyRenderer != null) f.body = anim.bodyRenderer.sprite;
                if (f.face == null && anim.faceRenderer != null) f.face = anim.faceRenderer.sprite;
            }
            return f;
        }

        // Tekstur badan yang sudah diwarnai sesuai material (null material = warna asli sprite).
        public static Texture2D Body(Sprite sprite, Material mat) => Get(sprite, mat, true);

        // Tekstur wajah apa adanya.
        public static Texture2D Face(Sprite sprite) => Get(sprite, null, false);

        private static Texture2D Get(Sprite sprite, Material mat, bool recolor)
        {
            if (sprite == null) return null;
            var key = (sprite.GetInstanceID(), mat != null && recolor ? mat.GetInstanceID() : 0);
            if (cache.TryGetValue(key, out var hit) && hit != null) return hit;

            var src = sprite.texture;
            Rect r = sprite.textureRect;
            int w = Mathf.Max(1, (int)r.width), h = Mathf.Max(1, (int)r.height);
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.ReadPixels(new Rect(r.x, r.y, w, h), 0, 0);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            if (recolor && mat != null && mat.HasProperty("_Band1"))
            {
                Color outline = mat.GetColor("_Outline"), b1 = mat.GetColor("_Band1"), b2 = mat.GetColor("_Band2"),
                      b3 = mat.GetColor("_Band3"), b4 = mat.GetColor("_Band4");
                Vector4 edges = mat.HasProperty("_BandEdges") ? mat.GetVector("_BandEdges") : new Vector4(0.3f, 0.55f, 0.85f, 0);
                var px = tex.GetPixels();
                for (int i = 0; i < px.Length; i++)
                {
                    var c = px[i];
                    if (c.a < 0.01f) continue;
                    float g = c.r;
                    // cahaya palsu dari kiri atas supaya potret punya volume (di game, band ditentukan lampu 2D)
                    float u = (i % w) / (float)w, v = (i / w) / (float)h;
                    float inten = g * Mathf.Lerp(0.7f, 1.25f, v * 0.7f + (1f - u) * 0.3f);
                    Color o = g < 0.2f ? outline : inten < edges.x ? b1 : inten < edges.y ? b2 : inten < edges.z ? b3 : b4;
                    o.a = c.a;
                    px[i] = o;
                }
                tex.SetPixels(px);
            }
            tex.Apply();
            cache[key] = tex;
            return tex;
        }
    }
}
