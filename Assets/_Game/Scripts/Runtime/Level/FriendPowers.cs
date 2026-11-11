using System.Collections.Generic;
using UnityEngine;

namespace GooNapped
{
    // Kekuatan teman (desain-v2 bagian 8). Teman yang auranya pulih membantu dari DALAM ventilasi:
    //   di ventilasi: Shift = pilih kekuatan, A/D = pilih lubang, E = pakai dari lubang itu.
    //   Dentum (Thump)  : ketukan keras, penjaga di sekitar datang memeriksa lubang.
    //   Padam  (Snuff)  : lampu di sekitar lubang padam 10 detik.
    //   Umpan  (Decoy)  : kristal berbentuk slime di depan lubang; penjaga melihatnya, mengejar, lalu memecahkannya.
    //   Kabut  (Mist)   : kabut di depan lubang menutup pandangan 6 detik.
    // Terbuka begitu teman pembawanya selamat (dicatat antar level di PlayerPrefs). Jatah terisi ulang tiap checkpoint.
    public static class FriendPowers
    {
        const string UnlockKey = "gn_powers";

        public static int MaxCharges(FriendPower p)
        {
            switch (p)
            {
                case FriendPower.Thump: return 3;
                case FriendPower.Snuff: return 2;
                case FriendPower.Decoy: return 1;
                case FriendPower.Mist: return 2;
            }
            return 0;
        }

        public static string NameOf(FriendPower p)
        {
            switch (p)
            {
                case FriendPower.Thump: return "Thump";
                case FriendPower.Snuff: return "Snuff";
                case FriendPower.Decoy: return "Crystal Decoy";
                case FriendPower.Mist: return "Mist";
            }
            return "";
        }

        private static readonly Dictionary<FriendPower, int> charges = new Dictionary<FriendPower, int>();
        private static int selected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            charges.Clear();
            selected = 0;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnScene;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnScene;
        }

        private static void OnScene(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) => Refill();

        // Kekuatan yang sudah terbuka (urut sesuai enum).
        public static List<FriendPower> Unlocked()
        {
            var list = new List<FriendPower>();
            string saved = PlayerPrefs.GetString(UnlockKey, "");
            foreach (FriendPower p in System.Enum.GetValues(typeof(FriendPower)))
                if (p != FriendPower.None && saved.Contains("[" + p + "]")) list.Add(p);
            return list;
        }

        public static void Unlock(FriendPower p)
        {
            if (p == FriendPower.None) return;
            string saved = PlayerPrefs.GetString(UnlockKey, "");
            if (saved.Contains("[" + p + "]")) return;
            PlayerPrefs.SetString(UnlockKey, saved + "[" + p + "]");
            PlayerPrefs.Save();
            charges[p] = MaxCharges(p);
        }

        public static void ResetUnlocks()
        {
            PlayerPrefs.DeleteKey(UnlockKey);
            PlayerPrefs.Save();
            charges.Clear();
        }

        // Aura pulih saat beristirahat: dipanggil tiap checkpoint dan tiap scene dimuat.
        public static void Refill()
        {
            foreach (var p in Unlocked()) charges[p] = MaxCharges(p);
        }

        public static int Charges(FriendPower p) => charges.TryGetValue(p, out var c) ? c : MaxCharges(p);

        public static FriendPower Selected
        {
            get
            {
                var list = Unlocked();
                if (list.Count == 0) return FriendPower.None;
                return list[Mathf.Abs(selected) % list.Count];
            }
        }

        public static void SelectNext() => selected++;

        // Pakai kekuatan terpilih dari lubang `vent`. Mengembalikan pesan untuk HUD.
        public static string Use(Vent vent, Sprite blobSprite)
        {
            var p = Selected;
            if (p == FriendPower.None) return "No friend in the vents can help yet.";
            if (Charges(p) <= 0) return NameOf(p) + " is used up. It recovers at the next vent checkpoint.";
            Vector2 at = vent.transform.position;
            Vector2 front = vent.Spot.ExitPoint(at + Vector2.down, 0.25f);
            switch (p)
            {
                case FriendPower.Thump:
                    NoiseBus.Emit(at, 5.5f);
                    AudioDirector.Play(AudioDirector.Sfx.GolemStep, at, 1f, 0.05f);
                    break;
                case FriendPower.Snuff:
                    int n = 0;
                    foreach (var l in Object.FindObjectsByType<Extinguishable>(FindObjectsSortMode.None))
                        if (l.IsLit && Vector2.Distance(l.transform.position, at) <= 4f) { l.ExtinguishFor(10f); n++; }
                    if (n == 0) return "No lights close enough to this vent.";
                    break;
                case FriendPower.Decoy:
                    Decoy.Spawn(front, blobSprite);
                    break;
                case FriendPower.Mist:
                    MistCloud.Spawn(front, blobSprite);
                    break;
            }
            charges[p] = Charges(p) - 1;
            return NameOf(p) + "! (" + charges[p] + " left)";
        }
    }
}
