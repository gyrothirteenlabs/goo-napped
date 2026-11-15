namespace GooNapped
{
    // Data tampil per level (urut GameFlow.Levels): nama, area, objektif, teman.
    // Sumber: tabel docs/desain-v2.md bagian 3. Dipakai main menu (Select Level) dan pause.
    // Teks yang tampil di game = bahasa Inggris. Kalau layout level dari tim berubah, ubah tabel ini.
    public static class LevelCatalog
    {
        public struct Info
        {
            public string name;
            public bool rescue;
            public string[] guards;
            // (nama tampil, nama aset RecipeData untuk RescueLog). Aset kosong = tidak dicatat RescueLog (Raja).
            public (string name, string asset)[] friends;
            public string lesson;
        }

        private static readonly (string, string)[] None = new (string, string)[0];

        private static readonly Info[] Infos =
        {
            new Info { name = "The Gate", rescue = false, guards = new[] { "Gate Monster" }, friends = None,
                lesson = "Light means danger. Find the gap in the fence." },
            new Info { name = "Hedge Garden", rescue = false, guards = new[] { "Golem" }, friends = None,
                lesson = "Read the vision cones. Hedges block their sight." },
            new Info { name = "Mud Path", rescue = false, guards = new[] { "Golem", "Slime Hound" }, friends = None,
                lesson = "Mud leaves a scent trail. Wash it off in a puddle." },
            new Info { name = "Empty Cells", rescue = true, guards = new[] { "Golem" }, friends = new[] { ("Pip", "Friend_Pip") },
                lesson = "Eat, split, leave a copy in the jar, lead Pip out." },
            new Info { name = "Cell Block", rescue = true, guards = new[] { "Golem", "Slime Hound" }, friends = new[] { ("Bolu", "Friend_Bolu") },
                lesson = "Hide inside objects. Big friends need two essences." },
            new Info { name = "Drain Lab", rescue = true, guards = new[] { "Fire Familiar", "Witch Eye" }, friends = new[] { ("Tok", "Friend_Tok"), ("Lumi", "Friend_Lumi") },
                lesson = "The drain alarm is ticking. Your first friend power." },
            new Info { name = "Great Hall", rescue = true, guards = new[] { "Hollow Armor", "Golem" }, friends = new[] { ("Dad", "Friend_Dad") },
                lesson = "The armor hunts sound. Move quietly, watch the shift change." },
            new Info { name = "Dining Hall", rescue = true, guards = new[] { "Hooded Servant", "Slime Hound" }, friends = new[] { ("Mom", "Friend_Mom") },
                lesson = "Plenty of hiding spots, but servants open them." },
            new Info { name = "Gallery", rescue = true, guards = new[] { "Watching Painting", "Witch Eye" }, friends = new[] { ("Sis", "Friend_Sis") },
                lesson = "Still guards sweep the room. Use the vent network." },
            new Info { name = "Tower Stairs", rescue = true, guards = new[] { "Mixed Guard" }, friends = new[] { ("Gran", "Friend_Gran") },
                lesson = "Every trick you know, all at once." },
            new Info { name = "The Summit", rescue = true, guards = new[] { "The Alchemist" }, friends = new[] { ("The King", "") },
                lesson = "Turn the three valves. One last split." },
        };

        public static int Count => Infos.Length;

        public static Info Get(int i) =>
            i >= 0 && i < Infos.Length ? Infos[i]
            : new Info { name = i >= 0 && i < GameFlow.Levels.Length ? GameFlow.Levels[i] : "?", guards = new string[0], friends = None, lesson = "" };

        public static string Name(int i) => Get(i).name;

        public static string Area(int i) => i < 3 ? "Castle Grounds" : i < 6 ? "Dungeon" : i < 10 ? "Upper Castle" : "Summit";

        public static bool IsCleared(int i) => GameFlow.IsUnlocked(i) && i < GameFlow.UnlockedIndex;

        // Teman yang sudah selamat di level ini. Raja (tanpa aset) dihitung selamat kalau level selesai.
        public static int SavedCount(int i)
        {
            int saved = 0;
            foreach (var f in Get(i).friends)
                if (string.IsNullOrEmpty(f.asset) ? IsCleared(i) : RescueLog.IsRescued(f.asset)) saved++;
            return saved;
        }
    }
}
