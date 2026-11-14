using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GooNapped
{
    // Menu utama + pilih level (level terkunci sampai level sebelumnya selesai).
    // Tampilan = UI Toolkit (UI/MainMenu.uxml + MainMenu.uss), PLACEHOLDER sampai UI dari tim desainer.
    // Kontrol: W/S atau panah = pilih, A/D atau panah = geser kartu level, Enter/Space/E = pilih, Esc/Backspace = kembali. Mouse juga bisa.
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private string gameTitle = "GOO-NAPPED";
        [SerializeField] private string subtitle = "working title  ·  Game Off 2026: SHIFT";

        private class Item
        {
            public VisualElement element;
            public Action action;
            public bool enabled = true;
        }

        private enum Mode { Main, Levels, Confirm, Options }

        private Mode mode = Mode.Main;
        private VisualElement root, levelPanel, confirmPanel, fade, mainPanel;
        private Label hint, progress;
        private readonly List<Item> mainItems = new List<Item>();
        private readonly List<Item> levelItems = new List<Item>();   // kartu level + tombol bawah (Back, Reset)
        private readonly List<Item> confirmItems = new List<Item>();
        private int mainSel, levelSel, confirmSel;
        private int levelCards;
        private bool leaving;
        private Mode confirmReturn;
        private OptionsPanel options;

        private void Start()
        {
            Time.timeScale = 1f;
            if (document == null) document = FindFirstObjectByType<UIDocument>();
            if (document == null) { Debug.LogError("MainMenuController: UIDocument tidak ada (jalankan GooNapped > Blockout > Main menu)."); return; }
            root = document.rootVisualElement;
            var titleLabel = root.Q<Label>("title");   // versi lama tanpa logo
            if (titleLabel != null) titleLabel.text = gameTitle;
            root.Q<Label>("subtitle").text = subtitle;
            mainPanel = root.Q("main-panel");
            levelPanel = root.Q("level-panel");
            confirmPanel = root.Q("confirm");
            fade = root.Q("fade");
            hint = root.Q<Label>("hint");
            progress = root.Q<Label>("progress");
            options = new OptionsPanel(() => SwitchMode(Mode.Main, true));
            hint.parent.Insert(hint.parent.IndexOf(hint), options.Root);   // di bawah hint + fade
            BuildMain();
            BuildLevels();
            ShowMode(Mode.Main);
            // datang dari level (pause > Main Menu): buka dengan gaya transisinya; selain itu fade masuk dari hitam
            if (UiFx.PendingReveal != UiFx.Reveal.None)
            {
                fade.AddToClassList("mm-fade--clear");
                fade.style.display = DisplayStyle.None;
                root.schedule.Execute(() => { UiFx.RevealIfPending(root); fade.style.display = StyleKeyword.Null; }).StartingIn(30);
            }
            else root.schedule.Execute(() => fade.AddToClassList("mm-fade--clear")).StartingIn(50);
            root.schedule.Execute(AnimateSelected).Every(16);
            root.schedule.Execute(AnimateStatus).Every(50);
            root.schedule.Execute(FlickerTorch).Every(60);
            // preview level di panel kanan: klik mouse = jalankan level yang ditampilkan
            var preview = root.Q("lv-preview");
            preview.RegisterCallback<ClickEvent>(_ =>
            {
                if (leaving || mode != Mode.Levels) return;
                Activate(levelItems[Mathf.Clamp(detailShown, 0, levelCards - 1)]);
            });
        }

        // Obor dinding: cahaya & api berkedip pelan (noise), tetap per 2px supaya pixel.
        private void FlickerTorch()
        {
            var glowEl = root.Q("torch-glow");
            var flame = root.Q("torch-flame");
            var core = root.Q("torch-core");
            if (glowEl == null) return;
            float t = Time.unscaledTime;
            float n = Mathf.PerlinNoise(t * 3.2f, 0.5f);
            glowEl.style.opacity = 0.75f + n * 0.25f;
            float s = 0.94f + n * 0.12f;
            glowEl.style.scale = new Scale(new Vector2(s, s));
            if (flame != null) flame.style.height = Mathf.Round(12f + n * 6f);
            if (core != null) core.style.translate = new Translate(Mathf.Round((Mathf.PerlinNoise(t * 5f, 3f) - 0.5f) * 3f), 0);
        }

        // Item terpilih/hover: mengambang pelan (sinus) + shake tipis lambat (noise).
        // Hanya label yang bergerak; pita merah (mm-slash) tetap diam.
        private VisualElement floating;

        private void AnimateSelected()
        {
            List<Item> list = mode == Mode.Main ? mainItems : mode == Mode.Confirm ? confirmItems : mode == Mode.Levels ? levelItems : null;
            int sel = mode == Mode.Main ? mainSel : mode == Mode.Confirm ? confirmSel : levelSel;
            VisualElement target = null;
            if (list != null && sel >= 0 && sel < list.Count && list[sel].enabled)
                target = list[sel].element.Q(className: "mm-item-label") ?? (mode == Mode.Levels ? list[sel].element : null);
            if (target != floating)
            {
                if (floating != null) { floating.style.translate = StyleKeyword.Null; floating.style.rotate = StyleKeyword.Null; }
                floating = target;
            }
            if (floating == null) return;
            float t = Time.unscaledTime;
            float floatY = Mathf.Sin(t * 2.4f) * 2.5f;
            float floatX = Mathf.Sin(t * 1.3f) * 1f;
            float shakeX = (Mathf.PerlinNoise(t * 3f, 0.3f) - 0.5f) * 1.4f;
            float shakeY = (Mathf.PerlinNoise(0.7f, t * 3f) - 0.5f) * 1.4f;
            float shakeR = (Mathf.PerlinNoise(t * 2.5f, 5.1f) - 0.5f) * 1.2f;
            float baseX = floating.ClassListContains("mm-item-label") ? 10f : 0f;   // geser 10px dari USS (.mm-item--selected .mm-item-label)
            floating.style.translate = new Translate(baseX + floatX + shakeX, floatY + shakeY);
            floating.style.rotate = new Rotate(shakeR);
        }

        // ---------- isi ----------

        private void BuildMain()
        {
            var box = root.Q("items");
            box.Clear();
            mainItems.Clear();
            bool hasProgress = GameFlow.UnlockedIndex > 0;
            if (hasProgress)
                AddItem(box, mainItems, "Continue", () => Go(GameFlow.Levels[GameFlow.UnlockedIndex], true));
            AddItem(box, mainItems, "New Game", () =>
            {
                if (!hasProgress) { StartNewGame(); return; }
                Confirm("START OVER?", "Your progress will be lost.", Mode.Main, StartNewGame, true);
            });
            AddItem(box, mainItems, "Select Level", () => SwitchMode(Mode.Levels));
            AddItem(box, mainItems, "Options", () => SwitchMode(Mode.Options));
            AddItem(box, mainItems, "Quit", Quit);
            mainSel = 0;
            int done = GameFlow.UnlockedIndex;
            progress.text = hasProgress ? $"Next: {LevelCatalog.Name(Mathf.Clamp(done, 0, GameFlow.Levels.Length - 1))}   ·   {RescueLog.Count} friends rescued" : "";
        }

        private void BuildLevels()
        {
            var grid = root.Q("level-grid");
            var actions = root.Q("level-actions");
            grid.Clear(); actions.Clear(); levelItems.Clear();
            glitches.Clear(); nodeRing = null;
            for (int i = 0; i < GameFlow.Levels.Length; i++)
            {
                int idx = i;
                bool open = GameFlow.IsUnlocked(i);
                bool cleared = open && i < GameFlow.UnlockedIndex;
                bool current = open && !cleared;
                var row = new VisualElement();
                row.AddToClassList("mm-item");
                row.AddToClassList("lv-row");
                if (cleared) row.AddToClassList("lv-row--cleared");
                // jalur progres: garis + node belah ketupat (teal = selesai, kuning berdenyut = berikutnya, kosong = terkunci)
                var track = new VisualElement { pickingMode = PickingMode.Ignore };
                track.AddToClassList("lv-track");
                var line = new VisualElement { pickingMode = PickingMode.Ignore };
                line.AddToClassList("lv-track-line");
                if (i == 0) line.AddToClassList("lv-track-line--first");
                if (current) line.AddToClassList("lv-track-line--current");
                if (!open) line.AddToClassList("lv-track-line--locked");
                if (i == 0 && current) line.style.display = DisplayStyle.None;
                track.Add(line);
                if (current)
                {
                    nodeRing = new VisualElement { pickingMode = PickingMode.Ignore };
                    nodeRing.AddToClassList("lv-node-ring");
                    track.Add(nodeRing);
                }
                var node = new VisualElement { pickingMode = PickingMode.Ignore };
                node.AddToClassList("lv-node");
                if (current) node.AddToClassList("lv-node--current");
                if (!open) node.AddToClassList("lv-node--locked");
                track.Add(node);
                row.Add(track);
                var caret = new VisualElement { pickingMode = PickingMode.Ignore };
                caret.AddToClassList("mm-caret");
                row.Add(caret);
                // nama di atas pita (sama seperti item main menu); geser "tangga" dipasang di pembungkus supaya jalur tetap lurus
                var wrap = new VisualElement { pickingMode = PickingMode.Ignore };
                wrap.AddToClassList("mm-item-wrap");
                wrap.style.marginLeft = LevelRowOffsets[i % LevelRowOffsets.Length];
                var back = new VisualElement { pickingMode = PickingMode.Ignore };
                back.AddToClassList("mm-slash-back");
                var slash = new VisualElement { pickingMode = PickingMode.Ignore };
                slash.AddToClassList("mm-slash");
                wrap.Add(back);
                wrap.Add(slash);
                wrap.Add(MakeLabel((i + 1).ToString("00"), "lv-num"));
                string name = LevelName(i).ToUpperInvariant();
                if (open) wrap.Add(MakeLabel(name, "mm-item-label"));
                else
                {
                    // terkunci: teks glitch + salinan merah/teal + garis scan
                    var g = new VisualElement { pickingMode = PickingMode.Ignore };
                    g.AddToClassList("lv-glitch");
                    var red = MakeLabel(name, "lv-glitch-copy", "lv-glitch-copy--red");
                    var teal = MakeLabel(name, "lv-glitch-copy", "lv-glitch-copy--teal");
                    var main = MakeLabel(name, "mm-item-label");
                    var scan = new VisualElement { pickingMode = PickingMode.Ignore };
                    scan.AddToClassList("lv-scanline");
                    g.Add(red); g.Add(teal); g.Add(main); g.Add(scan);
                    wrap.Add(g);
                    glitches.Add(new Glitch { main = main, red = red, teal = teal, scan = scan, text = name, seed = i * 0.37f });
                }
                row.Add(wrap);
                row.EnableInClassList("mm-item--disabled", !open);
                grid.Add(row);
                var item = new Item { element = row, enabled = open, action = () => Go(GameFlow.Levels[idx], true) };
                Hook(item, levelItems);
                levelItems.Add(item);
            }
            levelCards = levelItems.Count;
            AddItem(actions, levelItems, "Back", () => SwitchMode(Mode.Main, true));
            AddItem(actions, levelItems, "Reset Progress", () => Confirm("RESET ALL?", "Levels lock again and rescued friends are forgotten.", Mode.Levels, () =>
            {
                GameFlow.ResetProgress();
                BuildMain();
                BuildLevels();
                ShowMode(Mode.Levels);
            }, true));
            levelSel =Mathf.Clamp(GameFlow.UnlockedIndex, 0, levelCards - 1);
            detailShown = -1;
            UpdateLevelDetail(levelSel);
        }

        // geser kiri tiap baris daftar level (px), pola "tangga" dari desain Figma Opsi A
        private static readonly float[] LevelRowOffsets = { 0, 18, 6, 26, 12, 30, 8, 22, 4, 28, 14 };

        private static string LevelName(int i) => LevelCatalog.Name(i);
        private static string AreaOf(int i) => LevelCatalog.Area(i);

        // Panel kanan mengikuti level yang sedang dipilih.
        private int detailShown = -1;

        // Gambar preview per level (urut GameFlow.Levels). Kosong = latar warna area + teks kecil.
        [SerializeField] private Texture2D[] levelPreviews = new Texture2D[0];

        private static Color AreaTint(int i) =>
            i < 3 ? new Color32(18, 52, 56, 255) : i < 6 ? new Color32(38, 26, 64, 255) : i < 10 ? new Color32(62, 36, 20, 255) : new Color32(60, 14, 24, 255);

        private void UpdateLevelDetail(int i)
        {
            if (i < 0 || i >= levelCards || i == detailShown) return;
            detailShown = i;
            bool open = GameFlow.IsUnlocked(i);
            bool cleared = open && i < GameFlow.UnlockedIndex;
            var info = LevelCatalog.Get(i);

            string num = (i + 1).ToString("00");
            root.Q<Label>("lv-num").text = num;
            root.Q<Label>("lv-num-shadow").text = num;
            root.Q<Label>("lv-name").text = open ? LevelName(i).ToUpperInvariant() : "? ? ?";
            root.Q<Label>("lv-area").text = AreaOf(i).ToUpperInvariant();

            // preview
            var shot = root.Q("lv-shot");
            var tex = open && i < levelPreviews.Length ? levelPreviews[i] : null;
            shot.style.backgroundImage = tex != null ? new StyleBackground(tex) : new StyleBackground(StyleKeyword.None);
            shot.style.backgroundColor = open ? AreaTint(i) : (Color)new Color32(18, 26, 43, 255);
            root.Q<Label>("lv-shot-empty").text = tex != null ? "" : open ? "preview soon" : "locked";
            root.Q("lv-preview").EnableInClassList("lv-preview--locked", !open);

            // satu baris info ringkas
            int saved = 0;
            foreach (var f in info.friends)
                if (string.IsNullOrEmpty(f.asset) ? cleared : RescueLog.IsRescued(f.asset)) saved++;
            int n = info.friends.Length;
            string text;
            if (!open) text = "LOCKED  ·  FINISH THE PREVIOUS LEVEL";
            else if (!info.rescue || n == 0) text = "SNEAK THROUGH  ·  NO FRIENDS HERE";
            else if (saved >= n) text = "RESCUE  ·  " + (n == 1 ? "FRIEND" : "ALL " + n + " FRIENDS") + " SAVED";
            else text = "RESCUE  ·  " + (n - saved) + (n - saved == 1 ? " FRIEND" : " FRIENDS") + " INSIDE";
            var infoLabel = root.Q<Label>("lv-info");
            infoLabel.text = text;
            infoLabel.EnableInClassList("lv-info--locked", !open);
        }

        // ---------- efek status ----------
        private class Glitch { public Label main, red, teal; public VisualElement scan; public string text; public float seed; }
        private readonly List<Glitch> glitches = new List<Glitch>();
        private VisualElement nodeRing;
        private const string GlitchChars = "#%&@$?!/<>=+*";

        // Nama level terkunci selalu glitch: sebagian huruf berganti acak, salinan merah/teal bergetar,
        // sesekali "lonjakan" geser lebih jauh. Node level berikutnya berdenyut.
        private void AnimateStatus()
        {
            if (mode != Mode.Levels) return;
            float t = Time.unscaledTime;
            nodeRing?.EnableInClassList("lv-node-ring--dim", Mathf.Repeat(t, 1.2f) > 0.6f);
            foreach (var g in glitches)
            {
                bool burst = Mathf.Repeat(t + g.seed * 3f, 2.2f) < 0.18f;
                // ganti huruf tiap ~0.15 dtk (bukan tiap frame supaya tetap terbaca sebagai teks)
                if (Mathf.Repeat(t + g.seed, 0.15f) < 0.05f || burst)
                {
                    var chars = g.text.ToCharArray();
                    float chance = burst ? 0.6f : 0.3f;
                    for (int c = 0; c < chars.Length; c++)
                        if (chars[c] != ' ' && UnityEngine.Random.value < chance) chars[c] = GlitchChars[UnityEngine.Random.Range(0, GlitchChars.Length)];
                    string s = new string(chars);
                    g.main.text = s;
                    g.red.text = s;
                    g.teal.text = s;
                }
                float jx = burst ? UnityEngine.Random.Range(-5f, 5f) : UnityEngine.Random.Range(-1f, 1f);
                g.red.style.translate = new Translate(-2f + jx, 0);
                g.teal.style.translate = new Translate(2f - jx, burst ? 1f : 0f);
                g.scan.style.top = burst ? UnityEngine.Random.Range(4f, 22f) : 13f;
            }
        }

        private static Label MakeLabel(string text, string cls, string cls2)
        {
            var l = MakeLabel(text, cls);
            l.AddToClassList(cls2);
            return l;
        }

        private static Label MakeLabel(string text, string cls)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.AddToClassList(cls);
            return l;
        }

        private void AddItem(VisualElement parent, List<Item> list, string text, Action action)
        {
            var row = new VisualElement();
            row.AddToClassList("mm-item");
            row.AddToClassList("mm-row-" + list.Count);   // geser bertingkat (MainMenu.uss)
            var caret = new VisualElement { pickingMode = PickingMode.Ignore };
            caret.AddToClassList("mm-caret");
            row.Add(caret);
            // label dibungkus supaya pita sorot (slash) bisa di belakangnya
            var wrap = new VisualElement { pickingMode = PickingMode.Ignore };
            wrap.AddToClassList("mm-item-wrap");
            var back = new VisualElement { pickingMode = PickingMode.Ignore };
            back.AddToClassList("mm-slash-back");
            var slash = new VisualElement { pickingMode = PickingMode.Ignore };
            slash.AddToClassList("mm-slash");
            wrap.Add(back);
            wrap.Add(slash);
            wrap.Add(MakeLabel(text.ToUpperInvariant(), "mm-item-label"));
            row.Add(wrap);
            parent.Add(row);
            var item = new Item { element = row, action = action };
            Hook(item, list);
            list.Add(item);
        }

        // mouse: arahkan = pilih, klik = jalankan
        private void Hook(Item item, List<Item> list)
        {
            item.element.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (leaving) return;
                int i = list.IndexOf(item);
                if (list == mainItems && mode == Mode.Main) mainSel = i;
                else if (list == levelItems && mode == Mode.Levels) levelSel = i;
                else if (list == confirmItems && mode == Mode.Confirm) confirmSel = i;
                Refresh();
            });
            item.element.RegisterCallback<ClickEvent>(_ =>
            {
                if (leaving) return;
                if ((list == mainItems && mode == Mode.Main) || (list == levelItems && mode == Mode.Levels) || (list == confirmItems && mode == Mode.Confirm))
                    Activate(item);
            });
        }

        // ---------- mode ----------

        // Pindah layar dengan "slash wipe" (Figma "Menu Flow" baris 1). reverse = kembali (arah terbalik, lebih cepat).
        private bool switching;

        private void SwitchMode(Mode m, bool reverse = false)
        {
            if (switching || leaving) return;
            switching = true;
            UiFx.SlashWipe(root, () =>
            {
                ShowMode(m);
                if (m == Mode.Options) options.Open();
                else if (m == Mode.Levels) UiFx.StaggerIn(root.Q("level-grid").Children());
                else if (m == Mode.Main) UiFx.StaggerIn(root.Q("items").Children());
            }, () => switching = false, reverse);
        }

        private void ShowMode(Mode m)
        {
            mode = m;
            // popup konfirmasi tetap menampilkan layar asalnya di belakang
            Mode under = m == Mode.Confirm ? confirmReturn : m;
            levelPanel.EnableInClassList("mm-hidden", under != Mode.Levels);
            confirmPanel.EnableInClassList("mm-hidden", m != Mode.Confirm);
            mainPanel.EnableInClassList("mm-hidden", under == Mode.Levels || under == Mode.Options);
            root.Q("title-block").EnableInClassList("mm-hidden", under == Mode.Levels || under == Mode.Options);   // judul stiker menggantikan logo
            hint.text = m == Mode.Levels
                ? "W / S: choose    Enter: play    Esc: back"
                : m == Mode.Confirm ? "Left / Right: choose    Enter: confirm    Esc: cancel"
                : m == Mode.Options ? "W / S: choose    A / D: change    Esc: back"
                : "W / S: choose    Enter: select";
            Refresh();
        }

        // Popup konfirmasi. summary = tampilkan apa yang akan hilang (level terbuka + teman selamat).
        private void Confirm(string title, string text, Mode back, Action yes, bool summary = false)
        {
            confirmReturn = back;
            root.Q<Label>("confirm-title").text = title;
            root.Q<Label>("confirm-text").text = text;
            root.Q("confirm-summary").style.display = summary ? DisplayStyle.Flex : DisplayStyle.None;
            if (summary)
            {
                root.Q<Label>("confirm-levels").text = (GameFlow.UnlockedIndex + 1) + " / " + GameFlow.Levels.Length;
                int friends = RescueLog.Count;
                root.Q<Label>("confirm-friends").text = friends.ToString();
                var gems = root.Q("confirm-gems");
                gems.Clear();
                for (int i = 0; i < Mathf.Min(friends, 8); i++)
                {
                    var g = new VisualElement { pickingMode = PickingMode.Ignore };
                    g.AddToClassList("mm-confirm-gem");
                    g.style.opacity = 1f - i * 0.11f;   // makin pudar: "akan hilang"
                    gems.Add(g);
                }
            }
            var box = root.Q("confirm-actions");
            box.Clear(); confirmItems.Clear();
            AddItem(box, confirmItems, "No", () => ShowMode(confirmReturn));
            AddItem(box, confirmItems, "Yes", yes);
            confirmSel = 0;   // default aman = No
            ShowMode(Mode.Confirm);
            UiFx.Fade(root.Q(className: "mm-confirm-dim"), 0f, 1f, 0.1f);
            UiFx.Stamp(root.Q("confirm-card"));
        }

        private void Refresh()
        {
            Mark(mainItems, mainSel, "mm-item--selected");
            Mark(confirmItems, confirmSel, "mm-item--selected");
            for (int i = 0; i < levelItems.Count; i++)
                levelItems[i].element.EnableInClassList("mm-item--selected", i == levelSel && mode == Mode.Levels);
            if (mode == Mode.Levels) UpdateLevelDetail(levelSel);
        }

        private static void Mark(List<Item> list, int sel, string cls)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i].element.EnableInClassList(cls, i == sel);
                list[i].element.EnableInClassList("mm-item--disabled", !list[i].enabled);
            }
        }

        // ---------- input ----------

        private void Update()
        {
            if (root == null || leaving || switching) return;
            // tahan W/S/A/D = ulang otomatis, roda mouse = naik/turun (MenuInput)
            bool up = MenuInput.Up, down = MenuInput.Down, left = MenuInput.Left, right = MenuInput.Right;
            bool ok = MenuInput.Ok, back = MenuInput.Back;

            switch (mode)
            {
                case Mode.Main:
                    if (up) mainSel = (mainSel - 1 + mainItems.Count) % mainItems.Count;
                    if (down) mainSel = (mainSel + 1) % mainItems.Count;
                    if (ok) Activate(mainItems[mainSel]);
                    break;
                case Mode.Levels:
                    NavigateLevels(up, down, left, right);
                    if (ok) Activate(levelItems[levelSel]);
                    if (back) SwitchMode(Mode.Main, true);
                    break;
                case Mode.Confirm:
                    if (left || up) confirmSel = 0;
                    if (right || down) confirmSel = 1;
                    if (ok) Activate(confirmItems[confirmSel]);
                    if (back) ShowMode(confirmReturn);
                    break;
                case Mode.Options:
                    options.HandleInput();   // Esc / Back menutup panel lalu kembali ke Main
                    return;
            }
            if (up || down || left || right) Refresh();
        }

        // Daftar level vertikal, lalu baris tombol (Back, Reset) di bawah.
        private void NavigateLevels(bool up, bool down, bool left, bool right)
        {
            int n = levelItems.Count;
            if (levelSel < levelCards)
            {
                if (up) levelSel = Mathf.Max(0, levelSel - 1);
                if (down) levelSel = levelSel + 1;   // dari level terakhir turun ke tombol Back
                if (left || right) levelSel = levelCards;
            }
            else
            {
                if (left) levelSel = Mathf.Max(levelCards, levelSel - 1);
                if (right) levelSel = Mathf.Min(n - 1, levelSel + 1);
                if (up) levelSel = Mathf.Max(0, levelCards - 1);
            }
        }

        private void Activate(Item item)
        {
            if (!item.enabled) return;
            item.action?.Invoke();
        }

        // ---------- pindah scene ----------

        private void StartNewGame()
        {
            GameFlow.ResetProgress();
            Go(GameFlow.SceneExists(GameFlow.Intro) ? GameFlow.Intro : GameFlow.Levels[0], true);
        }

        private void Go(string scene, bool slimeLeaves)
        {
            if (leaving) return;
            leaving = true;
            if (slimeLeaves && MenuSlime.Instance != null) MenuSlime.Instance.Leave();
            // "goo drip" (Figma "Menu Flow" baris 2): lendir menetes, nama level muncul, lalu pindah scene
            UiFx.GooDrip(root, UiFx.CardForLevel(scene), () => GameFlow.Load(scene));
        }

        private void Quit()
        {
            if (leaving) return;
            leaving = true;
            StartCoroutine(FadeThen(() =>
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }));
        }

        private IEnumerator FadeThen(Action then)
        {
            fade.RemoveFromClassList("mm-fade--clear");
            yield return new WaitForSecondsRealtime(0.7f);
            then();
        }
    }
}
