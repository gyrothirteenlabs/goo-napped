using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GooNapped.EditorTools
{
    // Suara PLACEHOLDER hasil sintesis (tanpa aset pihak ketiga, aman untuk repo publik).
    // Menu: GooNapped > Audio > Buat suara placeholder. Hasil: Assets/_Game/Audio/{SFX,Music}/proto_*.wav
    // Tim audio mengganti klip di komponen AudioDirector (prefab LevelSystems) dan lentera, tanpa ubah kode.
    public static class ProtoAudioGenerator
    {
        const int Rate = 44100;
        const string Sfx = "Assets/_Game/Audio/SFX/";
        const string Music = "Assets/_Game/Audio/Music/";

        [MenuItem("GooNapped/Audio/Buat suara placeholder")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(Sfx);
            Directory.CreateDirectory(Music);
            var rng = new System.Random(7);

            Write(Sfx + "proto_slime_step.wav", Synth(0.14f, (t, d) =>
                Noise(rng) * Env(t, 0.004f, 0.11f) * 0.35f * LowpassSweep(t, d) +
                Math.Sin(2 * Math.PI * (140 - 300 * t) * t) * Env(t, 0.002f, 0.09f) * 0.5f, smooth: 0.82f));

            Write(Sfx + "proto_golem_step.wav", Synth(0.35f, (t, d) =>
                Math.Sin(2 * Math.PI * (70 - 60 * t) * t) * Env(t, 0.003f, 0.3f) * 0.9f +
                Noise(rng) * Env(t, 0.001f, 0.05f) * 0.4f, smooth: 0.6f));

            Write(Sfx + "proto_eat.wav", Synth(0.45f, (t, d) =>
                Math.Sin(2 * Math.PI * (300 - 450 * t) * t) * Env(t, 0.01f, 0.18f) * 0.55f +
                Math.Sin(2 * Math.PI * (220 - 200 * (t - 0.22)) * t) * Env(t - 0.22, 0.01f, 0.2f) * 0.5f, smooth: 0.5f));

            Write(Sfx + "proto_split.wav", Synth(0.4f, (t, d) =>
                Math.Sin(2 * Math.PI * (180 + 600 * t * t) * t) * Env(t, 0.02f, 0.35f) * 0.55f +
                Noise(rng) * Env(t - 0.25, 0.002f, 0.08f) * 0.3f, smooth: 0.7f));

            Write(Sfx + "proto_merge.wav", Synth(0.35f, (t, d) =>
                Math.Sin(2 * Math.PI * (520 - 900 * t) * t) * Env(t, 0.005f, 0.3f) * 0.55f, smooth: 0.6f));

            Write(Sfx + "proto_disguise.wav", Synth(0.4f, (t, d) =>
                Noise(rng) * Env(t, 0.03f, 0.3f) * 0.45f + Math.Sin(2 * Math.PI * 160 * t) * Env(t, 0.05f, 0.25f) * 0.2f, smooth: 0.9f));

            Write(Sfx + "proto_reveal.wav", Synth(0.25f, (t, d) =>
                Math.Sin(2 * Math.PI * (200 + 500 * t) * t) * Env(t, 0.005f, 0.2f) * 0.45f, smooth: 0.6f));

            Write(Sfx + "proto_switch.wav", Synth(0.3f, (t, d) =>
                (Math.Sin(2 * Math.PI * 660 * t) * Env(t, 0.003f, 0.12f) + Math.Sin(2 * Math.PI * 880 * t) * Env(t - 0.08, 0.003f, 0.18f)) * 0.3f, smooth: 0.3f));

            Write(Sfx + "proto_alert.wav", Synth(0.5f, (t, d) =>
                (Math.Sin(2 * Math.PI * 330 * t) + 0.5 * Math.Sin(2 * Math.PI * 349 * t)) * Env(t, 0.01f, 0.45f) * 0.35f, smooth: 0.4f));

            Write(Sfx + "proto_caught.wav", Synth(1.0f, (t, d) =>
                (Math.Sin(2 * Math.PI * 220 * t) + Math.Sin(2 * Math.PI * 233 * t) + Math.Sin(2 * Math.PI * 110 * t)) * Env(t, 0.01f, 0.9f) * 0.25f, smooth: 0.3f));

            Write(Sfx + "proto_vomit.wav", Synth(0.5f, (t, d) =>
                Math.Sin(2 * Math.PI * (120 + 80 * Math.Sin(t * 40)) * t) * Env(t, 0.02f, 0.4f) * 0.5f +
                Noise(rng) * Env(t - 0.15, 0.01f, 0.2f) * 0.25f, smooth: 0.75f));

            Write(Sfx + "proto_splat.wav", Synth(0.3f, (t, d) =>
                Noise(rng) * Env(t, 0.001f, 0.12f) * 0.6f + Math.Sin(2 * Math.PI * (180 - 250 * t) * t) * Env(t, 0.002f, 0.1f) * 0.5f, smooth: 0.7f));

            Write(Sfx + "proto_ui_click.wav", Synth(0.08f, (t, d) =>
                Math.Sin(2 * Math.PI * 880 * t) * Env(t, 0.001f, 0.03f) * 0.4f, smooth: 0.2f));

            // obor: brown noise pelan + letupan acak (loop 4 detik)
            double brown = 0;
            Write(Sfx + "proto_torch_loop.wav", Synth(4f, (t, d) =>
            {
                brown = Math.Max(-1, Math.Min(1, brown + Noise(rng) * 0.02));
                double pop = rng.NextDouble() < 0.0009 ? Noise(rng) * 0.9 : 0;
                return brown * 0.45 + pop;
            }, smooth: 0.55f, loopFade: true));

            // sunyi gua: angin rendah yang naik-turun pelan (loop 10 detik)
            double b2 = 0;
            Write(Music + "proto_ambience_cave.wav", Synth(10f, (t, d) =>
            {
                b2 = Math.Max(-1, Math.Min(1, b2 + Noise(rng) * 0.01));
                double lfo = 0.6 + 0.4 * Math.Sin(2 * Math.PI * t / 10.0);
                return b2 * 0.6 * lfo;
            }, smooth: 0.96f, loopFade: true));

            // BGM: drone rendah + petikan minor jarang (A minor pentatonik), 70 bpm, loop 16 bar ~ 27 detik
            double[] notes = { 220, 261.6, 293.7, 329.6, 392, 440 };
            int[] seq = { 0, -1, 2, -1, 1, -1, -1, 3, 0, -1, 4, -1, 2, -1, -1, 5, 3, -1, 1, -1, 0, -1, -1, 2, 1, -1, 3, -1, 0, -1, -1, -1 };
            double beat = 60.0 / 70.0;
            float len = (float)(seq.Length * beat);
            Write(Music + "proto_bgm_stealth.wav", Synth(len, (t, d) =>
            {
                double drone = (Math.Sin(2 * Math.PI * 55 * t) + 0.5 * Math.Sin(2 * Math.PI * 82.4 * t)) * 0.18 * (0.8 + 0.2 * Math.Sin(2 * Math.PI * t / 8));
                int i = (int)(t / beat) % seq.Length;
                double tn = t - Math.Floor(t / beat) * beat;
                double pluck = 0;
                if (seq[i] >= 0)
                {
                    double f = notes[seq[i]];
                    pluck = (Math.Sin(2 * Math.PI * f * tn) + 0.3 * Math.Sin(4 * Math.PI * f * tn)) * Math.Exp(-tn * 4) * 0.22;
                }
                return drone + pluck;
            }, smooth: 0.35f, loopFade: true));

            AssetDatabase.Refresh();
            Debug.Log("Suara placeholder dibuat di Assets/_Game/Audio/");
        }

        static double Noise(System.Random r) => r.NextDouble() * 2 - 1;

        // amplop: naik selama a detik lalu turun eksponensial (waktu paruh ~ dec)
        static double Env(double t, double a, double dec)
        {
            if (t < 0) return 0;
            if (t < a) return t / a;
            return Math.Exp(-(t - a) / Math.Max(1e-4, dec) * 3);
        }

        static double LowpassSweep(double t, double d) => 1 - t / Math.Max(1e-4, d);

        // Sintesis + lowpass satu kutub sederhana (smooth 0 = tajam, 0.95 = sangat redup)
        static float[] Synth(float seconds, Func<double, double, double> f, float smooth = 0.5f, bool loopFade = false)
        {
            int n = (int)(seconds * Rate);
            var s = new float[n];
            double y = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / Rate;
                double x = f(t, seconds);
                y = smooth * y + (1 - smooth) * x;
                s[i] = (float)y;
            }
            // normalisasi ke -3 dB
            float peak = 1e-4f;
            foreach (var v in s) peak = Mathf.Max(peak, Mathf.Abs(v));
            float g = 0.7f / peak;
            for (int i = 0; i < n; i++) s[i] *= g;
            if (loopFade)
            {
                // crossfade ujung supaya loop tidak klik
                int fade = Mathf.Min(n / 8, Rate / 4);
                for (int i = 0; i < fade; i++)
                {
                    float k = (float)i / fade;
                    s[i] = s[i] * k + s[n - fade + i] * (1 - k);
                }
                Array.Resize(ref s, n - fade);
            }
            return s;
        }

        static void Write(string path, float[] samples)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int bytes = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(Rate);
                w.Write(Rate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(bytes);
                foreach (var v in samples) w.Write((short)Mathf.Clamp(v * 32767f, -32768f, 32767f));
            }
        }
    }
}
