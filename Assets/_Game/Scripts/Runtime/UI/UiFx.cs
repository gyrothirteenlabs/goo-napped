using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GooNapped
{
    // Transisi antar menu (storyboard di Figma "Menu Flow"). Semua pakai waktu nyata (jalan saat Time.timeScale = 0)
    // dan gerak dibulatkan ke 2px supaya tetap terasa pixel. Semua lapisan transisi pakai font Pixelify (kelas fx-layer).
    //   SlashWipe   : lembar miring menutup layar lalu ditarik ke kanan (pindah layar di menu)
    //   GooDrip     : lendir menetes menutup layar + kartu judul, lalu callback (masuk level / retry)
    //   SlashCover  : lembar miring menutup layar lalu callback (balik ke main menu)
    //   RevealIfPending : dipanggil scene baru (HUD level / main menu) untuk "membuka" layar dengan gaya yang sama
    //   Stamp       : kartu muncul 115% miring lalu "dicap" ke tempatnya + getar kecil (popup konfirmasi)
    //   StaggerIn   : baris masuk dari kiri bergiliran
    public static class UiFx
    {
        public enum Reveal { None, Goo, Slash }
        // Diisi sebelum pindah scene; scene berikutnya membuka layar dengan gaya ini.
        public static Reveal PendingReveal = Reveal.None;

        private static float Now => Time.realtimeSinceStartup;
        private static float Snap(float v) => Mathf.Round(v / 2f) * 2f;
        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);
        private static float EaseIn(float t) => t * t * t;

        // Jalankan step(0..1) selama 'duration' detik, lalu done. Dijadwalkan di elemen owner.
        public static void Tween(VisualElement owner, float duration, Action<float> step, Action done = null, float delay = 0f)
        {
            float start = Now + delay;
            bool finished = false;
            IVisualElementScheduledItem item = null;
            item = owner.schedule.Execute(() =>
            {
                if (finished || Now < start) return;
                float t = duration <= 0f ? 1f : Mathf.Clamp01((Now - start) / duration);
                step(t);
                if (t >= 1f) { finished = true; item?.Pause(); done?.Invoke(); }
            }).Every(16);
        }

        private static VisualElement Layer(VisualElement root, string name)
        {
            var el = new VisualElement { name = name, pickingMode = PickingMode.Position };   // menahan klik selama transisi
            el.AddToClassList("fx-layer");
            el.style.position = Position.Absolute;
            el.style.left = 0; el.style.top = 0; el.style.right = 0; el.style.bottom = 0;
            el.style.overflow = Overflow.Hidden;
            root.Add(el);
            return el;
        }

        private static VisualElement Box(VisualElement parent, string cls)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList(cls);
            parent.Add(e);
            return e;
        }

        private static float Width(VisualElement root) => Mathf.Max(960f, root.resolvedStyle.width);
        private static float Height(VisualElement root) => Mathf.Max(540f, root.resolvedStyle.height);

        // ---------- slash ----------
        private static VisualElement SlashSheet(VisualElement layer)
        {
            var sheet = Box(layer, "fx-slash-sheet");
            Box(sheet, "fx-slash-line");
            Box(sheet, "fx-slash-edge");
            return sheet;
        }

        public static void SlashWipe(VisualElement root, Action covered, Action done = null, bool reverse = false)
        {
            var layer = Layer(root, "fx-slash");
            var sheet = SlashSheet(layer);
            float w = Width(root);
            float dir = reverse ? -1f : 1f;
            float inTime = reverse ? 0.12f : 0.18f, outTime = reverse ? 0.15f : 0.22f;
            Tween(layer, inTime, t => sheet.style.translate = new Translate(Snap(-dir * w * 1.6f * (1f - EaseOut(t))), 0), () =>
            {
                covered?.Invoke();
                Tween(layer, outTime, t => sheet.style.translate = new Translate(Snap(dir * w * 1.6f * EaseIn(t)), 0), () =>
                {
                    layer.RemoveFromHierarchy();
                    done?.Invoke();
                });
            });
        }

        // Tutup layar dengan lembar miring lalu panggil 'covered' (pindah scene). Scene tujuan membuka dengan RevealIfPending.
        public static void SlashCover(VisualElement root, Action covered)
        {
            var layer = Layer(root, "fx-slash");
            var sheet = SlashSheet(layer);
            float w = Width(root);
            Tween(layer, 0.22f, t => sheet.style.translate = new Translate(Snap(-w * 1.6f * (1f - EaseOut(t))), 0), () =>
            {
                PendingReveal = Reveal.Slash;
                Tween(layer, 0.06f, _ => { }, covered);
            });
        }

        // ---------- goo drip ----------
        // Kartu judul yang muncul setelah layar tertutup lendir.
        public struct TitleCard
        {
            public string kicker;   // teks kecil di atas, mis. "LEVEL" / "TRY AGAIN"
            public string number;   // "08" (kosong = tanpa nomor, mis. prolog)
            public string title;    // "DINING HALL"
            public string area;     // "UPPER CASTLE"
            public string info;     // "RESCUE  ·  1 FRIEND INSIDE"
        }

        // Label dengan bayangan pixel padat (salinan teks digeser), bukan text-shadow, supaya tepinya tetap tajam.
        private static VisualElement SolidText(VisualElement parent, string text, string cls, string shadowCls)
        {
            var box = Box(parent, "fx-solid");
            var sh = new Label(text) { pickingMode = PickingMode.Ignore };
            sh.AddToClassList(cls); sh.AddToClassList(shadowCls);
            var fg = new Label(text) { pickingMode = PickingMode.Ignore };
            fg.AddToClassList(cls);
            box.Add(sh); box.Add(fg);
            return box;
        }

        private static List<(VisualElement el, float delay, float speed)> Drops(VisualElement layer, float w, int seed)
        {
            const int cols = 40;
            float cw = Mathf.Ceil(w / cols);
            var drops = new List<(VisualElement, float, float)>();
            var rng = new System.Random(seed);
            for (int c = 0; c < cols; c++)
            {
                var d = Box(layer, "fx-goo-drop");
                d.style.left = c * cw; d.style.width = cw + 1;
                Box(d, "fx-goo-head");   // tetes membulat di ujung
                if (c % 3 == 0) Box(d, "fx-goo-shine");
                drops.Add((d, (float)rng.NextDouble() * 0.25f, 0.8f + (float)rng.NextDouble() * 0.5f));
            }
            return drops;
        }

        public static void GooDrip(VisualElement root, TitleCard card, Action done)
        {
            var layer = Layer(root, "fx-goo");
            float w = Width(root), h = Height(root);
            var drops = Drops(layer, w, 7);
            bool hasCard = !string.IsNullOrEmpty(card.title) || !string.IsNullOrEmpty(card.number);

            // ---- kartu judul ----
            var cardRoot = Box(layer, "fx-card");
            var band = Box(cardRoot, "fx-card-band");
            Box(band, "fx-card-band-line");
            Box(band, "fx-card-band-edge");
            var rowEl = Box(cardRoot, "fx-card-row");
            VisualElement num = null;
            var glitchCopies = new List<Label>();
            if (!string.IsNullOrEmpty(card.number))
            {
                var numWrap = Box(rowEl, "fx-card-numwrap");
                foreach (var cls in new[] { "fx-card-num--red", "fx-card-num--teal" })
                {
                    var g = new Label(card.number) { pickingMode = PickingMode.Ignore };
                    g.AddToClassList("fx-card-num"); g.AddToClassList(cls);
                    numWrap.Add(g);
                    glitchCopies.Add(g);
                }
                num = SolidText(numWrap, card.number, "fx-card-num", "fx-card-num--shadow");
            }
            var right = Box(rowEl, "fx-card-right");
            Label kicker = null;
            if (!string.IsNullOrEmpty(card.kicker)) { kicker = new Label(card.kicker) { pickingMode = PickingMode.Ignore }; kicker.AddToClassList("fx-card-kicker"); right.Add(kicker); }
            string fullTitle = card.title ?? "";
            var titleBox = SolidText(right, "", "fx-card-title", "fx-card-title--shadow");
            var titleLabels = titleBox.Query<Label>().ToList();
            Label area = null, info = null;
            if (!string.IsNullOrEmpty(card.area)) { area = new Label(card.area) { pickingMode = PickingMode.Ignore }; area.AddToClassList("fx-card-area"); right.Add(area); }
            if (!string.IsNullOrEmpty(card.info)) { info = new Label(card.info) { pickingMode = PickingMode.Ignore }; info.AddToClassList("fx-card-info"); cardRoot.Add(info); }
            cardRoot.style.opacity = 0;
            band.style.scale = new Scale(new Vector2(0f, 1f));
            if (num != null) num.style.opacity = 0;
            foreach (var g in glitchCopies) g.style.opacity = 0;
            if (kicker != null) kicker.style.opacity = 0;
            if (area != null) area.style.opacity = 0;
            if (info != null) info.style.opacity = 0;

            void Finish()
            {
                // keluar: kartu menyusut ke garis lalu lanjut (layar tetap tertutup lendir sampai scene baru membuka)
                Tween(cardRoot, 0.14f, t =>
                {
                    cardRoot.style.scale = new Scale(new Vector2(1f + t * 0.08f, 1f - EaseIn(t)));
                    cardRoot.style.opacity = 1f - t * 0.6f;
                }, () =>
                {
                    PendingReveal = Reveal.Goo;
                    done?.Invoke();
                });
            }

            void ShowCard()
            {
                if (!hasCard) { Tween(layer, 0.1f, _ => { }, Finish); return; }
                cardRoot.style.opacity = 1;
                // 1) pita menyapu dari kiri
                Tween(cardRoot, 0.16f, t => band.style.scale = new Scale(new Vector2(EaseOut(t), 1f)));
                if (kicker != null) Tween(kicker, 0.1f, t => kicker.style.opacity = t, null, 0.08f);
                // 2) nomor dicap + glitch merah/teal sebentar + kartu bergetar
                if (num != null)
                {
                    Tween(num, 0.12f, t =>
                    {
                        float s = Mathf.Lerp(1.7f, 1f, EaseIn(t));
                        num.style.scale = new Scale(new Vector2(s, s));
                        num.style.opacity = Mathf.Clamp01(t * 2f);
                    }, null, 0.1f);
                    Tween(cardRoot, 0.22f, t =>
                    {
                        float k = 1f - t;
                        float jx = (UnityEngine.Random.value - 0.5f) * 10f * k, jy = (UnityEngine.Random.value - 0.5f) * 6f * k;
                        cardRoot.style.translate = new Translate(Snap(jx), Snap(jy));
                        glitchCopies[0].style.opacity = k; glitchCopies[1].style.opacity = k;
                        glitchCopies[0].style.translate = new Translate(Snap(-8f * k), 0);
                        glitchCopies[1].style.translate = new Translate(Snap(8f * k), Snap(2f * k));
                    }, () => cardRoot.style.translate = StyleKeyword.Null, 0.22f);
                }
                // 3) nama diketik per huruf (huruf terakhir sempat "acak" sekilas)
                const string scramble = "#%&@$?!/<>=+*";
                float perChar = 0.035f;
                Tween(cardRoot, Mathf.Max(0.05f, fullTitle.Length * perChar), t =>
                {
                    int n = Mathf.Clamp(Mathf.CeilToInt(t * fullTitle.Length), 0, fullTitle.Length);
                    string s = fullTitle.Substring(0, n);
                    if (n < fullTitle.Length && n > 0 && fullTitle[n] != ' ') s += scramble[UnityEngine.Random.Range(0, scramble.Length)];
                    foreach (var l in titleLabels) l.text = s;
                }, () => { foreach (var l in titleLabels) l.text = fullTitle; }, 0.18f);
                // 4) area + info
                float after = 0.2f + fullTitle.Length * perChar;
                if (area != null) { SlideIn(area, new Vector2(-24, 0), 0.14f, after); Tween(area, 0.12f, t => area.style.opacity = t, null, after); }
                if (info != null) Tween(info, 0.15f, t => info.style.opacity = t, null, after + 0.08f);
                // 5) tahan sambil mendorong pelan, lalu keluar
                float hold = 0.8f;
                Tween(rowEl, after + hold, t => rowEl.style.scale = new Scale(new Vector2(1f + t * 0.03f, 1f + t * 0.03f)));
                Tween(cardRoot, 0f, _ => { }, Finish, after + hold);
            }

            Tween(layer, 0.6f, t =>
            {
                foreach (var (el, delay, speed) in drops)
                {
                    float k = Mathf.Clamp01((t - delay) / (1f - delay) * speed * 1.3f);
                    el.style.height = Snap(EaseIn(k) * h * 1.15f + k * 24f);
                }
            }, ShowCard);
        }

        // ---------- buka layar di scene baru ----------
        public static void RevealIfPending(VisualElement root)
        {
            var mode = PendingReveal;
            PendingReveal = Reveal.None;
            if (mode == Reveal.None || root == null) return;
            var layer = Layer(root, "fx-reveal");
            float w = Width(root), h = Height(root);
            if (mode == Reveal.Goo)
            {
                // lendir menutup penuh lalu surut ke atas, kolom demi kolom
                var drops = Drops(layer, w, 11);
                foreach (var (el, _, _) in drops) el.style.height = h * 1.2f;
                Tween(layer, 0.55f, t =>
                {
                    foreach (var (el, delay, speed) in drops)
                    {
                        float k = Mathf.Clamp01((t - delay) / (1f - delay) * speed * 1.3f);
                        el.style.height = Snap((1f - EaseIn(k)) * h * 1.2f);
                    }
                }, layer.RemoveFromHierarchy, 0.05f);
            }
            else
            {
                var sheet = SlashSheet(layer);
                Tween(layer, 0.24f, t => sheet.style.translate = new Translate(Snap(w * 1.6f * EaseIn(t)), 0), layer.RemoveFromHierarchy, 0.05f);
            }
        }

        // ---------- stamp ----------
        public static void Stamp(VisualElement card, float restAngle = -2f)
        {
            card.style.scale = new Scale(new Vector2(1.15f, 1.15f));
            card.style.rotate = new Rotate(-6f);
            card.style.opacity = 0;
            Tween(card, 0.12f, t =>
            {
                float s = Mathf.Lerp(1.15f, 1f, EaseIn(t));
                card.style.scale = new Scale(new Vector2(s, s));
                card.style.rotate = new Rotate(Mathf.Lerp(-6f, restAngle, t));
                card.style.opacity = Mathf.Clamp01(t * 3f);
            }, () =>
                Tween(card, 0.08f, t => card.style.translate = new Translate(t < 1f ? (t < 0.5f ? 2f : -2f) : 0f, 0), () =>
                    card.style.translate = StyleKeyword.Null));
        }

        // ---------- baris masuk bergiliran ----------
        public static void StaggerIn(IEnumerable<VisualElement> items, float step = 0.04f, float from = -40f)
        {
            int i = 0;
            foreach (var el in items)
            {
                var e = el;
                e.style.opacity = 0;
                e.style.translate = new Translate(from, 0);
                Tween(e, 0.16f, t =>
                {
                    e.style.opacity = t;
                    e.style.translate = new Translate(Snap(from * (1f - EaseOut(t))), 0);
                }, () => { e.style.translate = StyleKeyword.Null; e.style.opacity = StyleKeyword.Null; }, i * step);
                i++;
            }
        }

        // Geser elemen masuk dari offset (x,y) ke posisi asal.
        public static void SlideIn(VisualElement el, Vector2 from, float duration, float delay = 0f)
        {
            el.style.translate = new Translate(from.x, from.y);
            Tween(el, duration, t =>
            {
                float k = 1f - EaseOut(t);
                el.style.translate = new Translate(Snap(from.x * k), Snap(from.y * k));
            }, () => el.style.translate = StyleKeyword.Null, delay);
        }

        public static void Fade(VisualElement el, float from, float to, float duration, Action done = null)
        {
            el.style.opacity = from;
            Tween(el, duration, t => el.style.opacity = Mathf.Lerp(from, to, t), done);
        }

        // Kartu judul untuk scene level (dipakai main menu dan retry).
        public static TitleCard CardForLevel(string scene, string kicker = "LEVEL")
        {
            var card = new TitleCard();
            int i = GameFlow.IndexOf(scene);
            if (i >= 0)
            {
                var info = LevelCatalog.Get(i);
                int n = info.friends.Length, left = n - LevelCatalog.SavedCount(i);
                card.kicker = kicker;
                card.number = (i + 1).ToString("00");
                card.title = info.name.ToUpperInvariant();
                card.area = LevelCatalog.Area(i).ToUpperInvariant();
                card.info = !info.rescue || n == 0 ? "SNEAK THROUGH  ·  REACH THE VENT"
                    : left <= 0 ? "RESCUE  ·  EVERYONE IS SAFE"
                    : "RESCUE  ·  " + left + (left == 1 ? " FRIEND" : " FRIENDS") + " INSIDE";
            }
            else if (scene == GameFlow.Intro) card.title = "PROLOGUE";
            else if (scene == GameFlow.Tutorial) { card.kicker = "LEVEL"; card.number = "00"; card.title = "TUTORIAL"; card.area = "DUNGEON"; card.info = "LEARN TO SNEAK, SPLIT AND RESCUE"; }
            return card;
        }
    }
}
