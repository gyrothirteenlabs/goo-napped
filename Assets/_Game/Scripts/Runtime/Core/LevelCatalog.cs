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
            // Kalimat pembuka saat masuk level (LevelIntro, kamera kembali ke slime). SEMENTARA, ganti oleh tim cerita.
            public string intro;
        }

        private static readonly (string, string)[] None = new (string, string)[0];

        private static readonly Info[] Infos =
        {
            new Info { name = "The Gate", rescue = false, guards = new[] { "Gate Monster" }, friends = None,
                lesson = "Light means danger. Find the gap in the fence.",
                intro = "The front gate. Its guardian sees everything that stands in the light." },
            new Info { name = "Hedge Garden", rescue = false, guards = new[] { "Golem" }, friends = None,
                lesson = "Read the vision cones. Hedges block their sight.",
                intro = "The castle garden. Golems walk the hedge rows, but hedges block their sight." },
            new Info { name = "Mud Path", rescue = false, guards = new[] { "Golem", "Slime Hound" }, friends = None,
                lesson = "Mud leaves a scent trail. Wash it off in a puddle.",
                intro = "Mud everywhere. Whatever steps in it leaves a smell the hounds can follow." },
            new Info { name = "Empty Cells", rescue = true, guards = new[] { "Golem" }, friends = new[] { ("Pip", "Friend_Pip") },
                lesson = "Eat, split, leave a copy in the jar, lead Pip out.",
                intro = "The old cells are empty now. Almost. Pip is down here somewhere." },
            new Info { name = "Cell Block", rescue = true, guards = new[] { "Golem", "Slime Hound" }, friends = new[] { ("Bolu", "Friend_Bolu") },
                lesson = "Hide inside objects. Big friends need two essences.",
                intro = "Bolu is locked deeper in the cell block. A big friend needs a big copy." },
            new Info { name = "Drain Lab", rescue = true, guards = new[] { "Fire Familiar", "Witch Eye" }, friends = new[] { ("Tok", "Friend_Tok"), ("Lumi", "Friend_Lumi") },
                lesson = "The drain alarm is ticking. Your first friend power.",
                intro = "The drain lab. Tok and Lumi's auras are flowing up these pipes." },
            new Info { name = "Great Hall", rescue = true, guards = new[] { "Hollow Armor", "Golem" }, friends = new[] { ("Dad", "Friend_Dad") },
                lesson = "The armor hunts sound. Move quietly, watch the shift change.",
                intro = "The great hall. The empty armor cannot see, but it hunts every sound." },
            new Info { name = "Dining Hall", rescue = true, guards = new[] { "Hooded Servant", "Slime Hound" }, friends = new[] { ("Mom", "Friend_Mom") },
                lesson = "Plenty of hiding spots, but servants open them.",
                intro = "The dining hall. Plenty of places to hide, and servants who check them." },
            new Info { name = "Gallery", rescue = true, guards = new[] { "Watching Painting", "Witch Eye" }, friends = new[] { ("Sis", "Friend_Sis") },
                lesson = "Still guards sweep the room. Use the vent network.",
                intro = "The gallery. Even the paintings are watching." },
            new Info { name = "Tower Stairs", rescue = true, guards = new[] { "Mixed Guard" }, friends = new[] { ("Gran", "Friend_Gran") },
                lesson = "Every trick you know, all at once.",
                intro = "The tower stairs. Gran is waiting near the top." },
            new Info { name = "The Summit", rescue = true, guards = new[] { "The Alchemist" }, friends = new[] { ("The King", "") },
                lesson = "Turn the three valves. One last split.",
                intro = "The summit. The machine, the King, and the Alchemist himself." },
        };

        public static int Count => Infos.Length;

        public static Info Get(int i) =>
            i >= 0 && i < Infos.Length ? Infos[i]
            : new Info { name = i >= 0 && i < GameFlow.Levels.Length ? GameFlow.Levels[i] : "?", guards = new string[0], friends = None, lesson = "" };

        public static string Name(int i) => Get(i).name;

        // Kalimat objektif untuk pembuka level (kamera menyorot tujuan). Tidak menyebut letak toples: menemukannya bagian dari level.
        public static string Objective(int i)
        {
            var info = Get(i);
            if (i == Count - 1) return "Turn the three aura valves. The King's jar is waiting.";
            if (!info.rescue || info.friends.Length == 0) return "This vent is the way forward. Reach it without getting caught.";
            string who = info.friends.Length == 1 ? info.friends[0].name : info.friends[0].name + " and " + info.friends[1].name;
            return who + (info.friends.Length == 1 ? " is" : " are") + " locked in a jar on this floor. Find them, then lead them out through this vent.";
        }

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
