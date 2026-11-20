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
    //   tengah    : pesan singkat, banner selesai/tertangkap, kartu esens (saat dekat esens: aura, cocok toples, efek)
    [RequireComponent(typeof(UIDocument))]
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private PlayerBrain brain;
        [SerializeField] private LevelGoal goal;
        private FinalSequence final;   // L11: objektif = katup aura lalu segel Alkemis (bukan ventilasi)
        [SerializeField, Range(0.02f, 0.5f)] private float refreshInterval = 0.1f;
        [SerializeField, TextArea] private string controlsText =
            "WASD move · E eat/use · Space split/merge · Shift switch · C hide · F call friend · Q spit · R retry · Esc pause";

        private VisualElement recipes, card, slime, bodyImg, faceImg, glow, auraDot, sizePips, piecePips, friendBox, effects;
        private VisualElement essenceCard, ecDot, ecEffects;
        private Label massLabel, auraLabel, friendCount, goalLabel, blob, toast, banner, controls, ecName, ecKey, ecAura;
        private IngredientData shownEssence;
        private string shownEssenceKey;
        private Jar[] jars = new Jar[0];
        private float timer;
        private Texture2D shownBody, shownFace;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            recipes = root.Q("recipes");
            card = root.Q("slime-card");
            slime = root.Q("sc-slime");
            bodyImg = root.Q("sc-body");
            faceImg = root.Q("sc-face");
            glow = root.Q("sc-glow");
            auraDot = root.Q("sc-aura-dot");
            sizePips = root.Q("sc-size");
            piecePips = root.Q("sc-pieces");
            friendBox = root.Q("sc-friend");
            effects = root.Q("sc-effects");
            massLabel = root.Q<Label>("sc-mass");
            auraLabel = root.Q<Label>("sc-aura");
            friendCount = root.Q<Label>("sc-friend-count");
            goalLabel = root.Q<Label>("sc-goal");
            blob = root.Q<Label>("blob");
            toast = root.Q<Label>("toast");
            banner = root.Q<Label>("banner");
            controls = root.Q<Label>("controls");
            essenceCard = root.Q("essence-card");
            ecDot = root.Q("ec-dot");
            ecEffects = root.Q("ec-effects");
            ecName = root.Q<Label>("ec-name");
            ecKey = root.Q<Label>("ec-key");
            ecAura = root.Q<Label>("ec-aura");
            if (controls != null) controls.text = controlsText;
            // datang dari menu / retry: buka layar dengan transisi yang sama (lendir surut / lembar miring)
            root.schedule.Execute(() => UiFx.RevealIfPending(root)).StartingIn(30);
        }

        private void Start()
        {
            if (brain == null) brain = FindFirstObjectByType<PlayerBrain>();
            if (goal == null) goal = FindFirstObjectByType<LevelGoal>();
            if (final == null) final = FindFirstObjectByType<FinalSequence>();
            jars = FindObjectsByType<Jar>(FindObjectsSortMode.None);
        }

        private bool hiddenForIntro;

        private void Update()
        {
            // pembuka level sinematik: HUD disembunyikan (transisi lendir UiFx di root tetap tampil)
            if (LevelIntro.Running != hiddenForIntro)
            {
                hiddenForIntro = LevelIntro.Running;
                foreach (var e in new VisualElement[] { card, recipes, controls, essenceCard })
                    if (e != null) e.style.visibility = hiddenForIntro ? Visibility.Hidden : Visibility.Visible;
            }
            if ((timer -= Time.unscaledDeltaTime) > 0f) return;
            timer = refreshInterval;
            Refresh();
        }

        private void Refresh()
        {
            if (card == null || brain == null) return;
            var a = brain.Active;
            if (a != null) RefreshSlimeCard(a);

            recipes.Clear();
            foreach (var j in jars)
                if (j != null && j.IsPeeked && j.Recipe != null) recipes.Add(JarCard(j, a));

            int req = goal != null ? goal.Required : 0;
            int saved = goal != null ? goal.Saved : 0;
            // potongan: belah ketupat per potongan, yang dikendalikan berwarna kuningan
            piecePips.Clear();
            for (int i = 0; i < brain.MaxPieces; i++)
            {
                var p = Pip("sc-pip--piece");
                if (i < brain.Pieces.Count) p.AddToClassList(brain.Pieces[i] == a ? "sc-pip--active" : "sc-pip--on");
                piecePips.Add(p);
            }
            // teman yang sedang ikut (dipanggil F)
            int following = 0;
            foreach (var f in FriendSlime.All) if (f != null && f.State == FriendSlime.Mode.Following) following++;
            friendCount.text = following.ToString();
            friendBox.EnableInClassList("sc-friend--active", following > 0);
            if (final != null)
            {
                goalLabel.text = final.JarOpen ? "LURE THE GLOAM INTO THE JAR" : "AURA VALVES  " + final.Reversed + " / " + final.Total;
                goalLabel.EnableInClassList("sc-goal--done", final.JarOpen);
            }
            else
            {
                goalLabel.text = req > 0 ? "FRIENDS SAFE  " + saved + " / " + req : "REACH THE VENT";
                goalLabel.EnableInClassList("sc-goal--done", req > 0 && saved >= req);
            }
            blob.text = FriendStatus();
            if (a != null && a.HidingIn != null && Vent.Of(a.HidingIn) != null)
            {
                var sel = FriendPowers.Selected;
                blob.text = sel == FriendPower.None ? "In the vent: A/D other openings · C out"
                    : "Power: " + FriendPowers.NameOf(sel) + " x" + FriendPowers.Charges(sel) + " · E use · Shift change · A/D openings";
            }

            RefreshEssenceCard(a);

            bool hasMsg = !string.IsNullOrEmpty(brain.Message);
            toast.text = brain.Message;
            toast.EnableInClassList("toast--hidden", !hasMsg);

            bool win = goal != null && goal.IsComplete, fail = goal != null && goal.IsFailed;
            banner.EnableInClassList("banner--hidden", !win && !fail);
            banner.EnableInClassList("banner--win", win);
            banner.EnableInClassList("banner--fail", fail);
            banner.text = win ? (req > 0 ? "FRIENDS SAFE!" : "MADE IT!") : fail ? goal.FailReason.ToUpperInvariant() : "";
        }

        // Kartu slime: potret (ikut warna aura & besar sesuai massa), nama aura, pip ukuran, efek tubuh.
        private void RefreshSlimeCard(SlimePiece a)
        {
            var st = a.Body.State;
            var auraColor = TraitNames.AuraColor(st.Aura);

            // potret dari sprite slime sendiri, diwarnai seperti material aura sekarang
            var anim = a.GetComponentInChildren<SlimeSpriteAnimator>();
            var frames = SlimePortrait.IdleFront(a);
            var mat = anim != null && anim.bodyRenderer != null ? anim.bodyRenderer.sharedMaterial : null;
            var bodyTex = SlimePortrait.Body(frames.body, mat);
            var faceTex = SlimePortrait.Face(frames.face);
            if (bodyTex != shownBody) { shownBody = bodyTex; bodyImg.style.backgroundImage = bodyTex != null ? new StyleBackground(bodyTex) : new StyleBackground(StyleKeyword.None); }
            if (faceTex != shownFace) { shownFace = faceTex; faceImg.style.backgroundImage = faceTex != null ? new StyleBackground(faceTex) : new StyleBackground(StyleKeyword.None); }
            float k = SlimeBodyState.MaxMass > 1 ? (st.Mass - 1) / (float)(SlimeBodyState.MaxMass - 1) : 1f;
            float s = Mathf.Lerp(0.75f, 1.2f, Mathf.Clamp01(k));   // ukuran 1 = kecil, ukuran maks = memenuhi bingkai
            slime.style.scale = new Scale(new Vector2(s, s));
            glow.style.backgroundColor = auraColor;
            massLabel.text = st.Mass + "/" + SlimeBodyState.MaxMass;

            auraDot.style.backgroundColor = auraColor;
            auraLabel.text = TraitNames.Aura(st.Aura).ToUpperInvariant() + " AURA";

            sizePips.Clear();
            for (int i = 0; i < SlimeBodyState.MaxMass; i++)
            {
                var p = Pip(null);
                if (i < st.Mass) p.AddToClassList("sc-pip--on");
                sizePips.Add(p);
            }

            // efek: titik kecil warna slime + nama efek
            var names = new List<string>();
            void AddFx(string n) { n = n.ToUpperInvariant(); if (!names.Contains(n)) names.Add(n); }
            if (!a.IsHidden && LightExposure.At(a.Center) >= 0.35f) AddFx("lit");
            if (st.EmitsLight) AddFx("glowing");
            if (st.SmellRadius > 0f) AddFx("smelly");
            if (a.IsMuddy) AddFx("muddy");
            if (st.SpeedMultiplier < 0.85f) AddFx("slow");
            if (st.StepNoiseRadius > 0f) AddFx("noisy");
            if (a.IsHidden) AddFx("hidden");
            foreach (var t in st.Traits) if (t.kind != TraitKind.Color) AddFx(TraitNames.Of(t));
            effects.Clear();
            if (names.Count == 0)
            {
                var none = Fx("NO EFFECT", auraColor);
                none.AddToClassList("sc-fx--none");
                effects.Add(none);
            }
            else foreach (var n in names) effects.Add(Fx(n, auraColor));
        }

        // Kartu esens: slime dekat esens -> nama, tombol, aura (dan toples yang sudah diintip yang auranya cocok), efek samping.
        // Semua teks dari EssenceInfo (disusun dari data esens), jadi selalu sama dengan efek sebenarnya.
        private void RefreshEssenceCard(SlimePiece a)
        {
            if (essenceCard == null) return;
            // tidak tampil saat dialog cerita / pause (kotak dialog menutupi bagian bawah layar)
            bool busy = StoryPlayer.IsPlaying || Time.timeScale == 0f;
            var pickup = a != null && a.HidingIn == null && !busy ? a.FindPickup() : null;
            var e = pickup != null ? pickup.Ingredient : null;
            essenceCard.EnableInClassList("essence-card--hidden", e == null);
            if (e == null) { shownEssence = null; return; }

            var st = a.Body.State;
            string aura = EssenceInfo.AuraValue(e);
            var match = new List<string>();
            foreach (var j in jars)
                if (j != null && j.IsPeeked && j.HasFriend && j.Recipe != null && aura != "" && j.Recipe.Aura == aura)
                    match.Add(j.Recipe.FriendName);
            string key = e.name + "|" + st.CanEat + "|" + string.Join(",", match);
            if (e == shownEssence && key == shownEssenceKey) return;
            shownEssence = e; shownEssenceKey = key;

            var col = TraitNames.AuraColor(aura);
            ecDot.style.backgroundColor = col;
            essenceCard.style.borderLeftColor = col;
            ecName.text = e.DisplayName.ToUpperInvariant();
            ecKey.text = st.CanEat ? "E  EAT" : "FULL · Q SPIT";
            ecAura.text = aura == "" ? "No aura · +1 size"
                : match.Count > 0 ? EssenceInfo.AuraName(e) + " aura · matches " + string.Join(" & ", match) + "'s jar!"
                : EssenceInfo.AuraName(e) + " aura (your last essence sets your aura) · +1 size";
            ecAura.EnableInClassList("ec-aura--match", match.Count > 0);
            ecEffects.Clear();
            var fx = EssenceInfo.Effects(e);
            if (fx.Count == 0) fx.Add("no side effect");
            foreach (var f in fx)
            {
                var l = new Label("· " + f) { pickingMode = PickingMode.Ignore };
                l.AddToClassList("ec-fx");
                ecEffects.Add(l);
            }
        }

        private static VisualElement Pip(string extra)
        {
            var p = new VisualElement { pickingMode = PickingMode.Ignore };
            p.AddToClassList("sc-pip");
            if (extra != null) p.AddToClassList(extra);
            return p;
        }

        private static VisualElement Fx(string name, Color dotColor)
        {
            var fx = new VisualElement { pickingMode = PickingMode.Ignore };
            fx.AddToClassList("sc-fx");
            var dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.AddToClassList("sc-fx-dot");
            dot.style.backgroundColor = dotColor;
            fx.Add(dot);
            var l = new Label(name) { pickingMode = PickingMode.Ignore };
            l.AddToClassList("sc-fx-name");
            fx.Add(l);
            return fx;
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
