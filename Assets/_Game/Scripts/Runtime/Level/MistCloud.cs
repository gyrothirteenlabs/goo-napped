using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Kabut (kekuatan Ibu, desain-v2 bagian 8): awan di depan lubang ventilasi yang menutup pandangan penjaga beberapa detik.
    // Tidak menghalangi gerak. Penjaga memakai Blocks() saat mengecek garis pandang.
    public class MistCloud : MonoBehaviour
    {
        public static readonly List<MistCloud> All = new List<MistCloud>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        [SerializeField] private float radius = 1.6f;
        private float life = 6f, total = 6f;
        private SpriteRenderer sr;

        public static MistCloud Spawn(Vector2 at, Sprite sprite)
        {
            var go = new GameObject("Mist");
            go.transform.position = at;
            var m = go.AddComponent<MistCloud>();
            m.sr = go.AddComponent<SpriteRenderer>();
            m.sr.sprite = sprite;
            m.sr.sortingOrder = 50;
            foreach (var mat in Resources.FindObjectsOfTypeAll<Material>()) if (mat.name == "Vfx_Particle") { m.sr.sharedMaterial = mat; break; }
            go.transform.localScale = Vector3.one * 9f;
            AudioDirector.Play(AudioDirector.Sfx.Disguise, at, 0.7f, 0.1f);
            return m;
        }

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() => All.Remove(this);

        private void Update()
        {
            if ((life -= Time.deltaTime) <= 0f) { Destroy(gameObject); return; }
            if (sr != null) sr.color = new Color(0.85f, 0.9f, 1f, 0.55f * Mathf.Clamp01(life / 1f) * Mathf.Clamp01((total - life) / 0.4f));
        }

        // Ruas a-b menembus kabut aktif mana pun?
        public static bool Blocks(Vector2 a, Vector2 b)
        {
            foreach (var m in All)
            {
                if (m == null) continue;
                Vector2 c = m.transform.position, ab = b - a;
                float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(c - a, ab) / ab.sqrMagnitude) : 0f;
                if (Vector2.Distance(a + ab * t, c) < m.radius) return true;
            }
            return false;
        }
    }
}
