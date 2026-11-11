using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Objektif level (desain-v2 bagian 3):
    //   Menyusup     : tanpa toples. Sampai ke exit (ventilasi tujuan) = selesai.
    //   Menyelamatkan: tiap teman ditukar dengan tiruan lalu dikawal sampai ventilasi/exit (Saved). Setelah semua
    //                  teman selamat, pemain masuk exit = selesai.
    // Gagal kalau pemain, potongan, atau teman tertangkap: ulang dari checkpoint (ventilasi terakhir).
    public class LevelGoal : MonoBehaviour
    {
        [Tooltip("Toples di level ini. Kosong = semua Jar di scene.")]
        [SerializeField] private List<Jar> jars = new List<Jar>();
        [Tooltip("Jumlah teman yang harus diselamatkan. 0 = semua toples.")]
        [SerializeField, Min(0)] private int requiredFriends;
        [Tooltip("Ulang otomatis setelah tertangkap.")]
        [SerializeField] private bool restartOnFail = true;
        [SerializeField] private float restartDelay = 1.5f;
        [Tooltip("Nama scene berikutnya (Build Settings, atau di editor: scene mana pun di Assets/_Game/Scenes). " +
                 "Kosong = kembali ke menu utama.")]
        [SerializeField] private string nextScene = "";
        [SerializeField] private float nextDelay = 2f;

        private readonly List<FriendSlime> saved = new List<FriendSlime>();

        public IReadOnlyList<Jar> Jars => jars;
        public int Required => requiredFriends > 0 ? requiredFriends : jars.Count;
        public int Saved => saved.Count;
        public bool AllSaved => Saved >= Required;
        public bool IsComplete { get; private set; }
        public bool IsFailed { get; private set; }
        public string FailReason { get; private set; } = "";
        public string NextScene => nextScene;
        public event Action<LevelGoal> Completed;
        public event Action<LevelGoal> Failed;
        public event Action<FriendSlime> FriendSaved;

        private void Awake()
        {
            jars.RemoveAll(j => j == null);
            if (jars.Count == 0) jars.AddRange(FindObjectsByType<Jar>(FindObjectsSortMode.None));
        }

        // Dipanggil FriendSlime saat masuk ventilasi/exit.
        public void NotifySaved(FriendSlime friend)
        {
            if (friend == null || saved.Contains(friend)) return;
            saved.Add(friend);
            FriendSaved?.Invoke(friend);
        }

        // Dipanggil exit saat potongan yang dikendalikan menyentuhnya.
        public bool TryComplete(SlimePiece piece)
        {
            if (IsComplete || IsFailed || piece == null || !piece.IsControlled) return false;
            if (!AllSaved) return false;
            IsComplete = true;
            Debug.Log($"Level selesai: {Saved} teman selamat.");
            Completed?.Invoke(this);
            if (!string.IsNullOrEmpty(nextScene)) GameFlow.Unlock(nextScene);
            StartCoroutine(LoadNext());
            return true;
        }

        // Level berikutnya, atau menu utama kalau ini level terakhir yang ada.
        private IEnumerator LoadNext()
        {
            yield return new WaitForSeconds(nextDelay);
            if (string.IsNullOrEmpty(nextScene)) GameFlow.LoadMainMenu();
            else GameFlow.Load(nextScene);
        }

        // Dipanggil penjaga saat menangkap pemain/potongan/teman.
        public void Fail(string reason)
        {
            if (IsComplete || IsFailed) return;
            IsFailed = true;
            FailReason = reason;
            Debug.Log("Level gagal: " + reason);
            Failed?.Invoke(this);
            if (restartOnFail) StartCoroutine(Restart());
        }

        private IEnumerator Restart()
        {
            yield return new WaitForSeconds(restartDelay);
            GameFlow.ReloadCurrent();
        }
    }
}
