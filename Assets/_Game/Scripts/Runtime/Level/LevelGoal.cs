using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GooNapped
{
    // Objektif level: semua teman ditukar dengan tiruan, lalu dibawa ke exit.
    // Gagal kalau ketahuan penjaga: level diulang dari awal.
    public class LevelGoal : MonoBehaviour
    {
        [Tooltip("Toples di level ini. Kosong = semua Jar di scene.")]
        [SerializeField] private List<Jar> jars = new List<Jar>();
        [Tooltip("Jumlah teman yang harus dibawa ke exit. 0 = semua toples.")]
        [SerializeField, Min(0)] private int requiredFriends;
        [Tooltip("Ulang level otomatis setelah ketahuan.")]
        [SerializeField] private bool restartOnFail = true;
        [SerializeField] private float restartDelay = 1.5f;
        [Tooltip("Nama scene berikutnya (harus ada di Build Settings). Kosong = tetap di level ini.")]
        [SerializeField] private string nextScene = "";
        [SerializeField] private float nextDelay = 2f;

        public IReadOnlyList<Jar> Jars => jars;
        public int Required => requiredFriends > 0 ? requiredFriends : jars.Count;
        public bool IsComplete { get; private set; }
        public bool IsFailed { get; private set; }
        public string FailReason { get; private set; } = "";
        public event Action<LevelGoal> Completed;
        public event Action<LevelGoal> Failed;

        private void Awake()
        {
            jars.RemoveAll(j => j == null);
            if (jars.Count == 0) jars.AddRange(FindObjectsByType<Jar>(FindObjectsSortMode.None));
        }

        // Dipanggil LevelExit saat potongan yang dikendalikan menyentuh exit.
        public bool TryComplete(SlimePiece piece)
        {
            if (IsComplete || IsFailed || piece == null || !piece.IsControlled) return false;
            if (piece.Carried.Count < Required) return false;
            IsComplete = true;
            Debug.Log($"Level selesai: {piece.Carried.Count} teman dibawa kabur.");
            Completed?.Invoke(this);
            if (!string.IsNullOrEmpty(nextScene)) StartCoroutine(LoadNext());
            return true;
        }

        private IEnumerator LoadNext()
        {
            yield return new WaitForSeconds(nextDelay);
            if (Application.CanStreamedLevelBeLoaded(nextScene)) SceneManager.LoadScene(nextScene);
            else Debug.LogWarning($"LevelGoal: scene '{nextScene}' tidak ada di Build Settings.");
        }

        // Dipanggil penjaga saat curiga penuh.
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
            var scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // Scene sandbox tidak ada di Build Settings, jadi di editor dimuat lewat path.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scene.buildIndex);
#endif
        }
    }
}
