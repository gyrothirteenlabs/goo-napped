using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GooNapped
{
    // HUD level (UI Toolkit). Layout: Assets/_Game/UI/Hud.uxml, gaya: Hud.uss (desainer bebas mengubah gaya).
    //   kiri atas : ciri tubuh potongan aktif + status (kena cahaya, menyala, berbau, lambat, nyamar)
    //   kanan atas: kartu resep tiap toples yang sudah diintip, dengan tanda cocok/belum per ciri
    //   bawah     : jumlah potongan, teman dibawa, gumpalan siap/cooldown, petunjuk tombol
    //   tengah    : pesan singkat, banner selesai/ketahuan
    [RequireComponent(typeof(UIDocument))]
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private PlayerBrain brain;
        [SerializeField] private LevelGoal goal;
        [SerializeField, Range(0.02f, 0.5f)] private float refreshInterval = 0.1f;
        [SerializeField, TextArea] private string controlsText =
            "WASD gerak · E makan/tukar/telan · Space belah/gabung · Shift pindah · C nyamar · Q muntah · F gumpalan · R ulang · Esc jeda";

        private VisualElement traits, tags, recipes;
        private Label pieces, carried, blob, toast, banner, controls;
        private Jar[] jars = new Jar[0];
        private float timer;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            traits = root.Q("traits");
            tags = root.Q("tags");
            recipes = root.Q("recipes");
            pieces = root.Q<Label>("pieces");
            carried = root.Q<Label>("carried");
            blob = root.Q<Label>("blob");
            toast = root.Q<Label>("toast");
            banner = root.Q<Label>("banner");
            controls = root.Q<Label>("controls");
            if (controls != null) controls.text = controlsText;
        }

        private void Start()
        {
            if (brain == null) brain = FindFirstObjectByType<PlayerBrain>();
            if (goal == null) goal = FindFirstObjectByType<LevelGoal>();
            jars = FindObjectsByType<Jar>(FindObjectsSortMode.None);
        }

        private void Update()
        {
            if ((timer -= Time.unscaledDeltaTime) > 0f) return;
            timer = refreshInterval;
            Refresh();
        }

        private void Refresh()
        {
            if (traits == null || brain == null) return;
            var a = brain.Active;
            traits.Clear();
            tags.Clear();
            if (a != null)
            {
                int n = 0;
                foreach (var t in a.Body.State.Traits) { traits.Add(Chip(t)); n++; }
                if (n == 0) traits.Add(TextChip("netral"));
                var st = a.Body.State;
                if (!a.IsDisguised && LightExposure.At(a.Center) >= 0.35f) tags.Add(Tag("KENA CAHAYA", "tag--light"));
                if (st.EmitsLight) tags.Add(Tag("MENYALA", "tag--glow"));
                if (st.SmellRadius > 0f) tags.Add(Tag("BERBAU", "tag--smell"));
                if (st.SpeedMultiplier < 1f) tags.Add(Tag("LAMBAT", "tag--slow"));
                if (st.StepNoiseRadius > 0f) tags.Add(Tag("BERISIK", "tag--slow"));
                if (a.IsDisguised) tags.Add(Tag("NYAMAR", "tag--hidden"));
            }

            recipes.Clear();
            foreach (var j in jars)
                if (j != null && j.IsPeeked && j.Recipe != null) recipes.Add(RecipeCard(j, a));

            int req = goal != null ? goal.Required : 0;
            pieces.text = "Potongan " + brain.Pieces.Count;
            carried.text = "Teman " + (a != null ? a.Carried.Count : 0) + "/" + req;
            blob.text = brain.ThrowCooldownLeft > 0f ? "Gumpalan " + Mathf.CeilToInt(brain.ThrowCooldownLeft) + " dtk" : "Gumpalan siap";

            bool hasMsg = !string.IsNullOrEmpty(brain.Message);
            toast.text = brain.Message;
            toast.EnableInClassList("toast--hidden", !hasMsg);

            bool win = goal != null && goal.IsComplete, fail = goal != null && goal.IsFailed;
            banner.EnableInClassList("banner--hidden", !win && !fail);
            banner.EnableInClassList("banner--win", win);
            banner.EnableInClassList("banner--fail", fail);
            banner.text = win ? "TEMAN SELAMAT!" : fail ? goal.FailReason.ToUpperInvariant() : "";
        }

        private VisualElement RecipeCard(Jar jar, SlimePiece a)
        {
            var card = new VisualElement();
            card.AddToClassList("panel");
            card.AddToClassList("recipe-card");
            var title = new Label("Toples: " + jar.Recipe.FriendName);
            title.AddToClassList("recipe-title");
            card.Add(title);
            if (!jar.HasFriend) card.AddToClassList("recipe-card--done");
            foreach (var t in jar.Recipe.Traits)
            {
                // sudah ditukar: bandingkan dengan tiruan di toples; belum: dengan tubuh potongan aktif
                var body = jar.HasFriend ? (a != null ? a.Body.State : null) : jar.DecoyState;
                bool ok = body != null && body.Get(t.kind) == Trait.Normalize(t.value);
                var row = new VisualElement();
                row.AddToClassList("recipe-row");
                var dot = new VisualElement();
                dot.AddToClassList("recipe-dot");
                dot.AddToClassList(ok ? "recipe-dot--ok" : "recipe-dot--miss");
                var lbl = new Label(KindName(t.kind) + ": " + TraitText(t));
                lbl.AddToClassList("recipe-text");
                lbl.AddToClassList(ok ? "recipe-text--ok" : "recipe-text--miss");
                row.Add(dot);
                row.Add(lbl);
                card.Add(row);
            }
            if (!jar.HasFriend)
            {
                var note = new Label(jar.DecoyMismatch == 0 ? "Tiruan sempurna" : "Tiruan: " + jar.DecoyMismatch + " ciri salah (penjaga cepat curiga)");
                note.AddToClassList("recipe-note");
                card.Add(note);
            }
            return card;
        }

        private static VisualElement Chip(Trait t)
        {
            var chip = new VisualElement();
            chip.AddToClassList("chip");
            if (t.kind == TraitKind.Color)
            {
                var sw = new VisualElement();
                sw.AddToClassList("chip-swatch");
                sw.style.backgroundColor = ColorOf(t.value);
                chip.Add(sw);
            }
            var l = new Label(TraitText(t));
            l.AddToClassList("chip-label");
            chip.Add(l);
            return chip;
        }

        // Teks ringkas ciri: nilai saja ("kuning", "panas", "berat"), kecuali bau ("bau belerang").
        public static string TraitText(Trait t) =>
            t.kind == TraitKind.Smell ? "bau " + t.value : t.value;

        private static VisualElement TextChip(string text)
        {
            var chip = new VisualElement();
            chip.AddToClassList("chip");
            var l = new Label(text);
            l.AddToClassList("chip-label");
            chip.Add(l);
            return chip;
        }

        private static Label Tag(string text, string cls)
        {
            var l = new Label(text);
            l.AddToClassList("tag");
            l.AddToClassList(cls);
            return l;
        }

        public static string KindName(TraitKind k)
        {
            switch (k)
            {
                case TraitKind.Color: return "Warna";
                case TraitKind.Temperature: return "Suhu";
                case TraitKind.Weight: return "Berat";
                case TraitKind.Shine: return "Kilau";
                case TraitKind.Smell: return "Bau";
            }
            return k.ToString();
        }

        private static readonly Dictionary<string, Color> colors = new Dictionary<string, Color>
        {
            { "merah", new Color(0.9f, 0.25f, 0.25f) }, { "abu", new Color(0.6f, 0.62f, 0.68f) },
            { "hijau", new Color(0.4f, 0.85f, 0.45f) }, { "biru", new Color(0.4f, 0.6f, 1f) },
            { "kuning", new Color(0.95f, 0.85f, 0.3f) }, { "putih", new Color(0.95f, 0.95f, 0.95f) },
            { "ungu", new Color(0.6f, 0.4f, 0.85f) }, { "hitam", new Color(0.15f, 0.15f, 0.2f) },
        };

        public static Color ColorOf(string value) =>
            colors.TryGetValue(Trait.Normalize(value), out var c) ? c : Color.gray;
    }
}
