using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Alur adegan final di puncak (desain-v2 bagian 2 no. 17 dan bagian 9). Teks placeholder bahasa Inggris (isi final: tim).
    //   1. Mulai: raja bicara dari toplesnya, perintah membalik katup aura.
    //   2. Tiap katup terbalik: komentar singkat. Semua terbalik: toples raja terbuka, Alkemis mengamuk (berburu lebih sering).
    //   3. Alkemis menerkam di dalam toples raja (KingJar.Seal): Segel Raja, peran tertukar, level selesai -> Ending.
    public class FinalSequence : MonoBehaviour
    {
        [Tooltip("Interval berburu Alkemis setelah semua katup terbalik (detik).")]
        [SerializeField] private float enragedHuntInterval = 3f;
        [SerializeField] private float enragedSpeedMultiplier = 1.25f;

        private readonly List<AuraValve> valves = new List<AuraValve>();
        private bool opened;

        public int Reversed { get { int n = 0; foreach (var v in valves) if (v != null && v.IsReversed) n++; return n; } }
        public int Total => valves.Count;

        private IEnumerator Start()
        {
            valves.AddRange(FindObjectsByType<AuraValve>(FindObjectsSortMode.None));
            foreach (var v in valves) v.Reversed += OnValve;
            yield return new WaitForSeconds(0.6f);
            if (Reversed == 0)
                StoryPlayer.Say("The King", "Little clear one... the Gloam drinks our auras through this machine. Turn its three valves back!");
            else if (Reversed >= Total) OpenJar(false);
        }

        private void OnValve(AuraValve v)
        {
            int n = Reversed;
            if (n < Total)
            {
                StoryPlayer.Say("The King", n == 1 ? "Yes! I can feel it flowing back. Two more." : "One more valve. Hurry, it knows you're here!");
                return;
            }
            OpenJar(true);
        }

        private void OpenJar(bool talk)
        {
            if (opened) return;
            opened = true;
            if (KingJar.Instance != null) KingJar.Instance.Open();
            foreach (var g in FindObjectsByType<Guard>(FindObjectsSortMode.None))
                if (g.IsBoss) g.Enrage(enragedHuntInterval, enragedSpeedMultiplier);
            if (talk)
                StoryPlayer.Say("The King", "My jar is open! The Gloam wants your clear body. Leave a piece of yourself inside and let it pounce.");
        }

        public void OnSealed(Guard boss)
        {
            StartCoroutine(Ending(boss));
        }

        private IEnumerator Ending(Guard boss)
        {
            if (boss != null) boss.gameObject.SetActive(false);
            AudioDirector.Play(AudioDirector.Sfx.Caught, KingJar.Instance.Center, 1f, 0f);
            StoryPlayer.Say("The King", "By the Royal Seal, I bind you in your own jar!");
            while (StoryPlayer.IsPlaying) yield return null;
            StoryPlayer.Say("Pip", "The jailer got jarred. Blip, you did it!");
            while (StoryPlayer.IsPlaying) yield return null;
            var goal = FindFirstObjectByType<LevelGoal>();
            if (goal != null) goal.CompleteFinal();
        }
    }
}
