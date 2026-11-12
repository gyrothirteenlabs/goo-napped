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

        // Nama tampil level (urut GameFlow.Levels) dan areanya. Teks game = bahasa Inggris.
        private static readonly string[] LevelNames =
        {
            "The Gate", "Hedge Garden", "Mud Path", "Empty Cells", "Cell Block", "Drain Lab",
            "Great Hall", "Dining Hall", "Gallery", "Tower Stairs", "The Summit",
        };
        private static string AreaOf(int i) => i < 3 ? "Castle Grounds" : i < 6 ? "Dungeon" : i < 10 ? "Upper Castle" : "Summit";

        private class Item
        {
            public VisualElement element;
            public Action action;
            public bool enabled = true;
        }

        private enum Mode { Main, Levels, Confirm }

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

        private void Start()
        {
            Time.timeScale = 1f;
            if (document == null) document = FindFirstObjectByType<UIDocument>();
            if (document == null) { Debug.LogError("MainMenuController: UIDocument tidak ada (jalankan GooNapped > Blockout > Main menu)."); return; }
            root = document.rootVisualElement;
            root.Q<Label>("title").text = gameTitle;
            root.Q<Label>("subtitle").text = subtitle;
            mainPanel = root.Q("main-panel");
            levelPanel = root.Q("level-panel");
            confirmPanel = root.Q("confirm");
            fade = root.Q("fade");
            hint = root.Q<Label>("hint");
            progress = root.Q<Label>("progress");
            BuildMain();
            BuildLevels();
            ShowMode(Mode.Main);
            // fade masuk dari hitam (satu frame setelah layout supaya transisi jalan)
            root.schedule.Execute(() => fade.AddToClassList("mm-fade--clear")).StartingIn(50);
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
                Confirm("Start a new game?\nYour progress will be lost.", Mode.Main, StartNewGame);
            });
            AddItem(box, mainItems, "Select Level", () => ShowMode(Mode.Levels));
            AddItem(box, mainItems, "Quit", Quit);
            mainSel = 0;
            int done = GameFlow.UnlockedIndex;
            progress.text = hasProgress ? $"Next: {LevelNames[Mathf.Clamp(done, 0, LevelNames.Length - 1)]}   ·   {RescueLog.Count} friends rescued" : "";
        }

        private void BuildLevels()
        {
            var grid = root.Q("level-grid");
            var actions = root.Q("level-actions");
            grid.Clear(); actions.Clear(); levelItems.Clear();
            for (int i = 0; i < GameFlow.Levels.Length; i++)
            {
                int idx = i;
                bool open = GameFlow.IsUnlocked(i);
                var card = new VisualElement();
                card.AddToClassList("mm-card");
                if (!open) card.AddToClassList("mm-card--locked");
                card.Add(MakeLabel((i + 1).ToString("00") + (open ? "" : "  · locked"), "mm-card-num"));
                card.Add(MakeLabel(i < LevelNames.Length ? LevelNames[i] : GameFlow.Levels[i], "mm-card-name"));
                card.Add(MakeLabel(AreaOf(i), "mm-card-area"));
                grid.Add(card);
                var item = new Item { element = card, enabled = open, action = () => Go(GameFlow.Levels[idx], true) };
                Hook(item, levelItems);
                levelItems.Add(item);
            }
            levelCards = levelItems.Count;
            AddItem(actions, levelItems, "Back", () => ShowMode(Mode.Main));
            AddItem(actions, levelItems, "Reset Progress", () => Confirm("Reset all progress?\nLevels lock again and rescued friends are forgotten.", Mode.Levels, () =>
            {
                GameFlow.ResetProgress();
                BuildMain();
                BuildLevels();
                ShowMode(Mode.Levels);
            }));
            levelSel = Mathf.Clamp(GameFlow.UnlockedIndex, 0, levelCards - 1);
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
            row.Add(MakeLabel("▶", "mm-caret"));
            row.Add(MakeLabel(text, "mm-item-label"));
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

        private void ShowMode(Mode m)
        {
            mode = m;
            levelPanel.EnableInClassList("mm-hidden", m != Mode.Levels);
            confirmPanel.EnableInClassList("mm-hidden", m != Mode.Confirm);
            mainPanel.EnableInClassList("mm-hidden", m == Mode.Levels);
            hint.text = m == Mode.Levels
                ? "Arrows / WASD: choose    Enter: play    Esc: back"
                : m == Mode.Confirm ? "Left / Right: choose    Enter: confirm    Esc: cancel"
                : "W / S: choose    Enter: select";
            Refresh();
        }

        private void Confirm(string text, Mode back, Action yes)
        {
            confirmReturn = back;
            root.Q<Label>("confirm-text").text = text;
            var box = root.Q("confirm-actions");
            box.Clear(); confirmItems.Clear();
            AddItem(box, confirmItems, "No", () => ShowMode(confirmReturn));
            AddItem(box, confirmItems, "Yes", yes);
            confirmSel = 0;
            ShowMode(Mode.Confirm);
        }

        private void Refresh()
        {
            Mark(mainItems, mainSel, "mm-item--selected");
            Mark(confirmItems, confirmSel, "mm-item--selected");
            for (int i = 0; i < levelItems.Count; i++)
            {
                var el = levelItems[i].element;
                el.EnableInClassList(i < levelCards ? "mm-card--selected" : "mm-item--selected", i == levelSel && mode == Mode.Levels);
            }
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
            if (root == null || leaving) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            bool up = kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
            bool down = kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame;
            bool left = kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame;
            bool right = kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame;
            bool ok = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame;
            bool back = kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame;

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
                    if (back) ShowMode(Mode.Main);
                    break;
                case Mode.Confirm:
                    if (left || up) confirmSel = 0;
                    if (right || down) confirmSel = 1;
                    if (ok) Activate(confirmItems[confirmSel]);
                    if (back) ShowMode(confirmReturn);
                    break;
            }
            if (up || down || left || right) Refresh();
        }

        // Kartu level 4 kolom, lalu baris tombol (Back, Reset).
        private void NavigateLevels(bool up, bool down, bool left, bool right)
        {
            const int cols = 4;
            int n = levelItems.Count;
            if (levelSel < levelCards)
            {
                if (left) levelSel = Mathf.Max(0, levelSel - 1);
                if (right) levelSel = Mathf.Min(levelCards - 1, levelSel + 1);
                if (up && levelSel >= cols) levelSel -= cols;
                if (down) levelSel = levelSel + cols < levelCards ? levelSel + cols : levelCards;
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
            StartCoroutine(FadeThen(() => GameFlow.Load(scene)));
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
