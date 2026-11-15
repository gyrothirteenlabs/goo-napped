using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GooNapped
{
    // Pemutar cutscene & dialog. Satu per scene (prefab Prefabs/UI/StoryUI; sudah ada di LevelSystems).
    // Panggil dari mana saja:
    //   StoryPlayer.Play(sequence, () => { ... selesai ... });
    //   StoryPlayer.Say("Nama", "Teks dialog", potret);
    // Kalau sedang memutar, permintaan baru diantrekan. Selama cerita: input pemain terkunci, game dijeda (opsional).
    //
    // Tampilan gaya intro RPG 16-bit (Story.uxml/uss): halaman bergambar = gambar berbingkai tetap + kotak teks tetap
    // di bawahnya; dialog = kotak teks di atas permainan. Tiap baris MENGGANTIKAN isi kotak (tidak menumpuk), dan baris
    // yang terlalu panjang dipecah per kalimat jadi beberapa "kotak", jadi teks tidak pernah scroll dan gambar tidak hilang.
    [RequireComponent(typeof(UIDocument))]
    public class StoryPlayer : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private float charsPerSecond = 40f;
        [Tooltip("Lama fade hitam antar halaman bergambar (detik).")]
        [SerializeField] private float fadeTime = 0.22f;
        [Tooltip("Batas karakter satu kotak teks (3 baris). Baris yang lebih panjang dipecah per kalimat.")]
        [SerializeField] private int maxCharsPerBox = 150;

        public static StoryPlayer Instance { get; private set; }
        public static bool IsPlaying => Instance != null && Instance.playing;

        private VisualElement root, backdrop, fade, layoutCine, layoutDialogue, cineFrame, cineImage, cineBox, portrait;
        private VisualElement cineNext, dialogueNext, barTop, barBottom;
        private bool cinematic;
        private float barHeight;   // tinggi garis sinematik sekarang (px panel)
        private Label cineSpeaker, cineText, speaker, dialogueText, skipHint;
        private InputAction interact, jump, pause;
        private readonly Queue<(StorySequence seq, Action done)> queue = new Queue<(StorySequence, Action)>();
        private bool playing;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q("story-root");
            backdrop = doc.Q("backdrop");
            fade = doc.Q("fade");
            layoutCine = doc.Q("layout-cine");
            layoutDialogue = doc.Q("layout-dialogue");
            cineFrame = doc.Q("cine-frame");
            cineImage = doc.Q("cine-image");
            cineBox = doc.Q("cine-box");
            cineSpeaker = doc.Q<Label>("cine-speaker");
            cineText = doc.Q<Label>("cine-text");
            cineNext = doc.Q("cine-next");
            portrait = doc.Q("portrait");
            speaker = doc.Q<Label>("speaker");
            dialogueText = doc.Q<Label>("dialogue-text");
            dialogueNext = doc.Q("dialogue-next");
            skipHint = doc.Q<Label>("skip-hint");
            barTop = doc.Q("bar-top");
            barBottom = doc.Q("bar-bottom");
            if (actions != null)
            {
                var map = actions.FindActionMap("Player", false);
                if (map != null)
                {
                    interact = map.FindAction("Interact", false);
                    jump = map.FindAction("Jump", false);
                    pause = map.FindAction("Pause", false);
                    map.Enable();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (playing) Time.timeScale = 1f;
        }

        public static void Play(StorySequence sequence, Action onDone = null)
        {
            if (sequence == null) { onDone?.Invoke(); return; }
            if (Instance == null)
            {
                Debug.LogWarning("StoryPlayer: tidak ada StoryUI di scene ini (tambahkan prefab Prefabs/UI/StoryUI).");
                onDone?.Invoke();
                return;
            }
            Instance.queue.Enqueue((sequence, onDone));
            if (!Instance.playing) Instance.StartCoroutine(Instance.RunQueue());
        }

        public static void Say(string speakerName, string text, Sprite portraitSprite = null, Action onDone = null) =>
            Play(StorySequence.Dialogue(speakerName, text, portraitSprite), onDone);

        // Lanjut dari kode/tombol UI (misal tombol layar sentuh).
        public static void Next()
        {
            if (Instance != null) Instance.nextRequested = true;
        }

        private bool nextRequested;

        // ---- input (berjalan walau Time.timeScale = 0) ----
        private bool AdvancePressed()
        {
            if (nextRequested) { nextRequested = false; return true; }
            if ((interact != null && interact.WasPressedThisFrame()) || (jump != null && jump.WasPressedThisFrame())) return true;
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)) return true;
            var m = Mouse.current;
            return m != null && m.leftButton.wasPressedThisFrame;
        }

        private bool SkipPressed()
        {
            if (pause != null && pause.WasPressedThisFrame()) return true;
            var kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
        }

        // ---- mode sinematik (LevelIntro): garis hitam atas/bawah, layar bisa digelapkan, dialog tampil di atas game ----
        public static bool CinematicActive => Instance != null && Instance.cinematic;

        public static void SetCinematic(bool on)
        {
            if (Instance == null) return;
            var s = Instance;
            s.cinematic = on;
            s.root.EnableInClassList("story--cinematic", on);
            if (on)
            {
                s.root.RemoveFromClassList("story--hidden");
                s.backdrop.AddToClassList("backdrop--hidden");
                s.layoutCine.RemoveFromClassList("layout--active");
                s.layoutDialogue.RemoveFromClassList("layout--active");
                s.skipHint.style.display = DisplayStyle.Flex;
            }
            s.StopCoroutine(nameof(AnimateBars));
            s.StartCoroutine(nameof(AnimateBars), on ? 30f : 0f);
        }

        private IEnumerator AnimateBars(float target)
        {
            while (!Mathf.Approximately(barHeight, target))
            {
                barHeight = Mathf.MoveTowards(barHeight, target, Time.unscaledDeltaTime * 90f);
                barTop.style.height = barBottom.style.height = Mathf.Round(barHeight);
                yield return null;
            }
            if (target <= 0f && !cinematic && !playing) root.AddToClassList("story--hidden");
        }

        // Layar hitam (0 = terang, 1 = hitam) bertahap; dipakai pembuka level.
        public static IEnumerator FadeScreen(float to, float time = 0.4f)
        {
            if (Instance == null) yield break;
            var s = Instance;
            s.root.RemoveFromClassList("story--hidden");
            float from = s.fade.resolvedStyle.opacity;
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                s.fade.style.opacity = Mathf.Lerp(from, to, Mathf.Floor(t / time * 5f) / 5f);
                yield return null;
            }
            s.fade.style.opacity = to;
        }

        public static void SetBlack(bool black)
        {
            if (Instance == null) return;
            Instance.root.RemoveFromClassList("story--hidden");
            Instance.fade.style.opacity = black ? 1f : 0f;
        }

        private static bool IsCine(StorySequence.Page p) => p.layout != StorySequence.Layout.Dialogue;

        private IEnumerator RunQueue()
        {
            playing = true;
            float prevScale = Time.timeScale;
            while (queue.Count > 0)
            {
                var (seq, done) = queue.Dequeue();
                if (seq.pauseGame) Time.timeScale = 0f;
                root.RemoveFromClassList("story--hidden");
                skipHint.style.display = seq.skippable ? DisplayStyle.Flex : DisplayStyle.None;
                bool startsCine = seq.pages.Count > 0 && IsCine(seq.pages[0]);
                fade.style.opacity = startsCine ? 1f : 0f;   // halaman bergambar pertama muncul dari hitam
                yield return null;   // jangan pakai tombol yang memicu cerita sebagai "lanjut"
                bool skipped = false;
                StorySequence.Page prev = null;
                foreach (var page in seq.pages)
                {
                    if (skipped) break;
                    yield return ShowPage(seq, prev, page, s => skipped = s);
                    prev = page;
                }
                if (prev != null && IsCine(prev)) yield return FadeTo(1f);   // tutup ke hitam
                if (!cinematic) root.AddToClassList("story--hidden");
                else { layoutCine.RemoveFromClassList("layout--active"); layoutDialogue.RemoveFromClassList("layout--active"); backdrop.AddToClassList("backdrop--hidden"); }
                fade.style.opacity = 0f;
                Time.timeScale = prevScale <= 0f ? 1f : prevScale;
                done?.Invoke();
            }
            playing = false;
        }

        private IEnumerator ShowPage(StorySequence seq, StorySequence.Page prev, StorySequence.Page page, Action<bool> setSkipped)
        {
            bool cine = IsCine(page);
            // ganti gambar lewat hitam (gaya intro 16-bit); dialog langsung
            if (cine && prev != null && IsCine(prev)) yield return FadeTo(1f);
            layoutCine.EnableInClassList("layout--active", cine);
            layoutDialogue.EnableInClassList("layout--active", !cine);
            backdrop.EnableInClassList("backdrop--hidden", !cine);
            Label target;
            VisualElement nextArrow;
            if (cine)
            {
                cineFrame.EnableInClassList("cine-frame--empty", page.image == null);
                cineImage.style.backgroundImage = page.image != null ? new StyleBackground(page.image) : new StyleBackground(StyleKeyword.None);
                cineText.text = "";
                cineSpeaker.AddToClassList("speaker-tab--hidden");
                cineBox.EnableInClassList("cine-box--hidden", page.lines.Count == 0);
                target = cineText; nextArrow = cineNext;
                yield return FadeTo(0f);
            }
            else
            {
                dialogueText.text = "";
                target = dialogueText; nextArrow = dialogueNext;
            }

            if (page.lines.Count == 0)
            {
                // halaman gambar saja
                bool skip = false;
                yield return WaitAdvance(seq, page, nextArrow, s => skip = s);
                if (skip) setSkipped(true);
                yield break;
            }

            foreach (var line in page.lines)
            {
                var spk = cine ? cineSpeaker : speaker;
                spk.text = line.speaker ?? "";
                spk.EnableInClassList("speaker-tab--hidden", string.IsNullOrEmpty(line.speaker));
                if (!cine)
                {
                    portrait.EnableInClassList("portrait--hidden", line.portrait == null);
                    if (line.portrait != null) portrait.style.backgroundImage = new StyleBackground(line.portrait);
                }
                foreach (var chunk in Chunks(line.text ?? "", maxCharsPerBox))
                {
                    // ketik bertahap; tekan tombol = tampilkan seluruh kotak
                    nextArrow.AddToClassList("next--hidden");
                    float shown = 0f;
                    while (shown < chunk.Length)
                    {
                        if (seq.skippable && SkipPressed()) { setSkipped(true); yield break; }
                        if (AdvancePressed()) { yield return null; break; }
                        shown += Time.unscaledDeltaTime * charsPerSecond;
                        target.text = chunk.Substring(0, Mathf.Min(chunk.Length, (int)shown));
                        yield return null;
                    }
                    target.text = chunk;
                    bool skipNow = false;
                    yield return WaitAdvance(seq, page, nextArrow, s => skipNow = s);
                    if (skipNow) { setSkipped(true); yield break; }
                }
            }
        }

        // Pecah teks jadi potongan <= max karakter, di batas kalimat (. ! ? …) kalau bisa, kalau tidak di batas kata.
        public static List<string> Chunks(string text, int max)
        {
            var result = new List<string>();
            text = text.Trim();
            while (text.Length > max)
            {
                int cut = -1;
                for (int i = Mathf.Min(max, text.Length - 1); i > max / 3; i--)
                {
                    char c = text[i - 1];
                    if ((c == '.' || c == '!' || c == '?' || c == ':' || c == ';' || c == '…') && text[i] == ' ') { cut = i; break; }
                }
                if (cut < 0) cut = text.LastIndexOf(' ', max);
                if (cut <= 0) cut = max;
                result.Add(text.Substring(0, cut).Trim());
                text = text.Substring(cut).Trim();
            }
            if (text.Length > 0 || result.Count == 0) result.Add(text);
            return result;
        }

        private IEnumerator WaitAdvance(StorySequence seq, StorySequence.Page page, VisualElement nextArrow, Action<bool> setSkipped)
        {
            nextArrow.RemoveFromClassList("next--hidden");
            float t = 0f;
            while (true)
            {
                if (seq.skippable && SkipPressed()) { setSkipped(true); break; }
                if (AdvancePressed()) break;
                t += Time.unscaledDeltaTime;
                if (page.autoAdvance > 0f && t >= page.autoAdvance) break;
                nextArrow.style.opacity = Mathf.Repeat(Time.unscaledTime * 1.6f, 1f) < 0.6f ? 1f : 0f;   // kedip pixel (bukan pudar)
                yield return null;
            }
            nextArrow.AddToClassList("next--hidden");
            yield return null;
        }

        private IEnumerator FadeTo(float to)
        {
            float from = fade.resolvedStyle.opacity;
            if (Mathf.Approximately(from, to)) { fade.style.opacity = to; yield break; }
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                // fade bertangga 4 langkah (rasa 16-bit), bukan halus
                float k = Mathf.Floor(Mathf.Clamp01(t / fadeTime) * 4f) / 4f;
                fade.style.opacity = Mathf.Lerp(from, to, k);
                yield return null;
            }
            fade.style.opacity = to;
        }
    }
}
