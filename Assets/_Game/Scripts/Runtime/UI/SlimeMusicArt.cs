using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GooNapped
{
    // Ilustrasi di panel Options: slime dengan headphone yang bereaksi ke volume musik (dan suara).
    // PLACEHOLDER dibangun dari kotak pixel lewat kode; ganti dengan sprite/animasi dari tim desainer kalau sudah ada.
    //   Musik 0      : tidur (mata tertutup, "Z" melayang, headphone melorot, warna pudar)
    //   Musik 1-3    : santai (goyang pelan, not musik jarang)
    //   Musik 4-7    : manggut-manggut ikut ketukan
    //   Musik 8-9    : joget (squash & stretch, not makin banyak, speaker berdenyut)
    //   Musik 10 MAX : pesta (lompat tinggi, mata ^ ^, lampu warna-warni, speaker meledak)
    //   Suara (SFX)  : ikon speaker kecil di pojok, jumlah gelombang = level; 0 = tanda silang
    public class SlimeMusicArt : VisualElement
    {
        private const int Cell = 6;
        private static readonly string[] Body =
        {
            "....######....",
            "..##########..",
            ".############.",
            ".#hh#########.",
            "##h###########",
            "##############",
            "##############",
            "##############",
            ".############.",
            "..##########..",
        };

        private static readonly Color Ink = new Color32(11, 15, 26, 255);
        private static readonly Color Goo = new Color32(63, 209, 199, 255);
        private static readonly Color GooSleep = new Color32(74, 122, 130, 255);
        private static readonly Color Shine = new Color32(191, 240, 226, 255);
        private static readonly Color Cup = new Color32(194, 37, 58, 255);
        private static readonly Color[] NoteColors =
        {
            new Color32(255, 200, 97, 255), new Color32(255, 90, 95, 255), new Color32(122, 82, 199, 255),
            new Color32(63, 209, 199, 255), new Color32(255, 240, 176, 255),
        };

        private readonly VisualElement stage, slime, bodyRoot, phones, shadow, glow, speakerL, speakerR, sfxIcon, sfxX;
        private readonly List<VisualElement> bodyCells = new List<VisualElement>();
        private readonly List<VisualElement> eyesOpen = new List<VisualElement>(), eyesClosed = new List<VisualElement>(), eyesHappy = new List<VisualElement>();
        private readonly List<VisualElement> sfxWaves = new List<VisualElement>();
        private readonly List<VisualElement> coneL = new List<VisualElement>(), coneR = new List<VisualElement>();

        private class Particle { public VisualElement el; public float born, life; public Vector2 from, vel; public float spin; }
        private readonly List<Particle> particles = new List<Particle>();
        private float nextNote, nextZ;
        private readonly System.Random rng = new System.Random(3);

        public SlimeMusicArt()
        {
            AddToClassList("art-root");
            pickingMode = PickingMode.Ignore;

            glow = Box(this, "art-glow", 0, 0, 0, 0);
            glow.style.right = 0; glow.style.bottom = 0;
            stage = new VisualElement { pickingMode = PickingMode.Ignore };
            stage.AddToClassList("art-stage");
            Add(stage);

            // lantai + speaker kiri/kanan
            Box(stage, "art-floor", 0, 168, 320, 4);
            speakerL = Speaker(18, coneL);
            speakerR = Speaker(254, coneR);

            // bayangan + slime
            shadow = Box(stage, "art-shadow", 118, 162, 84, 6);
            slime = new VisualElement { pickingMode = PickingMode.Ignore };
            slime.AddToClassList("art-slime");
            slime.style.left = 160 - 7 * Cell; slime.style.top = 168 - 10 * Cell;
            slime.style.width = 14 * Cell; slime.style.height = 10 * Cell;
            slime.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));   // squash dari bawah
            stage.Add(slime);
            bodyRoot = new VisualElement { pickingMode = PickingMode.Ignore };
            bodyRoot.style.position = Position.Absolute; bodyRoot.style.left = 0; bodyRoot.style.top = 0;
            slime.Add(bodyRoot);
            for (int y = 0; y < Body.Length; y++)
                for (int x = 0; x < Body[y].Length; x++)
                {
                    char c = Body[y][x];
                    if (c == '.') continue;
                    var cell = Box(bodyRoot, "art-px", x * Cell, y * Cell, Cell, Cell);
                    cell.style.backgroundColor = c == 'h' ? Shine : Goo;
                    if (c == '#') bodyCells.Add(cell);
                }
            // mata
            foreach (int ex in new[] { 4, 9 })
            {
                var open = Box(slime, "art-eye", ex * Cell, 4 * Cell, Cell, Cell * 2);
                Box(open, "art-eye-shine", 0, 0, 2, 2).style.backgroundColor = Color.white;
                eyesOpen.Add(open);
                eyesClosed.Add(Box(slime, "art-eye", ex * Cell - 2, 5 * Cell + 2, Cell + 4, 2));
                var happy = new VisualElement { pickingMode = PickingMode.Ignore };
                happy.style.position = Position.Absolute; happy.style.left = ex * Cell - 3; happy.style.top = 4 * Cell + 4;
                var a = Box(happy, "art-eye", 0, 4, 8, 3); a.style.rotate = new Rotate(-35);
                var b = Box(happy, "art-eye", 6, 4, 8, 3); b.style.rotate = new Rotate(35);
                slime.Add(happy);
                eyesHappy.Add(happy);
            }
            Box(slime, "art-mouth", 6 * Cell, 7 * Cell, Cell * 2, 2);
            // headphone: bando + 2 cup
            phones = new VisualElement { pickingMode = PickingMode.Ignore };
            phones.style.position = Position.Absolute; phones.style.left = 0; phones.style.top = 0;
            slime.Add(phones);
            Box(phones, "art-band", 2 * Cell, -Cell - 2, 10 * Cell, 4);
            Box(phones, "art-band", Cell, -2, 4, Cell * 2);
            Box(phones, "art-band", 13 * Cell - 4, -2, 4, Cell * 2);
            Box(phones, "art-cup", -4, 3 * Cell, Cell + 4, Cell * 3).style.backgroundColor = Cup;
            Box(phones, "art-cup", 13 * Cell, 3 * Cell, Cell + 4, Cell * 3).style.backgroundColor = Cup;

            // ikon suara (SFX) pojok kanan atas
            sfxIcon = new VisualElement { pickingMode = PickingMode.Ignore };
            sfxIcon.style.position = Position.Absolute; sfxIcon.style.right = 10; sfxIcon.style.top = 10;
            sfxIcon.style.width = 40; sfxIcon.style.height = 20;
            Add(sfxIcon);
            Box(sfxIcon, "art-sfx", 0, 6, 6, 8);
            Box(sfxIcon, "art-sfx", 6, 2, 4, 16);
            for (int i = 0; i < 3; i++) sfxWaves.Add(Box(sfxIcon, "art-sfx-wave", 14 + i * 7, 6 - i * 3, 3, 8 + i * 6));
            sfxX = new VisualElement { pickingMode = PickingMode.Ignore };
            sfxX.style.position = Position.Absolute; sfxX.style.left = 14; sfxX.style.top = 2;
            var x1 = Box(sfxX, "art-sfx-x", 0, 7, 16, 3); x1.style.rotate = new Rotate(45);
            var x2 = Box(sfxX, "art-sfx-x", 0, 7, 16, 3); x2.style.rotate = new Rotate(-45);
            sfxIcon.Add(sfxX);

            schedule.Execute(Tick).Every(16);
        }

        private VisualElement Speaker(float x, List<VisualElement> cones)
        {
            var s = Box(stage, "art-speaker", x, 92, 48, 76);
            cones.Add(Box(s, "art-cone", 14, 10, 20, 20));
            cones.Add(Box(s, "art-cone", 10, 38, 28, 28));
            return s;
        }

        private static VisualElement Box(VisualElement parent, string cls, float x, float y, float w, float h)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList(cls);
            e.style.position = Position.Absolute;
            e.style.left = x; e.style.top = y;
            if (w > 0) e.style.width = w;
            if (h > 0) e.style.height = h;
            parent.Add(e);
            return e;
        }

        private static float Snap(float v) => Mathf.Round(v / 2f) * 2f;

        private void Tick()
        {
            if (panel == null || resolvedStyle.display == DisplayStyle.None) return;
            float t = Time.realtimeSinceStartup;
            int music = GameSettings.MusicLevel, max = GameSettings.MaxLevel;
            int sfx = GameSettings.SfxLevel;
            bool sleep = music <= 0, party = music >= max;
            float bpm = 70f + music * 8f;
            float beat = t * bpm / 60f;
            float pulse = Mathf.Abs(Mathf.Sin(beat * Mathf.PI));   // 0..1 tiap ketukan

            // ---- gerak slime ----
            float y = 0, sx = 1, sy = 1, rot = 0;
            if (sleep)
            {
                float br = Mathf.Sin(t * 1.6f) * 0.5f + 0.5f;   // napas pelan
                sy = 0.94f + br * 0.05f; sx = 1.04f - br * 0.03f;
            }
            else if (music <= 3) { rot = Mathf.Sin(t * 1.5f) * 4f; y = -pulse * 2f; }
            else if (music <= 7) { y = -pulse * 6f; sy = 1f - (1f - pulse) * 0.06f; sx = 1f + (1f - pulse) * 0.05f; rot = Mathf.Sin(beat * Mathf.PI * 0.5f) * 3f; }
            else if (!party) { y = -pulse * 12f; sy = 0.86f + pulse * 0.24f; sx = 1.12f - pulse * 0.18f; rot = Mathf.Sin(beat * Mathf.PI * 0.5f) * 8f; }
            else
            {
                float hop = Mathf.Abs(Mathf.Sin(beat * Mathf.PI * 0.5f));
                y = -hop * 26f; sy = 0.8f + hop * 0.35f; sx = 1.18f - hop * 0.25f; rot = Mathf.Sin(beat * Mathf.PI) * 10f;
            }
            slime.style.translate = new Translate(0, Snap(y));
            slime.style.scale = new Scale(new Vector2(sx, sy));
            slime.style.rotate = new Rotate(rot);
            float lift = Mathf.Clamp01(-y / 26f);
            shadow.style.scale = new Scale(new Vector2(1f - lift * 0.4f, 1f));
            shadow.style.opacity = 0.6f - lift * 0.3f;

            // ---- warna & mata ----
            var col = sleep ? GooSleep : Goo;
            foreach (var c in bodyCells) c.style.backgroundColor = col;
            SetShown(eyesOpen, !sleep && !party);
            SetShown(eyesClosed, sleep);
            SetShown(eyesHappy, party);
            phones.style.translate = new Translate(0, sleep ? 22 : 0);
            phones.style.rotate = new Rotate(sleep ? 8 : 0);

            // ---- speaker & lampu ----
            float cone = music >= 8 ? 1f + pulse * (party ? 0.35f : 0.18f) : music >= 4 ? 1f + pulse * 0.06f : 1f;
            foreach (var c in coneL) c.style.scale = new Scale(new Vector2(cone, cone));
            foreach (var c in coneR) c.style.scale = new Scale(new Vector2(cone, cone));
            speakerL.style.opacity = sleep ? 0.45f : 1f;
            speakerR.style.opacity = sleep ? 0.45f : 1f;
            if (party)
            {
                var gc = NoteColors[Mathf.FloorToInt(beat) % NoteColors.Length];
                glow.style.backgroundColor = new Color(gc.r, gc.g, gc.b, 0.12f + pulse * 0.18f);
            }
            else glow.style.backgroundColor = new Color(0, 0, 0, sleep ? 0.35f : 0f);

            // ---- partikel: not musik / Z ----
            if (sleep)
            {
                if (t >= nextZ) { SpawnZ(t); nextZ = t + 1.1f; }
            }
            else
            {
                float rate = music <= 3 ? 1.6f : music <= 7 ? 0.7f : party ? 0.16f : 0.32f;
                if (t >= nextNote) { SpawnNote(t, party); nextNote = t + rate * (0.7f + (float)rng.NextDouble() * 0.6f); }
            }
            UpdateParticles(t);

            // ---- ikon suara ----
            int waves = sfx <= 0 ? 0 : Mathf.CeilToInt(sfx / (float)max * 3f);
            for (int i = 0; i < sfxWaves.Count; i++) sfxWaves[i].style.display = i < waves ? DisplayStyle.Flex : DisplayStyle.None;
            sfxX.style.display = sfx <= 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void SetShown(List<VisualElement> list, bool on)
        {
            foreach (var e in list) e.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SpawnNote(float t, bool party)
        {
            var n = new VisualElement { pickingMode = PickingMode.Ignore };
            n.style.position = Position.Absolute;
            var c = NoteColors[rng.Next(NoteColors.Length)];
            Box(n, "art-note", 0, 10, 8, 6).style.backgroundColor = c;
            Box(n, "art-note", 6, 0, 2, 14).style.backgroundColor = c;
            Box(n, "art-note", 8, 0, 6, 2).style.backgroundColor = c;
            if (rng.Next(2) == 0)
            {
                Box(n, "art-note", 14, 12, 8, 6).style.backgroundColor = c;
                Box(n, "art-note", 20, 2, 2, 14).style.backgroundColor = c;
                Box(n, "art-note", 8, 0, 14, 2).style.backgroundColor = c;
            }
            stage.Add(n);
            float side = rng.Next(2) == 0 ? -1f : 1f;
            var from = new Vector2(160 + side * (20 + rng.Next(30)), 100);
            var vel = new Vector2(side * (10 + rng.Next(party ? 50 : 25)), -(40 + rng.Next(party ? 60 : 30)));
            particles.Add(new Particle { el = n, born = t, life = party ? 1.0f : 1.4f, from = from, vel = vel, spin = side * 30f });
        }

        private void SpawnZ(float t)
        {
            var z = new Label("Z") { pickingMode = PickingMode.Ignore };
            z.AddToClassList("art-z");
            z.style.position = Position.Absolute;
            stage.Add(z);
            particles.Add(new Particle { el = z, born = t, life = 2.2f, from = new Vector2(196, 96), vel = new Vector2(18, -30), spin = 0 });
        }

        private void UpdateParticles(float t)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                float age = (t - p.born) / p.life;
                if (age >= 1f) { p.el.RemoveFromHierarchy(); particles.RemoveAt(i); continue; }
                float sec = t - p.born;
                float wobble = p.el is Label ? Mathf.Sin(sec * 3f) * 6f : 0f;
                p.el.style.left = Snap(p.from.x + p.vel.x * sec + wobble);
                p.el.style.top = Snap(p.from.y + p.vel.y * sec);
                p.el.style.opacity = age < 0.15f ? age / 0.15f : 1f - Mathf.Clamp01((age - 0.6f) / 0.4f);
                p.el.style.rotate = new Rotate(p.spin * sec);
                if (p.el is Label) p.el.style.fontSize = 12 + age * 12f;
            }
        }
    }
}
