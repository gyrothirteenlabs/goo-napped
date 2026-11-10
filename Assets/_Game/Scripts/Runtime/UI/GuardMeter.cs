using UnityEngine;

namespace GooNapped
{
    // Indikator di atas kepala penjaga: bar curiga + ikon "?" (memeriksa) / "!" (melihatmu).
    // Sprite ikon dibuat dari kode (PLACEHOLDER pixel 8x8); desainer bisa mengisi sprite sendiri di Inspector.
    [RequireComponent(typeof(Guard))]
    public class GuardMeter : MonoBehaviour
    {
        [Tooltip("Material unlit supaya tetap terbaca di tempat gelap (Enemy_Glow_Unlit).")]
        [SerializeField] private Material material;
        [SerializeField] private Sprite questionSprite;
        [SerializeField] private Sprite exclaimSprite;
        [SerializeField] private float barWidth = 0.6f;
        [SerializeField] private Color calmColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color alertColor = new Color(1f, 0.25f, 0.2f);

        private Guard guard;
        private Transform root, fill;
        private SpriteRenderer bg, fillSr, icon;

        private static Sprite white, question, exclaim;

        private void Awake()
        {
            guard = GetComponent<Guard>();
            if (white == null) white = MakeSprite(new[] { "1" }, 1);
            if (question == null) question = MakeSprite(new[]
            {
                "011110", "110011", "000011", "000110", "001100", "001100", "000000", "001100",
            }, 6);
            if (exclaim == null) exclaim = MakeSprite(new[]
            {
                "0110", "0110", "0110", "0110", "0110", "0000", "0110", "0110",
            }, 4);

            root = new GameObject("GuardMeter").transform;   // terpisah dari SortingGroup penjaga, selalu di atas
            bg = Make("Bg", white, new Color(0f, 0f, 0f, 0.7f), 200);
            fillSr = Make("Fill", white, calmColor, 201);
            icon = Make("Icon", null, Color.white, 202);
            fill = fillSr.transform;
            bg.transform.localScale = new Vector3(barWidth + 0.04f, 0.09f, 1f);
        }

        private SpriteRenderer Make(string name, Sprite s, Color c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.color = c;
            sr.sortingOrder = order;
            if (material != null) sr.sharedMaterial = material;
            return sr;
        }

        private void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        private void LateUpdate()
        {
            root.position = transform.position + Vector3.up * guard.HeadHeight;
            float s = guard.Suspicion;
            bool showBar = s > 0.01f;
            bg.enabled = fillSr.enabled = showBar;
            if (showBar)
            {
                fill.localScale = new Vector3(barWidth * s, 0.06f, 1f);
                fill.localPosition = new Vector3(-barWidth * (1f - s) * 0.5f, 0f, 0f);
                fillSr.color = Color.Lerp(calmColor, alertColor, s);
            }
            Sprite want = guard.IsAlerted ? (exclaimSprite != null ? exclaimSprite : exclaim)
                        : guard.IsSearching ? (questionSprite != null ? questionSprite : question) : null;
            icon.sprite = want;
            icon.enabled = want != null;
            icon.color = guard.IsAlerted ? alertColor : calmColor;
            icon.transform.localPosition = new Vector3(0f, 0.22f + Mathf.Sin(Time.time * 6f) * 0.02f, 0f);
        }

        // Sprite pixel dari pola teks ("1" = isi). 1 unit = 48 px, jadi 8 baris = 8/48 unit.
        private static Sprite MakeSprite(string[] rows, int w)
        {
            int h = rows.Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, h - 1 - y, rows[y].Length > x && rows[y][x] == '1' ? Color.white : Color.clear);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w == 1 ? 1f : 48f);
        }
    }
}
