using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GooNapped
{
    // Pause (Esc / Start) dan ulang level cepat (R / Select).
    // Tampilan = UI Toolkit dibuat lewat kode, gaya di UI/MainMenu.uss + UI/Options.uss. PLACEHOLDER sampai UI dari tim desainer.
    // Isi: Resume, Retry, Options, Main Menu. Keyboard W/S + Enter, Esc = lanjut / kembali. Mouse juga bisa.
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private string actionMap = "Player";
        [SerializeField] private string pauseAction = "Pause";
        [SerializeField] private string restartAction = "Restart";

        [Header("Tampilan (UI Toolkit)")]
        [Tooltip("Kosong = pinjam PanelSettings dari UIDocument lain di scene (HUD).")]
        [SerializeField] private PanelSettings panelSettings;
        [SerializeField] private StyleSheet[] styleSheets;
        [SerializeField] private int sortingOrder = 50;

        private class Item
        {
            public VisualElement element;
            public Action action;
        }

        private InputAction pause, restart;
        private bool paused;
        private UIDocument document;
        private VisualElement overlay, pausePanel;
        private OptionsPanel options;
        private readonly List<Item> items = new List<Item>();
        private int sel;

        public bool IsPaused => paused;
        // Pemain tidak membaca input saat pause, dan juga 1 frame setelah lanjut: tombol Space/E yang dipakai memilih
        // "Resume" jangan ikut membelah/makan (dulu tembus ke PlayerBrain).
        public static bool BlocksInput => anyPaused || Time.frameCount <= resumedFrame + 1;
        private static bool anyPaused;
        private static int resumedFrame = -10;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { anyPaused = false; resumedFrame = -10; }

        private void OnEnable()
        {
            if (actions == null) return;
            var map = actions.FindActionMap(actionMap, true);
            pause = map.FindAction(pauseAction, false);
            restart = map.FindAction(restartAction, false);
            map.Enable();
        }

        private void OnDisable()
        {
            if (paused) SetPaused(false);
        }

        private void Start()
        {
            BuildUi();
        }

        private void Update()
        {
            if (StoryPlayer.IsPlaying || LevelIntro.Running || leaving) return;   // Esc dipakai untuk melewati cerita / pembuka level   // Esc dipakai untuk melewati cerita; saat transisi input diabaikan
            if (paused && options != null && options.IsOpen)
            {
                options.HandleInput();   // Esc di sini = kembali ke daftar pause
                return;
            }
            if (pause != null && pause.WasPressedThisFrame()) { SetPaused(!paused); return; }
            if (restart != null && restart.WasPressedThisFrame()) { Retry(); return; }
            if (paused) HandleMenuInput();
        }

        public void SetPaused(bool on)
        {
            if (on == paused && overlay != null && overlay.ClassListContains("mm-hidden") == !on) return;
            if (paused && !on) resumedFrame = Time.frameCount;
            paused = on;
            anyPaused = on;
            Time.timeScale = on ? 0f : 1f;
            AudioListener.pause = on;
            if (overlay == null) return;
            sel = 0;
            ShowOptions(false);
            if (on) PlayOpen();
            else PlayClose();
        }

        // ---------- transisi (storyboard Figma "Menu Flow" baris 3) ----------
        // buka 0.25 dtk: layar meredup abu -> slab meluncur dari kiri -> PAUSED naik -> item bergiliran
        private void PlayOpen()
        {
            overlay.RemoveFromClassList("mm-hidden");
            UpdateLevelCard();
            UiFx.Fade(grey, 0f, 1f, 0.12f);
            UiFx.SlideIn(slab, new Vector2(-420, 0), 0.16f);
            UiFx.SlideIn(pausedWord, new Vector2(0, 260), 0.2f, 0.05f);
            UiFx.StaggerIn(itemList.Children(), 0.04f, -50f);
            UiFx.SlideIn(levelCard, new Vector2(0, 80), 0.18f, 0.1f);
            Refresh();
        }

        // tutup 0.2 dtk: kebalikannya, lebih cepat
        private void PlayClose()
        {
            UiFx.Tween(overlay, 0.12f, t =>
            {
                slab.style.translate = new Translate(-420f * t * t, 0);
                pausedWord.style.translate = new Translate(0, 260f * t * t);
                levelCard.style.translate = new Translate(0, 80f * t * t);
                itemList.style.opacity = 1f - t;
                grey.style.opacity = 1f - t;
            }, () =>
            {
                if (paused) return;   // dibuka lagi sebelum selesai
                overlay.AddToClassList("mm-hidden");
                slab.style.translate = StyleKeyword.Null;
                pausedWord.style.translate = StyleKeyword.Null;
                levelCard.style.translate = StyleKeyword.Null;
                itemList.style.opacity = StyleKeyword.Null;
            });
        }

        // ---------- tampilan (desain Figma "Menu Flow > Pause") ----------

        private VisualElement grey, slab, pausedWord, itemList, levelCard;
        private Label cardNum, cardName, cardArea, cardInfo;
        private static readonly float[] ItemOffsets = { 0, 18, 6, 24 };

        private static VisualElement Box(VisualElement parent, string cls)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList(cls);
            parent.Add(e);
            return e;
        }

        private static Label Text(VisualElement parent, string text, string cls)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.AddToClassList(cls);
            parent.Add(l);
            return l;
        }

        private void BuildUi()
        {
            var ps = panelSettings;
            if (ps == null)
            {
                foreach (var other in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
                    if (other.panelSettings != null) { ps = other.panelSettings; break; }
            }
            if (ps == null) { Debug.LogWarning("PauseMenu: PanelSettings tidak ada, menu pause tidak tampil."); return; }

            var go = new GameObject("PauseUI");
            go.transform.SetParent(transform, false);
            document = go.AddComponent<UIDocument>();
            document.panelSettings = ps;
            document.sortingOrder = sortingOrder;

            var root = document.rootVisualElement;
            if (styleSheets != null)
                foreach (var s in styleSheets) if (s != null) root.styleSheets.Add(s);

            overlay = new VisualElement();
            overlay.AddToClassList("pz-overlay");
            overlay.AddToClassList("mm-hidden");
            root.Add(overlay);

            grey = Box(overlay, "pz-grey");
            pausePanel = Box(overlay, "pz-panel");
            slab = Box(pausePanel, "pz-slab");
            Box(slab, "pz-strip");
            Box(slab, "pz-strip2");
            pausedWord = Box(pausePanel, "pz-word");
            Text(pausedWord, "PAUSED", "pz-word-shadow");
            Text(pausedWord, "PAUSED", "pz-word-text");

            itemList = Box(pausePanel, "pz-items");
            itemList.pickingMode = PickingMode.Position;
            AddItem(itemList, "Resume", () => SetPaused(false));
            AddItem(itemList, "Retry", Retry);
            AddItem(itemList, "Options", () => ShowOptions(true));
            AddItem(itemList, "Main Menu", ToMainMenu);

            // kartu level yang sedang dimainkan (kanan bawah)
            levelCard = Box(pausePanel, "pz-card");
            var head = Box(levelCard, "pz-card-head");
            cardNum = Text(head, "", "pz-card-num");
            var names = Box(head, "pz-card-names");
            cardName = Text(names, "", "pz-card-name");
            cardArea = Text(names, "", "pz-card-area");
            cardInfo = Text(levelCard, "", "pz-card-info");

            Text(pausePanel, "W / S: choose    Enter: select    Esc: resume    R: retry", "pz-hint");

            options = new OptionsPanel(() => ShowOptions(false));
            overlay.Add(options.Root);

            overlay.EnableInClassList("mm-hidden", !paused);
            Refresh();
        }

        // ---------- pindah scene dengan transisi ----------
        private bool leaving;

        // Retry: lendir menetes + kartu "TRY AGAIN", lalu muat ulang. Level baru membuka dengan lendir surut.
        private void Retry()
        {
            if (leaving) return;
            if (document == null) { GameFlow.ReloadCurrent(); return; }
            leaving = true;
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            UiFx.GooDrip(document.rootVisualElement, UiFx.CardForLevel(scene, "TRY AGAIN"), () =>
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
                GameFlow.ReloadCurrent();
            });
        }

        // Main Menu: lembar miring menutup layar, main menu membuka dengan lembar yang sama.
        private void ToMainMenu()
        {
            if (leaving) return;
            if (document == null) { GameFlow.LoadMainMenu(); return; }
            leaving = true;
            UiFx.SlashCover(document.rootVisualElement, () =>
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
                GameFlow.LoadMainMenu();
            });
        }

        private void UpdateLevelCard()
        {
            int i = GameFlow.IndexOf(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            levelCard.style.display = i < 0 ? DisplayStyle.None : DisplayStyle.Flex;
            if (i < 0) return;
            var info = LevelCatalog.Get(i);
            cardNum.text = (i + 1).ToString("00");
            cardName.text = info.name.ToUpperInvariant();
            cardArea.text = LevelCatalog.Area(i).ToUpperInvariant();
            string text;
            if (!info.rescue || info.friends.Length == 0) text = "SNEAK  ·  REACH THE VENT";
            else
            {
                var left = new List<string>();
                foreach (var f in info.friends)
                    if (!(string.IsNullOrEmpty(f.asset) ? LevelCatalog.IsCleared(i) : RescueLog.IsRescued(f.asset))) left.Add(f.name.ToUpperInvariant());
                text = left.Count == 0 ? "RESCUE  ·  EVERYONE IS SAFE"
                    : "RESCUE  ·  " + string.Join(" & ", left) + (left.Count == 1 ? " IS" : " ARE") + " STILL IN A JAR";
            }
            cardInfo.text = text;
        }

        private void AddItem(VisualElement parent, string text, Action action)
        {
            var row = new VisualElement();
            row.AddToClassList("mm-item");
            row.AddToClassList("pz-item");
            row.style.marginLeft = ItemOffsets[items.Count % ItemOffsets.Length];
            var caret = new VisualElement { pickingMode = PickingMode.Ignore };   // belah ketupat pixel (MainMenu.uss)
            caret.AddToClassList("mm-caret");
            row.Add(caret);
            var wrap = Box(row, "mm-item-wrap");
            Box(wrap, "mm-slash-back");
            Box(wrap, "mm-slash");
            Text(wrap, text.ToUpperInvariant(), "mm-item-label");
            parent.Add(row);

            var item = new Item { element = row, action = action };
            items.Add(item);
            row.RegisterCallback<PointerEnterEvent>(_ => { if (!OptionsOpen) { sel = items.IndexOf(item); Refresh(); } });
            row.RegisterCallback<ClickEvent>(_ => { if (paused && !OptionsOpen) item.action(); });
        }

        private bool OptionsOpen => options != null && options.IsOpen;

        public void ShowOptions(bool on)
        {
            if (options == null) return;
            pausePanel.EnableInClassList("mm-hidden", on);
            if (on) options.Open();
            else if (options.IsOpen) options.Close();   // Close memanggil ShowOptions(false) lagi, aman karena IsOpen sudah false
            Refresh();
        }

        private void HandleMenuInput()
        {
            if (items.Count == 0) return;
            bool up = MenuInput.Up, down = MenuInput.Down, ok = MenuInput.Ok;   // tahan = ulang, roda mouse juga
            if (up) sel = (sel - 1 + items.Count) % items.Count;
            if (down) sel = (sel + 1) % items.Count;
            if (up || down) Refresh();
            if (ok) items[sel].action();
        }

        private void Refresh()
        {
            for (int i = 0; i < items.Count; i++)
                items[i].element.EnableInClassList("mm-item--selected", i == sel);
        }
    }
}
