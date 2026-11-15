using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GooNapped
{
    // Prompt tutorial yang melayang di atas slime aktif (desain Figma "HUD & Dungeon" > Tutorial, gaya B):
    // keycap pixel + teks ber-outline + bayangan lembut + panah kecil ke bawah. Gaya di UI/Hud.uss (kelas tp-*).
    // Animasi: muncul "pop" (membesar lewat lalu mantul), teks diketik sangat cepat, keycap muncul bergiliran,
    // lalu mengambang + shake halus lambat (sama dengan hover menu). Selesai = tombol menyala lalu prompt melayang hilang.
    // Dipakai lewat TutorialTrigger. Satu prompt sekaligus; ditempel ke dokumen HUD (GameHud) supaya ikut font & skala.
    public class TutorialPrompt : MonoBehaviour
    {
        private static TutorialPrompt instance;

        // Ambil/buat prompt di HUD level. Null kalau scene tidak punya GameHud.
        public static TutorialPrompt Get()
        {
            if (instance != null) return instance;
            var hud = FindFirstObjectByType<GameHud>();
            if (hud == null) return null;
            instance = hud.gameObject.AddComponent<TutorialPrompt>();
            return instance;
        }

        // ---------- data tombol ----------
        private class KeyView
        {
            public VisualElement el;
            public Key[] keys;
            public float appearDelay;
        }

        private enum State { Hidden, Showing, Leaving }

        private const float TypeSpeed = 0.012f;   // detik per huruf (sangat cepat)
        private const string Scramble = "#%&@$?!/<>=+*";

        private VisualElement layer, prompt, keysBox, arrow;
        private Label textFg;
        private readonly List<KeyView> keyViews = new List<KeyView>();
        private State state = State.Hidden;
        private object owner;
        private string fullText = "";
        private float shownAt, leaveAt;
        private bool completed;
        private PlayerBrain brain;

        private void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null) return;
            var root = doc.rootVisualElement.Q("root") ?? doc.rootVisualElement;
            layer = new VisualElement { pickingMode = PickingMode.Ignore };
            layer.AddToClassList("tp-layer");
            root.Add(layer);
        }

        private void OnDisable()
        {
            layer?.RemoveFromHierarchy();
            if (instance == this) instance = null;
        }

        // ---------- API ----------

        // keys: "WASD" (susunan keyboard), atau beberapa tombol dipisah spasi/koma: "E", "SPACE", "SHIFT", "C".
        public void Show(object who, string keys, string text)
        {
            if (layer == null) return;
            if (state == State.Showing && owner == who && text == fullText) return;
            owner = who;
            fullText = (text ?? "").ToUpperInvariant();
            completed = false;
            Build(keys);
            state = State.Showing;
            shownAt = Time.unscaledTime;
        }

        // Aksi berhasil: tombol menyala, prompt naik sedikit lalu memudar.
        public void Complete(object who)
        {
            if (owner != who || state != State.Showing) return;
            completed = true;
            foreach (var k in keyViews) k.el.AddToClassList("tp-key--down");
            state = State.Leaving;
            leaveAt = Time.unscaledTime + 0.35f;   // tahan sebentar supaya "berhasil"-nya terlihat
        }

        public void Hide(object who)
        {
            if (owner != who || state != State.Showing) return;
            state = State.Leaving;
            leaveAt = Time.unscaledTime;
        }

        public bool IsShowingFor(object who) => owner == who && state == State.Showing;

        // Ada prompt yang masih terlihat (tampil atau sedang menghilang). Trigger lain menunggu supaya tidak saling timpa.
        public bool IsBusy => state != State.Hidden;

        // Apakah salah satu tombol prompt sedang ditekan.
        public static bool AnyHeld(string keys)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            foreach (var group in Parse(keys))
                foreach (var k in group)
                    if (kb[k].isPressed) return true;
            return false;
        }

        // ---------- bangun tampilan ----------

        private void Build(string keys)
        {
            prompt?.RemoveFromHierarchy();
            keyViews.Clear();

            prompt = Box(layer, "tp-prompt");
            Box(prompt, "tp-shadow");
            var row = Box(prompt, "tp-row");
            keysBox = Box(row, "tp-keys");

            var groups = Parse(keys);
            bool wasd = keys != null && keys.Trim().ToUpperInvariant() == "WASD";
            if (wasd)
            {
                var pad = Box(keysBox, "tp-wasd");
                AddKey(pad, "W", new[] { Key.W, Key.UpArrow }, 24, 0, false, 0f);
                AddKey(pad, "A", new[] { Key.A, Key.LeftArrow }, 0, 26, false, 0.03f);
                AddKey(pad, "S", new[] { Key.S, Key.DownArrow }, 24, 26, false, 0.06f);
                AddKey(pad, "D", new[] { Key.D, Key.RightArrow }, 48, 26, false, 0.09f);
            }
            else
            {
                string[] names = Names(keys);
                for (int i = 0; i < groups.Count; i++)
                    AddKey(keysBox, names[i], groups[i], 0, 0, true, i * 0.04f);
            }

            // teks: bayangan padat + depan, diisi per huruf
            var textBox = Box(row, "tp-textbox");
            textFg = new Label("") { pickingMode = PickingMode.Ignore };
            textFg.AddToClassList("tp-text");
            textBox.Add(textFg);

            arrow = Box(prompt, "tp-arrow");
            Box(arrow, "tp-arrow-a"); Box(arrow, "tp-arrow-b"); Box(arrow, "tp-arrow-c");

            prompt.style.opacity = 0;
        }

        private void AddKey(VisualElement parent, string label, Key[] keys, float x, float y, bool inline, float delay)
        {
            var el = Box(parent, "tp-key");
            bool longKey = label.Length > 1;
            if (inline) el.AddToClassList("tp-key--inline");
            else { el.style.left = x; el.style.top = y; }
            if (longKey) { el.AddToClassList("tp-key--long"); el.style.width = 18 + label.Length * 7; }
            Box(el, "tp-key-edge");
            Box(el, "tp-key-side");
            Box(el, "tp-key-top");
            Box(el, "tp-key-face");
            Box(el, "tp-key-shine");
            var l = new Label(label) { pickingMode = PickingMode.Ignore };
            l.AddToClassList("tp-key-label");
            el.Add(l);
            el.style.scale = new Scale(Vector2.zero);
            keyViews.Add(new KeyView { el = el, keys = keys, appearDelay = delay });
        }

        private static VisualElement Box(VisualElement parent, string cls)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList(cls);
            parent.Add(e);
            return e;
        }

        private static string[] Names(string keys)
        {
            var parts = (keys ?? "").ToUpperInvariant().Split(new[] { ' ', ',', '+' }, StringSplitOptions.RemoveEmptyEntries);
            return parts;
        }

        private static List<Key[]> Parse(string keys)
        {
            var list = new List<Key[]>();
            if (string.IsNullOrWhiteSpace(keys)) return list;
            string k = keys.Trim().ToUpperInvariant();
            if (k == "WASD")
            {
                list.Add(new[] { Key.W, Key.UpArrow }); list.Add(new[] { Key.A, Key.LeftArrow });
                list.Add(new[] { Key.S, Key.DownArrow }); list.Add(new[] { Key.D, Key.RightArrow });
                return list;
            }
            foreach (var name in Names(k))
            {
                switch (name)
                {
                    case "SPACE": list.Add(new[] { Key.Space }); break;
                    case "SHIFT": list.Add(new[] { Key.LeftShift, Key.RightShift }); break;
                    case "ESC": list.Add(new[] { Key.Escape }); break;
                    case "ENTER": list.Add(new[] { Key.Enter, Key.NumpadEnter }); break;
                    default:
                        list.Add(Enum.TryParse(name, true, out Key parsed) && parsed != Key.None ? new[] { parsed } : new Key[0]);
                        break;
                }
            }
            return list;
        }

        // ---------- animasi + ikut slime ----------

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);

        private void LateUpdate()
        {
            if (prompt == null || state == State.Hidden) return;
            float now = Time.unscaledTime;
            float t = now - shownAt;

            // posisi: di atas kepala slime aktif
            if (brain == null) brain = FindFirstObjectByType<PlayerBrain>();
            var piece = brain != null ? brain.Active : null;
            if (piece != null && layer.panel != null && Camera.main != null)
            {
                Vector3 world = (Vector3)piece.Center + Vector3.up * (piece.Radius + 0.3f);
                Vector2 p = RuntimePanelUtils.CameraTransformWorldToPanel(layer.panel, world, Camera.main);
                float w = prompt.resolvedStyle.width, h = prompt.resolvedStyle.height;
                if (float.IsNaN(w)) w = 0;
                if (float.IsNaN(h)) h = 0;
                prompt.style.left = Mathf.Round(p.x - w * 0.5f);
                prompt.style.top = Mathf.Round(p.y - h);
            }

            // pop masuk: 0.4 -> 1.15 -> 1 dalam 0.2 dtk
            float s;
            if (t < 0.12f) s = Mathf.Lerp(0.4f, 1.15f, EaseOut(t / 0.12f));
            else if (t < 0.22f) s = Mathf.Lerp(1.15f, 1f, (t - 0.12f) / 0.1f);
            else s = 1f;
            float alpha = Mathf.Clamp01(t / 0.06f);

            // keycap muncul bergiliran (pop kecil masing-masing) + menyala saat ditekan
            var kb = Keyboard.current;
            foreach (var k in keyViews)
            {
                float kt = t - 0.04f - k.appearDelay;
                float ks = kt <= 0 ? 0f : kt < 0.1f ? Mathf.Lerp(0f, 1.25f, EaseOut(kt / 0.1f)) : kt < 0.18f ? Mathf.Lerp(1.25f, 1f, (kt - 0.1f) / 0.08f) : 1f;
                k.el.style.scale = new Scale(new Vector2(ks, ks));
                if (!completed && kb != null)
                {
                    bool down = false;
                    foreach (var key in k.keys) if (kb[key].isPressed) { down = true; break; }
                    k.el.EnableInClassList("tp-key--down", down);
                }
            }

            // teks diketik sangat cepat, huruf berikutnya sempat teracak sekilas
            float tt = t - 0.06f;
            int n = Mathf.Clamp(Mathf.FloorToInt(tt / TypeSpeed), 0, fullText.Length);
            string shown = fullText.Substring(0, n);
            if (n < fullText.Length && n > 0 && fullText[n] != ' ') shown += Scramble[UnityEngine.Random.Range(0, Scramble.Length)];
            if (textFg.text != shown) textFg.text = shown;

            // mengambang + shake halus lambat (sama seperti hover menu)
            float floatY = Mathf.Sin(now * 2.4f) * 2.5f;
            float floatX = Mathf.Sin(now * 1.3f) * 1f;
            float shakeX = (Mathf.PerlinNoise(now * 3f, 0.3f) - 0.5f) * 1.4f;
            float shakeY = (Mathf.PerlinNoise(0.7f, now * 3f) - 0.5f) * 1.4f;
            float rot = (Mathf.PerlinNoise(now * 2.5f, 5.1f) - 0.5f) * 1.2f;
            float idle = Mathf.Clamp01((t - 0.2f) / 0.3f);   // masuk pelan setelah pop selesai
            float ox = (floatX + shakeX) * idle, oy = (floatY + shakeY) * idle;

            // keluar: naik + membesar sedikit + memudar
            if (state == State.Leaving && now >= leaveAt)
            {
                float lt = Mathf.Clamp01((now - leaveAt) / 0.22f);
                s *= 1f + lt * 0.15f;
                oy -= lt * 14f;
                alpha *= 1f - lt;
                if (lt >= 1f)
                {
                    state = State.Hidden;
                    prompt.RemoveFromHierarchy();
                    prompt = null;
                    owner = null;
                    return;
                }
            }

            prompt.style.scale = new Scale(new Vector2(s, s));
            prompt.style.translate = new Translate(Mathf.Round(ox), Mathf.Round(oy));
            prompt.style.rotate = new Rotate(rot * idle);
            prompt.style.opacity = alpha;
            if (arrow != null) arrow.style.translate = new Translate(0, Mathf.Round(Mathf.Abs(Mathf.Sin(now * 4f)) * 3f));
        }
    }
}
