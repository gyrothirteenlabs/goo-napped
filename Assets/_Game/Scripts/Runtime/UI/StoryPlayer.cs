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
    [RequireComponent(typeof(UIDocument))]
    public class StoryPlayer : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private float charsPerSecond = 45f;
        [SerializeField] private float fadeTime = 0.25f;

        public static StoryPlayer Instance { get; private set; }
        public static bool IsPlaying => Instance != null && Instance.playing;

        private VisualElement root, backdrop, portrait;
        private VisualElement[] layouts;
        private VisualElement[] images;
        private VisualElement[] lineBoxes;
        private Label speaker, dialogueText, next, skipHint;
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
            layouts = new[] { doc.Q("layout-left"), doc.Q("layout-top"), doc.Q("layout-only"), doc.Q("layout-dialogue") };
            images = new[] { doc.Q("left-image"), doc.Q("top-image"), doc.Q("only-image"), null };
            lineBoxes = new[] { doc.Q("left-lines"), doc.Q("top-lines"), doc.Q("only-lines"), null };
            portrait = doc.Q("portrait");
            speaker = doc.Q<Label>("speaker");
            dialogueText = doc.Q<Label>("dialogue-text");
            next = doc.Q<Label>("next");
            skipHint = doc.Q<Label>("skip-hint");
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
                yield return null;   // jangan pakai tombol yang memicu cerita sebagai "lanjut"
                bool skipped = false;
                foreach (var page in seq.pages)
                {
                    if (skipped) break;
                    yield return ShowPage(seq, page, s => skipped = s);
                }
                root.AddToClassList("story--hidden");
                Time.timeScale = prevScale <= 0f ? 1f : prevScale;
                done?.Invoke();
            }
            playing = false;
        }

        private IEnumerator ShowPage(StorySequence seq, StorySequence.Page page, Action<bool> setSkipped)
        {
            int li = (int)page.layout;
            for (int i = 0; i < layouts.Length; i++) layouts[i].EnableInClassList("layout--active", i == li);
            backdrop.EnableInClassList("backdrop--hidden", page.layout == StorySequence.Layout.Dialogue);
            var layout = layouts[li];
            if (images[li] != null)
            {
                images[li].style.backgroundImage = page.image != null ? new StyleBackground(page.image) : new StyleBackground();
                images[li].style.display = page.image != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (lineBoxes[li] != null) lineBoxes[li].Clear();
            yield return Fade(layout, 0f, 1f);

            if (page.lines.Count == 0)
            {
                // halaman gambar saja
                bool skip = false;
                yield return WaitAdvance(seq, page, s => skip = s);
                if (skip) { setSkipped(true); yield break; }
            }

            foreach (var line in page.lines)
            {
                Label target;
                if (page.layout == StorySequence.Layout.Dialogue)
                {
                    speaker.text = line.speaker ?? "";
                    speaker.style.display = string.IsNullOrEmpty(line.speaker) ? DisplayStyle.None : DisplayStyle.Flex;
                    portrait.EnableInClassList("portrait--hidden", line.portrait == null);
                    if (line.portrait != null) portrait.style.backgroundImage = new StyleBackground(line.portrait);
                    target = dialogueText;
                }
                else
                {
                    // gaya cutscene: baris baru ditambahkan di bawah baris sebelumnya
                    if (!string.IsNullOrEmpty(line.speaker))
                    {
                        var sp = new Label(line.speaker);
                        sp.AddToClassList("line-speaker");
                        lineBoxes[li].Add(sp);
                    }
                    target = new Label();
                    target.AddToClassList("line");
                    lineBoxes[li].Add(target);
                }

                // ketik bertahap; tekan tombol = tampilkan seluruh baris
                string full = line.text ?? "";
                next.AddToClassList("next--hidden");
                float shown = 0f;
                while (shown < full.Length)
                {
                    if (seq.skippable && SkipPressed()) { setSkipped(true); yield break; }
                    if (AdvancePressed()) { shown = full.Length; yield return null; break; }
                    shown += Time.unscaledDeltaTime * charsPerSecond;
                    target.text = full.Substring(0, Mathf.Min(full.Length, (int)shown));
                    yield return null;
                }
                target.text = full;

                bool skipNow = false;
                yield return WaitAdvance(seq, page, s => skipNow = s);
                if (skipNow) { setSkipped(true); yield break; }
            }
            yield return Fade(layout, 1f, 0f);
        }

        private IEnumerator WaitAdvance(StorySequence seq, StorySequence.Page page, Action<bool> setSkipped)
        {
            next.RemoveFromClassList("next--hidden");
            float t = 0f;
            while (true)
            {
                if (seq.skippable && SkipPressed()) { setSkipped(true); break; }
                if (AdvancePressed()) break;
                t += Time.unscaledDeltaTime;
                if (page.autoAdvance > 0f && t >= page.autoAdvance) break;
                next.style.opacity = Mathf.PingPong(Time.unscaledTime * 2f, 1f);   // kedip
                yield return null;
            }
            next.AddToClassList("next--hidden");
            yield return null;
        }

        private IEnumerator Fade(VisualElement e, float from, float to)
        {
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                e.style.opacity = Mathf.Lerp(from, to, t / fadeTime);
                yield return null;
            }
            e.style.opacity = to;
        }
    }
}
