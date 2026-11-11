using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GooNapped
{
    // HUD level (UI Toolkit). Layout: Assets/_Game/UI/Hud.uxml, gaya: Hud.uss (desainer bebas mengubah gaya).
    // Semua teks bahasa Inggris (desain-v2 jawaban 66).
    //   kiri atas : ukuran (massa) + aura + ciri lain potongan aktif, status (lit, glowing, smelly, slow, noisy, hidden)
    //   kanan atas: kartu tiap toples yang sudah didekati: nama teman, aura, ukuran, apakah tiruanmu pas; alarm
    //   bawah     : potongan, teman selamat, status teman, petunjuk tombol
    //   tengah    : pesan singkat, banner selesai/tertangkap
    [RequireComponent(typeof(UIDocument))]
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private PlayerBrain brain;
        [SerializeField] private LevelGoal goal;
        [SerializeField, Range(0.02f, 0.5f)] private float refreshInterval = 0.1f;
        [SerializeField, TextArea] private string controlsText =
            "WASD move · E eat/use · Space split/merge · Shift switch · C hide · F call friend · Q spit · R retry · Esc pause";

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
                var st = a.Body.State;
                traits.Add(TextChip("Size " + st.Mass + "/" + SlimeBodyState.MaxMass));
                traits.Add(AuraChip(st.Aura));
                foreach (var t in st.Traits) if (t.kind != TraitKind.Color) traits.Add(TextChip(TraitNames.Of(t)));
                if (!a.IsHidden && LightExposure.At(a.Center) >= 0.35f) tags.Add(Tag("LIT", "tag--light"));
                if (st.EmitsLight) tags.Add(Tag("GLOWING", "tag--glow"));
                if (st.SmellRadius > 0f) tags.Add(Tag("SMELLY", "tag--smell"));
                if (st.SpeedMultiplier < 0.85f) tags.Add(Tag("SLOW", "tag--slow"));
                if (st.StepNoiseRadius > 0f) tags.Add(Tag("NOISY", "tag--slow"));
                if (a.IsHidden) tags.Add(Tag("HIDDEN", "tag--hidden"));
            }

            recipes.Clear();
            foreach (var j in jars)
                if (j != null && j.IsPeeked && j.Recipe != null) recipes.Add(JarCard(j, a));

            int req = goal != null ? goal.Required : 0;
            pieces.text = "Pieces " + brain.Pieces.Count + "/" + brain.MaxPieces;
            carried.text = req > 0 ? "Friends safe " + (goal != null ? goal.Saved : 0) + "/" + req : "Reach the vent";
            blob.text = FriendStatus();

            bool hasMsg = !string.IsNullOrEmpty(brain.Message);
            toast.text = brain.Message;
            toast.EnableInClassList("toast--hidden", !hasMsg);

            bool win = goal != null && goal.IsComplete, fail = goal != null && goal.IsFailed;
            banner.EnableInClassList("banner--hidden", !win && !fail);
            banner.EnableInClassList("banner--win", win);
            banner.EnableInClassList("banner--fail", fail);
            banner.text = win ? (req > 0 ? "FRIENDS SAFE!" : "MADE IT!") : fail ? goal.FailReason.ToUpperInvariant() : "";
        }

        // Teman yang sedang di luar toples: mengikuti / menunggu / sembunyi.
        private static string FriendStatus()
        {
            var parts = new List<string>();
            foreach (var f in FriendSlime.All)
            {
                if (f == null || f.State == FriendSlime.Mode.Saved) continue;
                string s = f.State == FriendSlime.Mode.Following ? "following" : f.State == FriendSlime.Mode.Hidden ? "hiding" : "waiting";
                parts.Add(f.Name + ": " + s);
            }
            return parts.Count > 0 ? string.Join(" · ", parts) : "";
        }

        private VisualElement JarCard(Jar jar, SlimePiece a)
        {
            var r = jar.Recipe;
            var card = new VisualElement();
            card.AddToClassList("panel");
            card.AddToClassList("recipe-card");
            var title = new Label(r.FriendName + (r.Size > 1 ? " (big)" : ""));
            title.AddToClassList("recipe-title");
            card.Add(title);
            if (!jar.HasFriend) card.AddToClassList("recipe-card--done");

            if (jar.HasFriend)
            {
                // pratinjau tiruan dari tubuh potongan aktif: potongan baru mengambil `Size` unit terakhir
                SlimeBodyState copy = null;
                if (a != null) copy = a.Body.State.Clone().Split(jar.Size);
                bool sizeOk = copy != null;
                bool auraOk = copy != null && jar.MismatchOf(copy) == 0;
                card.Add(Row(sizeOk, "Size " + jar.Size + (sizeOk ? "" : " (you need size " + (jar.Size + 1) + ")")));
                foreach (var t in r.Traits)
                {
                    bool ok = copy != null ? copy.Get(t.kind) == Trait.Normalize(t.value)
                                           : a != null && a.Body.State.Get(t.kind) == Trait.Normalize(t.value);
                    card.Add(Row(ok, TraitNames.Kind(t.kind) + ": " + (t.kind == TraitKind.Color ? TraitNames.Aura(t.value) : TraitNames.Of(t))));
                }
                if (sizeOk && auraOk) card.Add(Note("Split here (Space)"));
            }
            else
            {
                string note = jar.DecoyMismatch == 0 ? "Copy holds" : "Wrong aura: guards get suspicious faster";
                if (jar.AlarmRaised) note = "ALARM! The machine noticed";
                else if (jar.AlarmTimeLeft > 0f) note += " · alarm in " + Mathf.CeilToInt(jar.AlarmTimeLeft) + "s";
                card.Add(Note(note));
            }
            return card;
        }

        private static VisualElement Row(bool ok, string text)
        {
            var row = new VisualElement();
            row.AddToClassList("recipe-row");
            var dot = new VisualElement();
            dot.AddToClassList("recipe-dot");
            dot.AddToClassList(ok ? "recipe-dot--ok" : "recipe-dot--miss");
            var lbl = new Label(text);
            lbl.AddToClassList("recipe-text");
            lbl.AddToClassList(ok ? "recipe-text--ok" : "recipe-text--miss");
            row.Add(dot);
            row.Add(lbl);
            return row;
        }

        private static Label Note(string text)
        {
            var note = new Label(text);
            note.AddToClassList("recipe-note");
            return note;
        }

        private static VisualElement AuraChip(string aura)
        {
            var chip = new VisualElement();
            chip.AddToClassList("chip");
            var sw = new VisualElement();
            sw.AddToClassList("chip-swatch");
            sw.style.backgroundColor = TraitNames.AuraColor(aura);
            chip.Add(sw);
            var l = new Label(TraitNames.Aura(aura) + " aura");
            l.AddToClassList("chip-label");
            chip.Add(l);
            return chip;
        }

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
    }
}
