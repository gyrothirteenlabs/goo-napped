using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GooNapped
{
    // Panel Options layar penuh (UI Toolkit, dibuat lewat kode), dipakai main menu dan pause menu.
    // Desain: Figma "Menu Flow > Options". Gaya di UI/Options.uss (kelas op-*, art-*), pita/penanda dari MainMenu.uss.
    //   Music / Sound : bar 10 blok pixel (klik blok = set langsung), panah < > saat dipilih
    //   Fullscreen    : OFF / ON (klik salah satu)
    //   Resolution    : < 1920 x 1080 >
    //   Kanan         : ilustrasi slime yang bereaksi ke volume (SlimeMusicArt)
    // Keyboard: W/S pilih, A/D ubah nilai, Enter = pilih/ubah, Esc = kembali.
    public class OptionsPanel
    {
        private class Row
        {
            public VisualElement element;
            public Action<int> change;   // -1 / +1
            public Action activate;      // Enter / klik label
            public Action refresh;       // perbarui tampilan nilai
            public bool repeats;         // tahan A/D = ulang otomatis (bar volume)
        }

        private static bool MenuInputPressedThisFrame()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame
                                  || kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame);
        }

        public VisualElement Root { get; }
        public bool IsOpen { get; private set; }

        private readonly List<Row> rows = new List<Row>();
        private readonly VisualElement list, artFrame;
        private readonly Label mood;
        private readonly Action onBack;
        private List<Vector2Int> resolutions;
        private int sel;
        private static readonly float[] RowOffsets = { 0, 14, 4, 18, 8 };

        public OptionsPanel(Action onBack)
        {
            this.onBack = onBack;
            Root = new VisualElement();
            Root.AddToClassList("op-panel");
            Root.AddToClassList("mm-hidden");

            Add(Root, "op-dim");
            Add(Root, "op-slab");
            Add(Root, "op-accent");
            Add(Root, "op-accent2");

            // judul stiker (sama dengan Select Level)
            var titleWrap = Add(Root, "lv-title-wrap");
            Add(titleWrap, "lv-title-shadow");
            Add(titleWrap, "lv-title-bg");
            titleWrap.Add(MakeLabel("OPTIONS", "lv-title"));

            list = Add(Root, "op-list");
            list.pickingMode = PickingMode.Position;
            AddBarRow("Music", () => GameSettings.MusicLevel, v => GameSettings.MusicLevel = v);
            AddBarRow("Sound", () => GameSettings.SfxLevel, v => GameSettings.SfxLevel = v);
            AddToggleRow("Fullscreen", () => GameSettings.Fullscreen, v => GameSettings.Fullscreen = v);
            AddPickRow("Resolution", ResolutionText, StepResolution);
            AddButtonRow("Back", Close);

            // ilustrasi kanan
            artFrame = Add(Root, "op-art-frame");
            artFrame.Add(new SlimeMusicArt());
            mood = MakeLabel("", "op-mood");
            Root.Add(mood);
            Root.Add(MakeLabel(Application.isEditor ? "Screen settings apply in the build only." : "", "op-note"));
            Root.schedule.Execute(UpdateMood).Every(200);
        }

        private static VisualElement Add(VisualElement parent, string cls)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList(cls);
            parent.Add(e);
            return e;
        }

        private static Label MakeLabel(string text, string cls)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.AddToClassList(cls);
            return l;
        }

        public void Open()
        {
            resolutions = GameSettings.AvailableResolutions();
            IsOpen = true;
            sel = 0;
            Root.RemoveFromClassList("mm-hidden");
            Refresh();
            UiFx.StaggerIn(list.Children());
            UiFx.SlideIn(artFrame, new Vector2(60, 0), 0.22f, 0.08f);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Root.AddToClassList("mm-hidden");
            onBack?.Invoke();
        }

        // Dipanggil tiap frame oleh pemilik panel selama panel terbuka.
        public void HandleInput()
        {
            if (!IsOpen) return;
            // tahan W/S/A/D = ulang otomatis, roda mouse = naik/turun (MenuInput)
            bool up = MenuInput.Up, down = MenuInput.Down, left = MenuInput.Left, right = MenuInput.Right;
            bool ok = MenuInput.Ok, back = MenuInput.Back;

            if (up) sel = (sel - 1 + rows.Count) % rows.Count;
            if (down) sel = (sel + 1) % rows.Count;
            var row = rows[sel];
            // tahan A/D hanya berulang di bar volume; toggle & resolusi cukup sekali per tekan
            if (!row.repeats && (left || right) && !MenuInputPressedThisFrame()) { left = right = false; }
            if (left) row.change?.Invoke(-1);
            if (right) row.change?.Invoke(1);
            if (ok) { if (row.activate != null) row.activate(); else row.change?.Invoke(1); }
            if (back) { Close(); return; }
            if (up || down || left || right || ok) Refresh();
        }

        // ---------- baris ----------

        private Row MakeRow(string label)
        {
            var el = new VisualElement();
            el.AddToClassList("mm-item");
            el.AddToClassList("op-row");
            el.style.marginLeft = RowOffsets[rows.Count % RowOffsets.Length];
            var caret = new VisualElement { pickingMode = PickingMode.Ignore };
            caret.AddToClassList("mm-caret");
            el.Add(caret);
            var wrap = Add(el, "mm-item-wrap");
            wrap.AddToClassList("op-name");
            Add(wrap, "mm-slash-back");
            Add(wrap, "mm-slash");
            wrap.Add(MakeLabel(label.ToUpperInvariant(), "mm-item-label"));
            list.Add(el);

            var row = new Row { element = el };
            rows.Add(row);
            el.RegisterCallback<PointerEnterEvent>(_ => { sel = rows.IndexOf(row); Refresh(); });
            el.RegisterCallback<ClickEvent>(_ => { if (row.activate != null) { row.activate(); Refresh(); } });
            return row;
        }

        private VisualElement Arrow(string glyph, Action onClick)
        {
            var a = new Label(glyph);
            a.AddToClassList("op-arrow");
            a.RegisterCallback<ClickEvent>(e => { onClick(); Refresh(); e.StopPropagation(); });
            return a;
        }

        private void AddBarRow(string label, Func<int> get, Action<int> set)
        {
            var row = MakeRow(label);
            var box = Add(row.element, "op-value-box");
            box.pickingMode = PickingMode.Position;
            box.Add(Arrow("<", () => set(get() - 1)));
            var bar = Add(box, "op-bar");
            bar.pickingMode = PickingMode.Position;
            var blocks = new List<VisualElement>();
            for (int i = 0; i < GameSettings.MaxLevel; i++)
            {
                var b = new VisualElement { pickingMode = PickingMode.Ignore };
                b.AddToClassList("op-block");
                bar.Add(b);
                blocks.Add(b);
            }
            // klik / drag di bar = set volume sesuai posisi mouse (kiri ujung = 0)
            bool dragging = false;
            void SetFromPointer(Vector2 local)
            {
                float w = Mathf.Max(1f, bar.resolvedStyle.width);
                int v = Mathf.Clamp(Mathf.RoundToInt(local.x / w * GameSettings.MaxLevel), 0, GameSettings.MaxLevel);
                if (v != get()) { set(v); Refresh(); }
            }
            bar.RegisterCallback<PointerDownEvent>(e =>
            {
                dragging = true;
                bar.CapturePointer(e.pointerId);
                sel = rows.IndexOf(row);
                SetFromPointer(e.localPosition);
                e.StopPropagation();
            });
            bar.RegisterCallback<PointerMoveEvent>(e => { if (dragging) SetFromPointer(e.localPosition); });
            bar.RegisterCallback<PointerUpEvent>(e => { dragging = false; bar.ReleasePointer(e.pointerId); e.StopPropagation(); });
            bar.RegisterCallback<PointerCaptureOutEvent>(_ => dragging = false);
            box.Add(Arrow(">", () => set(get() + 1)));
            var pct = MakeLabel("", "op-pct");
            box.Add(pct);
            row.change = d => set(get() + d);
            row.repeats = true;
            row.refresh = () =>
            {
                int v = get();
                for (int i = 0; i < blocks.Count; i++) blocks[i].EnableInClassList("op-block--on", i < v);
                pct.text = v <= 0 ? "OFF" : v >= GameSettings.MaxLevel ? "MAX" : (v * 100 / GameSettings.MaxLevel) + "%";
                pct.EnableInClassList("op-pct--off", v <= 0);
            };
        }

        private void AddToggleRow(string label, Func<bool> get, Action<bool> set)
        {
            var row = MakeRow(label);
            var box = Add(row.element, "op-value-box");
            box.pickingMode = PickingMode.Position;
            var off = new Label("OFF"); off.AddToClassList("op-pill");
            var on = new Label("ON"); on.AddToClassList("op-pill");
            off.RegisterCallback<ClickEvent>(e => { set(false); Refresh(); e.StopPropagation(); });
            on.RegisterCallback<ClickEvent>(e => { set(true); Refresh(); e.StopPropagation(); });
            box.Add(off); box.Add(on);
            row.change = _ => set(!get());
            row.refresh = () =>
            {
                bool v = get();
                on.EnableInClassList("op-pill--on", v);
                off.EnableInClassList("op-pill--on", !v);
            };
        }

        private void AddPickRow(string label, Func<string> text, Action<int> step)
        {
            var row = MakeRow(label);
            var box = Add(row.element, "op-value-box");
            box.pickingMode = PickingMode.Position;
            box.Add(Arrow("<", () => step(-1)));
            var value = MakeLabel("", "op-value");
            box.Add(value);
            box.Add(Arrow(">", () => step(1)));
            row.change = step;
            row.refresh = () => value.text = text();
        }

        private void AddButtonRow(string label, Action activate)
        {
            var row = MakeRow(label);
            row.activate = activate;
            row.element.AddToClassList("op-row--button");
        }

        private string ResolutionText()
        {
            var r = GameSettings.Resolution;
            return r.x + " x " + r.y;
        }

        private void StepResolution(int d)
        {
            if (resolutions == null || resolutions.Count == 0) return;
            int i = resolutions.IndexOf(GameSettings.Resolution);
            if (i < 0) i = resolutions.Count - 1;
            i = Mathf.Clamp(i + d, 0, resolutions.Count - 1);
            GameSettings.Resolution = resolutions[i];
        }

        private void UpdateMood()
        {
            int m = GameSettings.MusicLevel;
            mood.text = m <= 0 ? "zzz... MUSIC OFF" : m <= 3 ? "CHILLING" : m <= 7 ? "BOPPING" : m < GameSettings.MaxLevel ? "DANCING" : "PARTY MODE!";
            mood.EnableInClassList("op-mood--party", m >= GameSettings.MaxLevel);
            mood.EnableInClassList("op-mood--off", m <= 0);
        }

        private void Refresh()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].element.EnableInClassList("mm-item--selected", i == sel);
                rows[i].refresh?.Invoke();
            }
            UpdateMood();
        }
    }
}
