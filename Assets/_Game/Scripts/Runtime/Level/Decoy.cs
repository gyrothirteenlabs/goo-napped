using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Umpan Kristal (kekuatan Lumi, desain-v2 bagian 8): kristal berbentuk slime yang berkilau.
    // Penjaga melihatnya seperti slime biasa (curiga, mengejar). Tertangkap = pecah, TIDAK menggagalkan level.
    public class Decoy : MonoBehaviour
    {
        public static readonly List<Decoy> All = new List<Decoy>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();

        private float life = 14f;
        private SpriteRenderer sr;

        public Vector2 Center => (Vector2)transform.position + Vector2.up * 0.2f;

        public static Decoy Spawn(Vector2 at, Sprite sprite)
        {
            var go = new GameObject("CrystalDecoy");
            go.transform.position = at;
            var d = go.AddComponent<Decoy>();
            d.sr = go.AddComponent<SpriteRenderer>();
            d.sr.sprite = sprite;
            d.sr.color = new Color(0.7f, 1f, 0.75f, 0.95f);
            go.transform.localScale = Vector3.one * 2.2f;
            var unlit = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var m in unlit) if (m.name == "Vfx_Particle") { d.sr.sharedMaterial = m; break; }
            AudioDirector.Play(AudioDirector.Sfx.Split, at, 0.7f, 0.1f);
            return d;
        }

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() => All.Remove(this);

        private void Update()
        {
            if ((life -= Time.deltaTime) <= 0f) { Pop(); return; }
            if (sr != null) sr.color = new Color(0.7f, 1f, 0.75f, 0.7f + 0.3f * Mathf.Sin(Time.time * 6f));
        }

        // Dipecahkan penjaga (atau habis waktu).
        public void Pop()
        {
            AudioDirector.Play(AudioDirector.Sfx.Merge, Center, 0.8f, 0.2f);
            Destroy(gameObject);
        }
    }
}
