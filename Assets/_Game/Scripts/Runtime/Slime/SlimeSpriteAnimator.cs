using System;
using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Prototipe: mainkan frame body + face bersamaan (2 SpriteRenderer, animasi sama).
    // Arah: front / side (hadap kanan) / back. Kiri = flipX.
    public class SlimeSpriteAnimator : MonoBehaviour
    {
        [Serializable]
        public class Clip
        {
            public string anim;   // idle, move, eat, jelly, split, merge
            public string dir;    // front, side, back
            public Sprite[] body;
            public Sprite[] face;
        }

        public SpriteRenderer bodyRenderer;
        public SpriteRenderer faceRenderer;
        public List<Clip> clips = new();
        public string anim = "idle";
        public string dir = "front";
        public bool faceLeft;
        public float fps = 8f;
        // Pengali tempo (1 = normal). Diatur SlimePiece supaya animasi jalan ikut kecepatan gerak.
        [NonSerialized] public float speedScale = 1f;
        // >= 0: frame dipilih dari fase ini (0..1), bukan dari waktu. Dipakai supaya sprite "move" sinkron dengan lompatan.
        [NonSerialized] public float forcedPhase = -1f;

        Clip current;
        float t;

        // Posisi dalam siklus clip yang sedang main, 0..1. Dipakai SlimeMotor untuk "denyut" gerak.
        public float Phase =>
            current != null && current.body != null && current.body.Length > 0
                ? (t % current.body.Length) / current.body.Length : 0f;

        public void Play(string newAnim, string newDir, bool left)
        {
            if (newAnim == anim && newDir == dir && left == faceLeft && current != null) return;
            bool restart = newAnim != anim;
            anim = newAnim; dir = newDir; faceLeft = left;
            current = Find(anim, dir);
            if (restart) t = 0f;
        }

        Clip Find(string a, string d)
        {
            foreach (var c in clips) if (c.anim == a && c.dir == d) return c;
            foreach (var c in clips) if (c.anim == a) return c;
            return clips.Count > 0 ? clips[0] : null;
        }

        void Update()
        {
            if (current == null || current.anim != anim || current.dir != dir) current = Find(anim, dir);
            if (current == null || current.body == null || current.body.Length == 0) return;

            t += Time.deltaTime * fps * speedScale;
            int n = current.body.Length;
            int i = forcedPhase >= 0f ? Mathf.Min(n - 1, (int)(Mathf.Repeat(forcedPhase, 1f) * n)) : (int)t % n;

            // Hanya arah side yang di-mirror untuk hadap kiri. Front/back punya sprite sendiri,
            // kalau ikut di-flip wajah & glint pindah sisi. faceLeft tetap diingat untuk saat kembali ke side.
            bool flip = faceLeft && dir == "side";
            bodyRenderer.sprite = current.body[i];
            bodyRenderer.flipX = flip;
            if (faceRenderer != null && current.face != null && current.face.Length > i)
            {
                faceRenderer.sprite = current.face[i];
                faceRenderer.flipX = flip;
            }
        }
    }
}
