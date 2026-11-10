using System.Collections;
using UnityEngine;

namespace GooNapped
{
    // Gumpalan lendir kecil yang dilempar slime: melambung, mendarat dengan bunyi "plok",
    // penjaga dalam radius suara mendatangi tempat itu (pengalih). Lalu meleleh hilang.
    public class GooBlob : MonoBehaviour
    {
        public static GooBlob Spawn(Sprite sprite, Color color, Vector2 from, Vector2 to, float noiseRadius)
        {
            var go = new GameObject("GooBlob");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 5;
            go.transform.localScale = Vector3.one * 0.6f;
            var b = go.AddComponent<GooBlob>();
            b.StartCoroutine(b.Fly(sr, from, to, noiseRadius));
            return b;
        }

        private IEnumerator Fly(SpriteRenderer sr, Vector2 from, Vector2 to, float noiseRadius)
        {
            float dist = Vector2.Distance(from, to);
            float duration = Mathf.Clamp(dist / 7f, 0.25f, 0.6f);
            float height = Mathf.Clamp(dist * 0.25f, 0.3f, 0.9f);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                var p = Vector2.Lerp(from, to, k) + Vector2.up * (height * 4f * k * (1f - k));
                transform.position = p;
                yield return null;
            }
            transform.position = to;
            transform.localScale = new Vector3(0.9f, 0.35f, 1f);   // gepeng saat jatuh
            NoiseBus.Emit(to, noiseRadius);
            AudioDirector.Play(AudioDirector.Sfx.Splat, to);
            var c = sr.color;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                sr.color = new Color(c.r, c.g, c.b, c.a * (1f - t / 1.5f));
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
