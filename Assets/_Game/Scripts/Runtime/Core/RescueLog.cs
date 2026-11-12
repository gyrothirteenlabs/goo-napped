using UnityEngine;

namespace GooNapped
{
    // Teman yang sudah diselamatkan, dicatat antar level (PlayerPrefs). Dipakai markas (siapa yang hadir).
    public static class RescueLog
    {
        const string Key = "gn_rescued";

        public static bool IsRescued(RecipeData friend) =>
            friend != null && PlayerPrefs.GetString(Key, "").Contains("[" + friend.name + "]");

        public static void Add(RecipeData friend)
        {
            if (friend == null || IsRescued(friend)) return;
            PlayerPrefs.SetString(Key, PlayerPrefs.GetString(Key, "") + "[" + friend.name + "]");
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
