using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GooNapped.EditorTools
{
    // Blockout level dari peta teks (BUKAN desain final). Mengisi scene level placeholder:
    // tile lantai/batu di Tilemap Floor/Walls, pemain, toples, bahan, penjaga, exit, lampu, kamera.
    // Desainer: ubah peta di bawah lalu jalankan menu GooNapped > Blockout, atau gambar ulang langsung di Tilemap.
    //
    // Legenda:  # batu (tembok)   . lantai   S spawn pemain   E exit   J toples
    //           esens: b Batu (abu, berat)  l Pendar (hijau, menyala)  w Embun (biru, dingin)  r Bara (merah, panas, menyala)
//                  y Belerang (kuning, bau)  i Beku (putih, dingin, memadamkan)  u Bayang (ungu, sulit terlihat)   L lentera
    //           J atau 1 = toples resep pertama (recipe), 2 = toples resep kedua (recipe2)
    //           g/G, h/H, k/K  titik awal / akhir patroli Golem 1, 2, 3 (garis lurus; huruf kecil = posisi awal)
    //           f/F, v/V       titik awal / akhir patroli Familiar Api 1, 2
    //           d/D            titik awal / akhir patroli Anjing Lendir
    //           |  pintu (tertutup)   /  tuas   o  pelat tekan   O  pelat tekan berat   ~  celah (lorong tikus, slime saja)
    //           T  tong (tempat sembunyi, muat 1)   P  peti (tempat sembunyi, muat 2)
    //           A  lubang ventilasi (aman, checkpoint, jaringan "A")   X  lubang ventilasi EXIT (level selesai)
    //           m  lumpur (jejak bau beberapa detik)   =  genangan air (mencuci lumpur)
    //           n  kursi teman di markas (diisi berurutan dari Config.friends; tampil kalau sudah diselamatkan)
    //           q  barang rapuh (vas: tersenggol = pecah, berisik)   c  lantai berderit (tubuh besar/berat = berisik)
    //           B  peti dorong (massa >= 2, satu tile per dorongan)   z  tempat istirahat Golem terdekat (shift jaga)
    //           musuh unik (desain-v2 bagian 9), titik awal/akhir patroli:
    //           3/4 Zirah Hampa   5/6 Mata Penyihir   7/8 Pelayan Tudung   9/0 Alkemis
    //           p  Lukisan Bermata (diam, menyapu, alarm)   @  Monster Gerbang (diam, sorot = tertangkap)
    //           Y  katup aura   R  toples raja (adegan final; FinalSequence dipasang otomatis)
    //           Tuas dan pelat tersambung ke PINTU TERDEKAT (atur peta supaya jelas, atau ubah sambungan di Inspector).
    //           Prefab props: GooNapped > Placeholder > Props level (dibuat otomatis kalau belum ada).
    public static class LevelBlockout
    {
        const string P = "Assets/_Game/Prefabs/";
        const string D = "Assets/_Game/Data/";
        const string TileDir = "Assets/_Game/Art/Tiles/Placeholder/";

        class Config
        {
            public string scene, recipe, recipe2, next, note;
            public string[] map;
            // markas: teman untuk kursi `n`, berurutan (aset Data/Recipes/Friend_*)
            public string[] friends;
        }

        static readonly Config C1L01 = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/Chapter1/C1_L01.unity",
            recipe = "Recipe_Kerikil",
            next = "C1_L02",
            note = "BLOCKOUT C1_L01 (tutorial, tanpa penjaga). Jalan setapak tebing tambang, malam.\n" +
                   "Mengajarkan: gerak, intip toples, makan 1 bahan (Serbuk Batu = abu + berat), belah, tukar, telan, exit.\n" +
                   "Resep Kerikil: abu + berat. Dibangun oleh GooNapped > Blockout > C1_L01 (Editor/Level/LevelBlockout.cs). Layout final: tim desainer.",
            map = new[]
            {
                "####################",
                "#..........#########",
                "#.S........#########",
                "#......#..L#.....J.#",
                "#......#...........#",
                "####...#####...L...#",
                "####.......####....#",
                "####.b.....b...E...#",
                "####################",
            },
        };

        static readonly Config C1L02 = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/Chapter1/C1_L02.unity",
            recipe = "Recipe_Pendar",
            next = "C1_L03",
            note = "BLOCKOUT C1_L02 (kenalan Golem). Kamp tambang, malam.\n" +
                   "Golem patroli di lorong tengah, melihat lurus. Lewati di belakangnya, atau tinggalkan potongan nyamar untuk menghadangnya (golem berhenti lalu putar balik).\n" +
                   "Resep Pendar: berat + berkilau. Lumut Pendar membuat tubuh menyala: terlihat dari lebih jauh.\n" +
                   "Dibangun oleh GooNapped > Blockout > C1_L02. Layout final: tim desainer.",
            map = new[]
            {
                "######################",
                "#S...#.......b.......#",
                "#....#..####...####..#",
                "#....#g.............G#",
                "#.......####...####..#",
                "######..#.........J..#",
                "#.l.....#....L.......#",
                "#...L...#....b.......#",
                "#E......#............#",
                "######################",
            },
        };

        static readonly Config C1L03 = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/Chapter1/C1_L03.unity",
            recipe = "Recipe_Debu",
            next = "C1_L04",
            note = "BLOCKOUT C1_L03 (urutan makan + 2 golem). Lorong tambang sempit.\n" +
                   "Resep Debu: abu + berkilau. Lumut Pendar (hijau + berkilau) dulu, LALU Serbuk Batu (abu menimpa hijau). Kebalik = warna salah.\n" +
                   "Golem 1 patroli lorong tengah (horizontal), Golem 2 patroli poros tambang (vertikal), jalurnya bersilangan.\n" +
                   "Lumut membuat tubuh menyala (terlihat lebih jauh) selama dibawa ke Serbuk Batu.\n" +
                   "Dibangun oleh GooNapped > Blockout > C1_L03. Layout final: tim desainer.",
            map = new[]
            {
                "########################",
                "#.....#.....h....#..J..#",
                "#..S..#.###...##.......#",
                "#.....#.#.......##..L..#",
                "###.###.#.##.#......####",
                "#g.................G#..#",
                "#.###.####.#.##.###.#..#",
                "#.#l#.....#...#...#....#",
                "#.#.#.L.b.#.H.#.b.#.####",
                "#E..#.....#...#.......##",
                "########################",
            },
        };

        static readonly Config C1L04 = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/Chapter1/C1_L04.unity",
            recipe = "Recipe_Tetua",
            next = "C2_L01",
            note = "BLOCKOUT C1_L04 (ujian chapter 1). Mulut tambang menuju gerbang kastil, slime tetua.\n" +
                   "Resep Tetua: berat + dingin (Serbuk Batu + Air Embun). Kabur sambil berat = lambat.\n" +
                   "Golem 1 patroli jalan utama, Golem 2 menjaga gerbang (exit). Pancing Golem 2 dengan umpan:\n" +
                   "makan Lumut Pendar, belah, tinggalkan potongan menyala di depan pandangannya, lalu Shift dan menyelinap ke gerbang.\n" +
                   "Dibangun oleh GooNapped > Blockout > C1_L04. Layout final: tim desainer.",
            map = new[]
            {
                "##########################",
                "#S...#..........#....J...#",
                "#....#.l..####..#........#",
                "#....#....#..#..#..L.....#",
                "##.###....#b.#..####.#####",
                "#g.....................G.#",
                "#.####.###.#####.#######.#",
                "#....#.#w#.#...#.#..L..#.#",
                "#.L..#.....#.b.#.#..h..H.#",
                "#...............#.#.....E#",
                "##########################",
            },
        };

        static readonly Config C2L01 = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/Chapter2/C2_L01.unity",
            recipe = "Recipe_Bara",
            next = "C2_L02",
            note = "BLOCKOUT C2_L01 (kenalan Familiar Api). Halaman kastil malam, sedikit lampu.\n" +
                   "Familiar Api melayang di jalan tengah: BUTA di gelap, tapi melihat yang panas/menyala dari jauh, dan membawa cahaya sendiri.\n" +
                   "Resep Bara: merah + berat. Serbuk Batu dulu, lalu Ramuan Api (merah + panas + menyala).\n" +
                   "Setelah minum Ramuan Api kamu terlihat dari jauh oleh Familiar: pakai gumpalan (F) untuk memancingnya, atau muntahkan (Q) setelah tukar.\n" +
                   "Dibangun oleh GooNapped > Blockout > C2_L01. Layout final: tim desainer.",
            map = new[]
            {
                "########################",
                "#S....#........#.....J.#",
                "#.....#.####...#.......#",
                "#..b..#.#..#...####.####",
                "#.....#.#..#...........#",
                "##.####.#..#####.###...#",
                "#.......f..........F...#",
                "#.###.####.####.###.####",
                "#.#r#....#....#.......L#",
                "#.#.#.L..#.b..#........#",
                "#E..#.........#....r...#",
                "########################",
            },
        };

        static readonly Config C2L02 = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/Chapter2/C2_L02.unity",
            recipe = "Recipe_Lumpur",
            next = "C2_L03",
            note = "BLOCKOUT C2_L02 (kenalan Anjing Lendir). Kandang & selokan kastil.\n" +
                   "Anjing Lendir: penglihatan pendek, tapi MENCIUM jejak bau dan mengikutinya (tembus tikungan, tidak butuh cahaya).\n" +
                   "Resep Lumpur: kuning + berat. Serbuk Batu lalu Minyak Belerang (kuning + bau). Selama berbau kamu meninggalkan jejak.\n" +
                   "Trik: belah saat masih berbau, tinggalkan potongan berbau nyamar sebagai umpan; atau muntahkan (Q) Minyak Belerang setelah tiruan masuk toples.\n" +
                   "Dibangun oleh GooNapped > Blockout > C2_L02. Layout final: tim desainer.",
            map = new[]
            {
                "########################",
                "#S...#.........#.....J.#",
                "#....#.###.###.#.......#",
                "#.b..#.#.....#.###.#####",
                "#....#.#..y..#.........#",
                "##.###.##.####.###.###.#",
                "#......d...........D...#",
                "#.####.####.#####.####.#",
                "#.#..#....#.....#......#",
                "#.#y.#.L..#..b..#..L...#",
                "#E...#....#.....#......#",
                "########################",
            },
        };

        static readonly Config C2L03 = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/Chapter2/C2_L03.unity",
            recipe = "Recipe_Percik",
            next = "C2_L04",
            note = "BLOCKOUT C2_L03 (ruang tungku). Familiar Api di lorong tengah, Golem di gudang bawah, banyak lentera.\n" +
                   "Resep Percik: merah + berkilau. Lumut Pendar dulu, lalu Ramuan Api: dua-duanya menyala (Familiar melihat dari jauh).\n" +
                   "Setelah tukar: makan Garam Beku (ruang toples) untuk MEMADAMKAN tubuh, lalu padamkan lentera (E, butuh tubuh dingin) di jalan kabur.\n" +
                   "Dibangun oleh GooNapped > Blockout > C2_L03. Layout final: tim desainer.",
            map = new[]
            {
                "########################",
                "#S..#....L.....#.......#",
                "#...#.###.####.#...1.i.#",
                "#.l.#.#......#.##.###.##",
                "#...#.#..r...#.........#",
                "##.##.#.####.#####.##..#",
                "#.......f.........F....#",
                "#.###.####.###.####.####",
                "#.#..#..L#.......#....L#",
                "#.#..#...#..g.....G....#",
                "#E.......#.......#.....#",
                "########################",
            },
        };

        static readonly Config C2L04 = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/Chapter2/C2_L04.unity",
            recipe = "Recipe_Ibu",
            recipe2 = "Recipe_Adik",
            next = "Ending",
            note = "BLOCKOUT C2_L04 (ujian chapter 2). Gudang toples besar: DUA toples (ibu dan adik), Golem + Familiar + Anjing.\n" +
                   "Resep Ibu: kuning + panas (Minyak Belerang + Ramuan Api: berbau DAN menyala).\n" +
                   "Resep Adik: abu + dingin. Garam Beku dulu (putih + dingin), lalu Serbuk Batu (abu menimpa putih).\n" +
                   "Butuh dua kali belah: setelah toples pertama, bentuk ulang tubuh (muntah Q / makan lagi) untuk toples kedua.\n" +
                   "Dibangun oleh GooNapped > Blockout > C2_L04. Layout final: tim desainer.",
            map = new[]
            {
                "##########################",
                "#S...#.....1.....#....2..#",
                "#....#.#########.#.......#",
                "#.y..#.#.......#.#.##.##.#",
                "#....#...L.....#.........#",
                "##.###.###.#####.###.#####",
                "#g.....................G.#",
                "#.##.####.#####.####.###.#",
                "#.#r.#..d.....D.#..f....F#",
                "#.#..#.........#.#.......#",
                "#.##.#..b..i...#.#..L....#",
                "#E.......................#",
                "##########################",
            },
        };

        // Ruang uji props (BUKAN level, tidak masuk build): pintu+tuas, celah, pelat tekan, pelat berat.
        static readonly Config SandboxProps = new Config
        {
            scene = "Assets/_Game/Scenes/Sandbox/ProtoProps.unity",
            recipe = "Friend_Sis",
            next = "",
            note = "SANDBOX ProtoProps: ruang uji pintu, tuas, pelat tekan, celah. Bukan desain level.\n" +
                   "Ruang A: tarik tuas (E) untuk membuka pintu ke ruang B (bunyinya memancing Golem).\n" +
                   "Ruang B -> C lewat celah (~): slime bisa menyelinap, Golem tidak; tubuh berat (Serbuk Batu) tidak muat.\n" +
                   "Ruang C: makan Esens Belerang (y) lalu Space di depan toples Sis (kuning, kecil). Pintu ke exit dibuka pelat tekan:\n" +
                   "suruh teman menunggu di pelat (F) atau tinggalkan potongan di sana, lalu lewat pintu.\n" +
                   "Pelat berat (ruang A, bawah) hanya tertekan tubuh berat: Esens Batu atau massa >= 3.\n" +
                   "Dibangun oleh GooNapped > Blockout > Sandbox ProtoProps.",
            map = new[]
            {
                "##########################",
                "#S.....#.........#....J..#",
                "#......|..g....G.~.y.....#",
                "#..b../#.........#.....r.#",
                "#......#.........#..o....#",
                "#...O..#.........#####|###",
                "####|###.........#.......#",
                "#.l....#.........#...E...#",
                "##########################",
            },
        };

        // Ruang uji alur inti v2 (BUKAN level, tidak masuk build): makan esens -> belah ke toples -> ajak teman (F) -> exit.
        static readonly Config SandboxV2 = new Config
        {
            scene = "Assets/_Game/Scenes/Sandbox/ProtoV2.unity",
            recipe = "Friend_Pip",
            recipe2 = "Friend_Bolu",
            next = "Ending",   // sementara jadi satu-satunya level di alur game (GameFlow.Levels)
            note = "SANDBOX ProtoV2: semua sistem desain v2 di satu ruang uji. Bukan desain level.\n" +
                   "Ventilasi (A): C = masuk (aman, checkpoint), A/D = pindah lubang, C = keluar. Lubang X = exit level.\n" +
                   "Toples 1 (Pip, kecil, aura Embun/biru): makan 1 Esens Embun (w) lalu Space di depan toples.\n" +
                   "Toples 2 (Bolu, besar, aura Batu/abu): massa 3, esens TERAKHIR = Batu (b). Contoh: Pendar (l) lalu Batu.\n" +
                   "Teman keluar dan menunggu: F = ajak ikut / suruh tunggu. Bawa ke ventilasi mana pun = selamat.\n" +
                   "Golem di lorong tengah, Anjing di lorong bawah. Lumpur (m) meninggalkan jejak bau, genangan (=) mencucinya.\n" +
                   "Tong (T) muat 1, peti (P) muat 2. Pintu (|) dibuka tuas (/) di ruang kanan.\n" +
                   "Dibangun oleh GooNapped > Blockout > Sandbox ProtoV2.",
            map = new[]
            {
                "##########################",
                "#S.....#...........#.....#",
                "#......#..w.....1..#..T..#",
                "#..A...........l..b#.....#",
                "#......#...........|.../.#",
                "####.######.####...2.#####",
                "#g.................G.....#",
                "#.##.###.####.####.###.A.#",
                "#..mmmm....P.......=.....#",
                "#X....d..........D.......#",
                "##########################",
            },
        };

        // Markas di jaringan ventilasi (desain-v2 bagian 12): muncul di antara level. Teman yang sudah diselamatkan duduk di
        // kursinya dan bisa diajak bicara (E). Ventilasi exit = lanjut ke level berikutnya.
        static readonly Config Hub = new Config
        {
            scene = "Assets/_Game/Scenes/Story/Markas.unity",
            next = "",
            friends = new[] { "Friend_Pip", "Friend_Bolu", "Friend_Tok", "Friend_Lumi", "Friend_Dad", "Friend_Mom", "Friend_Sis", "Friend_Gran" },
            note = "MARKAS (hideout) di saluran lendir tua. Bukan desain final: layout dan art dari tim desainer.\n" +
                   "Teman yang sudah diselamatkan (RescueLog) duduk di kursi n. E di dekatnya = bicara (RecipeData.HubLines).\n" +
                   "Lubang X = lanjut ke level berikutnya (GameFlow.PendingNext). Dibangun oleh GooNapped > Blockout > Markas.",
            map = new[]
            {
                "################",
                "#..n...L...n...#",
                "#n...........n.#",
                "#......S.......#",
                "#n...........n.#",
                "#..n...L...n...#",
                "#......X.......#",
                "################",
            },
        };

        // Ruang uji benda interaksi (BUKAN level): vas rapuh, lantai berderit, peti dorong ke pelat, Golem dengan shift jaga.
        static readonly Config SandboxInteract = new Config
        {
            scene = "Assets/_Game/Scenes/Sandbox/ProtoInteract.unity",
            next = "",
            note = "SANDBOX ProtoInteract: benda interaksi desain v2 bagian 10. Bukan desain level.\n" +
                   "Vas (q): tersenggol = pecah, bunyinya memancing Golem. Lantai berderit (c): berbunyi kalau tubuhmu besar/berat.\n" +
                   "Peti (B): makan esens (w) dulu supaya massa 2, dorong peti ke kanan sampai pelat (o) di depan tembok: pintu terbuka.\n" +
                   "Golem bertugas 12 dtk lalu istirahat 8 dtk di titik z: lewati posnya saat kosong. Exit = lubang X.",
            map = new[]
            {
                "####################",
                "#S...#.......#..z..#",
                "#..w.#..q....#.....#",
                "#....|.......###.###",
                "#.B.o#..cccc.......#",
                "#....#.......gG..X.#",
                "#....#.......#.....#",
                "####################",
            },
        };

        [MenuItem("GooNapped/Blockout/Sandbox ProtoInteract")]
        public static void BuildSandboxInteract()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SandboxInteract.scene) == null)
                AssetDatabase.CopyAsset("Assets/_Game/Scenes/Levels/Chapter1/C1_L01.unity", SandboxInteract.scene);
            Build(SandboxInteract);
        }

        // ===== Level v2 (desain-v2 bagian 3). Blockout kasar, layout final dari tim desainer. =====
        static readonly Config V2_L01_Gate = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/V2/L01_Gate.unity",
            recipe = null,
            recipe2 = null,
            next = "L02_Hedge",
            note = "L01 GERBANG (level pembuka, menyusup). Tutorial gerak.\n" +
                   "Monster Gerbang (@) menjaga halaman gerbang dan menyapu pandangan. Lewati pandangannya di celah lorong,\n" +
                   "lalu menyelinap lewat celah pagar (~) ke taman. Ventilasi A = checkpoint, X = masuk ke taman. Dibangun oleh GooNapped > Blockout > Semua level v2. Layout final: tim desainer.",
            map = new[]
            {
                "############################################",
                "#S.....#....................#..............#",
                "#......#.........@..........#....T.........#",
                "#......#....................#..............#",
                "#..L...#...T..........T.....#......L.......#",
                "#......#....................####.######.####",
                "#......#....................#..............#",
                "###.####....................#..............#",
                "#...........................~..............#",
                "#......#######.######.......#..............#",
                "#......#............#.......#####.######.###",
                "#..A...#.....L......#.......#..............#",
                "#......#............#.......~..............#",
                "####.###............#.......#.......L......#",
                "#...................#.......#..............#",
                "#..............T....#.......#.......T....X.#",
                "#...........................#..............#",
                "############################################",
            },
        };

        static readonly Config V2_L02_Hedge = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/V2/L02_Hedge.unity",
            recipe = null,
            recipe2 = null,
            next = "L03_Mud",
            note = "L02 TAMAN PAGAR (menyusup). Golem patroli, pagar tanaman menutup pandangan, lentera = bahaya.\n" +
                   "Baca timing patroli, lewat di belakang golem. X = lanjut ke jalan lumpur. Dibangun oleh GooNapped > Blockout > Semua level v2. Layout final: tim desainer.",
            map = new[]
            {
                "############################################",
                "#S....#.................#..................#",
                "#.....#..g..........G...#...h........H.....#",
                "#.....#.................#..................#",
                "#..L..#...###.....###...#...####....####...#",
                "#.....#...#.........#...#......#....#......#",
                "#.....#...#....L....#..........#.L..#......#",
                "#.....#...#.........#...#......#....#......#",
                "###.###...###.....###...#...####....####...#",
                "#.......................#..................#",
                "#..A....................######.######..#####",
                "#.......#####.#####.........#.......#......#",
                "#.......#.........#.........#.......#......#",
                "#..T....#....L....#...k............K#...X..#",
                "#.......#.........#.........#.......#......#",
                "#.......#####.#####.........#.......#......#",
                "#...........................#..............#",
                "############################################",
            },
        };

        static readonly Config V2_L03_Mud = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/V2/L03_Mud.unity",
            recipe = null,
            recipe2 = null,
            next = "L04_Cells",
            note = "L03 JALAN LUMPUR (menyusup). Anjing Lendir mencium jejak; lumpur (m) meninggalkan jejak bau beberapa detik.\n" +
                   "Cuci di genangan (=). Golem menjaga dekat ventilasi turun (X) ke bawah tanah. Dibangun oleh GooNapped > Blockout > Semua level v2. Layout final: tim desainer.",
            map = new[]
            {
                "############################################",
                "#S....#..................#.................#",
                "#.....#...d..............#.......=.........#",
                "#.....#..................#.................#",
                "#..L..#..mmmmmmmmmmmm....####.#######......#",
                "#.....#..................#..........#......#",
                "#.....#######.############...T......#..L...#",
                "#........................#..........#......#",
                "###.###...mmmmmm.........#####.######......#",
                "#.....#..................#.................#",
                "#.....#....T......D......#.........mmmmm...#",
                "#..A..####.######.###########.##########.###",
                "#........................#.............#...#",
                "#.......=................#.....g.......#...#",
                "#........mmmmmmmmm.......~.............#...#",
                "#..L.....................#.......G.....#.X.#",
                "#........................#.............~...#",
                "############################################",
            },
        };

        static readonly Config V2_L04_Cells = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/V2/L04_Cells.unity",
            recipe = "Friend_Pip",
            recipe2 = null,
            next = "L05_CellBlock",
            note = "L04 PENJARA KOSONG (penyelamatan). Sel-sel kosong, golem bawah tanah.\n" +
                   "Ruang eksperimen: Pip (kecil, aura Embun). Esens pertama (w) -> belah ke toples -> F -> kawal ke ventilasi. Dibangun oleh GooNapped > Blockout > Semua level v2. Layout final: tim desainer.",
            map = new[]
            {
                "############################################",
                "#S..#..#..#..#..#..#......L..#.............#",
                "#...#..#..#..#..#..#...T.....#....1....w...#",
                "#...#..#..#..#..#..#.........#.............#",
                "#...##.##.##.##.##.#.........####.#####.####",
                "#..........................................#",
                "#.........g...........................G....#",
                "#..........................................#",
                "#...##.##.##.##.##.######.######.###########",
                "#...#..#..#..#..#..#..........#............#",
                "#.A.#..#..#..#..#..#....h..........H.......#",
                "#...#..#..#..#..#..#..........~..........X.#",
                "#...#..#..#..#..#..#..........#............#",
                "############################################",
            },
        };

        static readonly Config V2_L05_CellBlock = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/V2/L05_CellBlock.unity",
            recipe = "Friend_Bolu",
            recipe2 = null,
            next = "L06_DrainLab",
            note = "L05 LORONG SEL (penyelamatan). Tempat sembunyi pertama (T tong, P peti).\n" +
                   "Bolu (besar, aura Batu): makan Pendar (l) lalu Batu (b) TERAKHIR (berat = lambat dan berisik). Dibangun oleh GooNapped > Blockout > Semua level v2. Layout final: tim desainer.",
            map = new[]
            {
                "############################################",
                "#S...#.............#...........#...........#",
                "#....#..T.....T....#...P.......#....1......#",
                "#....#.............#...........#..l....b...#",
                "#....####.####.#####....T......#...........#",
                "#..........................................#",
                "#.......d................................D.#",
                "#..........................................#",
                "####.#######.#######.#######.######.########",
                "#.......#.........#.........#.........#....#",
                "#.A.....#....T....#....P....#....T....#..X.#",
                "#.......#.........#.........#.........#....#",
                "#.......#.........#.........#.........#....#",
                "#..g................................G......#",
                "############################################",
            },
        };

        static readonly Config V2_L06_DrainLab = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/V2/L06_DrainLab.unity",
            recipe = "Friend_Tok",
            recipe2 = "Friend_Lumi",
            next = "L07_GreatHall",
            note = "L06 LAB PENYADAP (penyelamatan, 2 toples). Familiar Api (melihat yang menyala) dan Mata Penyihir\n" +
                   "(memeriksa tempat sembunyi + membongkar tiruan beraura salah). Tok (aura Bayang, esens u), Lumi (aura Pendar, esens l). Dibangun oleh GooNapped > Blockout > Semua level v2. Layout final: tim desainer.",
            map = new[]
            {
                "############################################",
                "#S...#.........L.........#.................#",
                "#....#...1...........2...#.....T.......L...#",
                "#....#....u.......l......#.................#",
                "#....#...................####.#######.######",
                "#....######.#######.######.................#",
                "#..........................................#",
                "#..f..............................F........#",
                "#..........................................#",
                "######.##########.#######.#######.###.######",
                "#.........#.............#.......#..........#",
                "#..A......#....5...............6#....T...X.#",
                "#....T....#.............~.......#..........#",
                "#.........#.............#.................##",
                "############################################",
            },
        };

        static readonly Config V2_L07_GreatHall = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/V2/L07_GreatHall.unity",
            recipe = "Friend_Dad",
            recipe2 = null,
            next = "L08_Dining",
            note = "L07 AULA BESAR (penyelamatan). Zirah Hampa buta tapi memburu suara: lantai berderit (c) dan vas (q) berbahaya.\n" +
                   "Golem penjaga exit bergiliran istirahat di z (shift jaga). Ayah (besar, aura Bara): Embun (w) lalu Bara (r) terakhir. Dibangun oleh GooNapped > Blockout > Semua level v2. Layout final: tim desainer.",
            map = new[]
            {
                "############################################",
                "#S...#..............................#......#",
                "#....#..3.........................4.#..1...#",
                "#....#..............................#..w.r.#",
                "#....#..cccccccccccccccccccccccccc..#......#",
                "#....#..............................####.###",
                "#....###########.#######.#######...........#",
                "#..........................................#",
                "#....q.......q..........q..........q.......#",
                "#.......g.............................G....#",
                "#..........................................#",
                "#####.###########.##########.#######.#######",
                "#.......#...................#..............#",
                "#.A.....#...............z...#..h....H..X...#",
                "#.......#...................#..............#",
                "############################################",
            },
        };

        static readonly Config V2_L08_Dining = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/V2/L08_Dining.unity",
            recipe = "Friend_Mom",
            recipe2 = null,
            next = "L09_Gallery",
            note = "L08 RUANG MAKAN (penyelamatan). Banyak tempat sembunyi, tapi Pelayan Tudung rutin memeriksanya. Anjing di bawah.\n" +
                   "Ibu (besar, aura Embun): Belerang (y) lalu Embun (w) terakhir (bau Belerang ikut ke tiruan). Dibangun oleh GooNapped > Blockout > Semua level v2. Layout final: tim desainer.",
            map = new[]
            {
                "############################################",
                "#S...#.............T..........#............#",
                "#....#..T....P............q...#.....1......#",
                "#....#........................#...y....w...#",
                "#....#..7..................8..#............#",
                "#....#........T......P........####.#########",
                "#....#####.###########.#######.............#",
                "#..........................................#",
                "#...d......q................D.......q......#",
                "#..........................................#",
                "####.##########.###########.#########.######",
                "#..........#..........#.........#..........#",
                "#.A...T....#....P.....#....T....#.....X....#",
                "#..........#..........#.........#..........#",
                "############################################",
            },
        };

        static readonly Config V2_L09_Gallery = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/V2/L09_Gallery.unity",
            recipe = "Friend_Sis",
            recipe2 = null,
            next = "L10_Tower",
            note = "L09 GALERI (penyelamatan). Lukisan Bermata diam menyapu pandangan; curiga penuh = alarm memanggil Mata Penyihir.\n" +
                   "Adik (kecil, aura Belerang): esens y. Dibangun oleh GooNapped > Blockout > Semua level v2. Layout final: tim desainer.",
            map = new[]
            {
                "############################################",
                "#S...#....p..........p...........p...#.....#",
                "#....#...............................#..1..#",
                "#....#.....T.......T.........T.......#..y..#",
                "#....#...............................#.....#",
                "#....###.#############.###########.###.#####",
                "#..........................................#",
                "#...5................................6.....#",
                "#..........................................#",
                "#####.######.#########.#######.######.######",
                "#.........#..........#........#............#",
                "#.A.......#....T.....#........#.......X....#",
                "#.........#..........#........#............#",
                "############################################",
            },
        };

        static readonly Config V2_L10_Tower = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/V2/L10_Tower.unity",
            recipe = "Friend_Gran",
            recipe2 = null,
            next = "L11_Summit",
            note = "L10 TANGGA MENARA (ujian). Campuran Golem, Anjing, Zirah Hampa. Peti (B) bisa didorong ke pelat (o) untuk membuka pintu.\n" +
                   "Nenek (besar, aura Beku): Batu (b) lalu Beku (i) terakhir. Dibangun oleh GooNapped > Blockout > Semua level v2. Layout final: tim desainer.",
            map = new[]
            {
                "############################################",
                "#S...#.............|......#........#.......#",
                "#....#...B.........#..i...#........#...1...#",
                "#....#.......o.....#......#....b...#.......#",
                "#....#.............#...............#.......#",
                "#....#######.#######......#........####.####",
                "#....#.............#......#................#",
                "#....#.............####.#####.######.......#",
                "#..........................................#",
                "#...g...................G.......d.......D..#",
                "#..........................................#",
                "######.###########.#########.#######.#######",
                "#..........#...........#..........#........#",
                "#.A........#....3..............4..#....X...#",
                "#..........#...........#..........#........#",
                "############################################",
            },
        };

        static readonly Config V2_L11_Summit = new Config
        {
            scene = "Assets/_Game/Scenes/Levels/V2/L11_Summit.unity",
            recipe = null,
            recipe2 = null,
            next = "Ending",
            note = "L11 PUNCAK (final). Putar balik 3 katup aura (Y, E) sambil diburu Alkemis yang memeriksa\n" +
                   "tempat sembunyi. Toples raja (R) terbuka: makan esens (w), belah, tinggalkan potongan di toples. Alkemis menerkam di dalamnya = tersegel. Dibangun oleh GooNapped > Blockout > Semua level v2. Layout final: tim desainer.",
            map = new[]
            {
                "############################################",
                "#S....#..........p...........#.........Y...#",
                "#.....#..T...................#....P........#",
                "#.....#......................#.............#",
                "#..A..####.########.#######..####.######.###",
                "#..........................................#",
                "#.Y.........9..........R..............0....#",
                "#......................w...................#",
                "#..T.................................T.....#",
                "#####.##########.###########.######.########",
                "#..........#.........................#.....#",
                "#....P.....#.........Y...............#..T..#",
                "#..........#.........................#.....#",
                "############################################",
            },
        };

        static readonly Config[] V2Levels = { V2_L01_Gate, V2_L02_Hedge, V2_L03_Mud, V2_L04_Cells, V2_L05_CellBlock, V2_L06_DrainLab, V2_L07_GreatHall, V2_L08_Dining, V2_L09_Gallery, V2_L10_Tower, V2_L11_Summit };

        [MenuItem("GooNapped/Blockout/Semua level v2")]
        public static void BuildAllV2()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Scenes/Levels/V2"))
                AssetDatabase.CreateFolder("Assets/_Game/Scenes/Levels", "V2");
            foreach (var c in V2Levels)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(c.scene) == null)
                    AssetDatabase.CopyAsset("Assets/_Game/Scenes/Levels/Chapter1/C1_L01.unity", c.scene);
                Build(c);
            }
        }

        // Galeri musuh unik (BUKAN level): tiap musuh di ruangannya sendiri untuk dilihat dan disetel.
        static readonly Config SandboxEnemies = new Config
        {
            scene = "Assets/_Game/Scenes/Sandbox/ProtoEnemies.unity",
            recipe = "Friend_Pip",
            next = "",
            note = "SANDBOX ProtoEnemies: musuh unik desain v2 bagian 9 (sprite placeholder). Bukan desain level.\n" +
                   "Atas: Lukisan Bermata (p, diam menyapu, curiga penuh = alarm), Zirah Hampa (3-4, buta, memburu suara).\n" +
                   "Tengah: Mata Penyihir (5-6, periksa tempat sembunyi + bongkar toples beraura salah: makan Esens Batu lalu masukkan ke toples Pip),\n" +
                   "Pelayan Tudung (7-8, membuka tempat sembunyi). Bawah: Monster Gerbang (@, sorot = tertangkap), Alkemis (9-0, pemburu).",
            map = new[]
            {
                "##########################",
                "#S........#..p...#.......#",
                "#.........#......#...3...#",
                "#..w.b...........#.......#",
                "#.........#..........4...#",
                "####.#########.######.####",
                "#5........6#...7.......8.#",
                "#..T...J...#...T....P....#",
                "#..........#.............#",
                "######.#########.#########",
                "#9..........@...........0#",
                "#.....................X..#",
                "##########################",
            },
        };

        [MenuItem("GooNapped/Blockout/Sandbox ProtoEnemies")]
        public static void BuildSandboxEnemies()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SandboxEnemies.scene) == null)
                AssetDatabase.CopyAsset("Assets/_Game/Scenes/Levels/Chapter1/C1_L01.unity", SandboxEnemies.scene);
            Build(SandboxEnemies);
        }

        [MenuItem("GooNapped/Blockout/Markas")]
        public static void BuildHub()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Hub.scene) == null)
                AssetDatabase.CopyAsset("Assets/_Game/Scenes/Levels/Chapter1/C1_L01.unity", Hub.scene);
            Build(Hub);
        }

        [MenuItem("GooNapped/Blockout/Sandbox ProtoV2")]
        public static void BuildSandboxV2()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SandboxV2.scene) == null)
                AssetDatabase.CopyAsset("Assets/_Game/Scenes/Levels/Chapter1/C1_L01.unity", SandboxV2.scene);
            Build(SandboxV2);
        }

        [MenuItem("GooNapped/Blockout/Sandbox ProtoProps")]
        public static void BuildSandboxProps()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SandboxProps.scene) == null)
                AssetDatabase.CopyAsset("Assets/_Game/Scenes/Levels/Chapter1/C1_L01.unity", SandboxProps.scene);
            Build(SandboxProps);
        }

        [MenuItem("GooNapped/Blockout/C2_L01")]
        public static void BuildC2L01() => Build(C2L01);

        [MenuItem("GooNapped/Blockout/C2_L03")]
        public static void BuildC2L03() => Build(C2L03);

        [MenuItem("GooNapped/Blockout/C2_L04")]
        public static void BuildC2L04() => Build(C2L04);

        [MenuItem("GooNapped/Blockout/C2_L02")]
        public static void BuildC2L02() => Build(C2L02);

        [MenuItem("GooNapped/Blockout/C1_L03")]
        public static void BuildC1L03() => Build(C1L03);

        [MenuItem("GooNapped/Blockout/C1_L04")]
        public static void BuildC1L04() => Build(C1L04);

        [MenuItem("GooNapped/Blockout/Semua chapter 1")]
        public static void BuildChapter1()
        {
            Build(C1L01);
            Build(C1L02);
            Build(C1L03);
            Build(C1L04);
        }

        [MenuItem("GooNapped/Blockout/Semua level")]
        public static void BuildAll()
        {
            BuildChapter1();
            Build(C2L01);
            Build(C2L02);
            Build(C2L03);
            Build(C2L04);
        }

        [MenuItem("GooNapped/Blockout/C1_L01")]
        public static void BuildC1L01() => Build(C1L01);

        [MenuItem("GooNapped/Blockout/C1_L02")]
        public static void BuildC1L02() => Build(C1L02);

        static T Load<T>(string path) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) throw new System.Exception("Aset tidak ada: " + path);
            return a;
        }

        static Tile PlaceholderTile(string name, string spritePath, Color color)
        {
            string path = TileDir + name + ".asset";
            // Sprite tile ada di Sprites/Tiles/ (pivot tengah, lihat PixelSpriteImporter). JANGAN geser lewat
            // Tile.transform atau tileAnchor: keduanya ikut menggeser collider sehingga gambar dan collision tidak sejajar.
            var fix = Matrix4x4.identity;
            var t = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (t != null)
            {
                Sprite want = null;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(spritePath))
                    if (o is Sprite s0 && (want == null || string.CompareOrdinal(s0.name, want.name) < 0)) want = s0;
                if (t.transform != fix || t.sprite != want) { t.transform = fix; t.sprite = want; EditorUtility.SetDirty(t); }
                return t;
            }
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Art/Tiles/Placeholder"))
                AssetDatabase.CreateFolder("Assets/_Game/Art/Tiles", "Placeholder");
            Sprite sprite = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(spritePath))
                if (o is Sprite s && (sprite == null || string.CompareOrdinal(s.name, sprite.name) < 0)) sprite = s;
            t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = sprite;
            t.color = color;
            t.colliderType = Tile.ColliderType.Grid;
            t.transform = fix;
            AssetDatabase.CreateAsset(t, path);
            return t;
        }

        static GameObject Spawn(string prefabPath, Transform parent, Vector3 pos, string name = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(prefabPath), parent);
            go.transform.position = pos;
            if (name != null) go.name = name;
            return go;
        }

        static void Set(Object target, string prop, System.Action<SerializedProperty> set)
        {
            var so = new SerializedObject(target);
            set(so.FindProperty(prop));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.DestroyImmediate(t.GetChild(i).gameObject);
        }

        static Transform Find(UnityEngine.SceneManagement.Scene s, string path)
        {
            var parts = path.Split('/');
            foreach (var r in s.GetRootGameObjects())
                if (r.name == parts[0])
                    return parts.Length == 1 ? r.transform : r.transform.Find(string.Join("/", parts, 1, parts.Length - 1));
            return null;
        }

        static void Build(Config c)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Stop Play mode dulu."); return; }
            var scene = EditorSceneManager.OpenScene(c.scene, OpenSceneMode.Single);
            var envMat = Load<Material>("Assets/_Game/Art/Materials/Env_Lit.mat");
            var floorTile = PlaceholderTile("Tile_Ground", "Assets/_Game/Art/Sprites/Tiles/tile_floor_stone.png", new Color(0.7f, 0.6f, 0.5f));
            var rockTile = PlaceholderTile("Tile_Rock", "Assets/_Game/Art/Sprites/Tiles/tile_wall_stone.png", new Color(0.4f, 0.42f, 0.5f));

            var floor = Find(scene, "Grid/Floor").GetComponent<Tilemap>();
            var walls = Find(scene, "Grid/Walls").GetComponent<Tilemap>();
            floor.ClearAllTiles();
            walls.ClearAllTiles();
            floor.tileAnchor = walls.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            floor.GetComponent<TilemapRenderer>().sharedMaterial = envMat;
            floor.GetComponent<TilemapRenderer>().sortingOrder = -100;
            walls.GetComponent<TilemapRenderer>().sharedMaterial = envMat;
            walls.GetComponent<TilemapRenderer>().sortingOrder = -90;
            // cahaya lentera tidak tembus tembok: ShadowCaster2D dari TilemapCollider2D (tipe dicari lewat nama
            // supaya editor asmdef tidak butuh referensi ke assembly URP 2D)
            var casterType = System.Type.GetType("UnityEngine.Rendering.Universal.ShadowCaster2D, Unity.RenderPipelines.Universal.2D.Runtime");
            if (casterType != null && walls.GetComponent(casterType) == null)
            {
                var caster = walls.gameObject.AddComponent(casterType);
                Set(caster, "m_CastingOption", p => p.enumValueIndex = 1);   // CastShadow: tembok sendiri tetap terang
            }

            var actors = Find(scene, "Actors");
            var spawn = Find(scene, "Actors/PlayerSpawn");
            var exitRoot = Find(scene, "Actors/Exit");
            var jarRoot = Find(scene, "Actors/Jars");
            var ingRoot = Find(scene, "Actors/Ingredients");
            var guardRoot = Find(scene, "Actors/Guards");
            var lights = Find(scene, "Lights");
            foreach (var t in new[] { spawn, exitRoot, jarRoot, ingRoot, guardRoot, lights }) Clear(t);
            foreach (var n in new[] { "LevelSystems", "CameraBounds", "LevelProps" })
            {
                var old = GameObject.Find(n);
                if (old != null) Object.DestroyImmediate(old);
            }
            Transform propRoot = null;
            var doors = new List<Door>();
            var switches = new List<(Component comp, Vector2 pos)>();
            Transform PropRoot()
            {
                if (propRoot == null) propRoot = new GameObject("LevelProps").transform;
                if (AssetDatabase.LoadAssetAtPath<GameObject>(P + "Level/Door.prefab") == null ||
                    AssetDatabase.LoadAssetAtPath<GameObject>(P + "Level/HidingSpot_Barrel.prefab") == null ||
                    AssetDatabase.LoadAssetAtPath<GameObject>(P + "Level/Vent.prefab") == null ||
                    AssetDatabase.LoadAssetAtPath<GameObject>(P + "Level/PushCrate.prefab") == null ||
                    AssetDatabase.LoadAssetAtPath<GameObject>(P + "Level/KingJar.prefab") == null) LevelPropBuilder.BuildAll();
                return propRoot;
            }

            int h = c.map.Length, w = c.map[0].Length;
            int hubSlot = 0;
            bool hasFinal = false;
            var breakPoints = new List<Vector2>();
            const string golemKeys = "ghkfvd";   // g h k = Golem Batu, f v = Familiar Api, d = Anjing Lendir
            var uniqueStart = new Dictionary<char, string> { { '3', "HollowArmor" }, { '5', "WitchEye" }, { '7', "ClocheButler" }, { '9', "Alchemist" } };
            var uniqueEnd = new Dictionary<char, char> { { '4', '3' }, { '6', '5' }, { '8', '7' }, { '0', '9' } };
            var uniqueSingle = new Dictionary<char, string> { { 'p', "Portrait" }, { '@', "GateBeast" } };
            var uStart = new Dictionary<char, Vector2>();
            var uEnd = new Dictionary<char, Vector2>();
            var uSingles = new List<(string kind, Vector2 pos)>();
            var golemStart = new Dictionary<char, Vector2>();
            var golemEnd = new Dictionary<char, Vector2>();
            // esens (aset Data/Ingredients/Essence_<nama>): b batu, l pendar, w embun, r bara, y belerang, i beku, u bayang
            var ingredients = new Dictionary<char, string>
            {
                { 'b', "Batu" }, { 'l', "Pendar" }, { 'w', "Embun" }, { 'r', "Bara" }, { 'y', "Belerang" }, { 'i', "Beku" }, { 'u', "Bayang" },
            };
            for (int row = 0; row < h; row++)
            {
                for (int x = 0; x < w; x++)
                {
                    char ch = x < c.map[row].Length ? c.map[row][x] : '#';
                    int y = h - 1 - row;
                    var cell = new Vector3Int(x, y, 0);
                    // Posisi aktor = pivot sprite (tengah bawah cell 48px). Kaki/collider ada ±0.22–0.29 di atas pivot,
                    // jadi pivot ditaruh 0.28 di atas tepi bawah tile supaya kaki tepat di TENGAH tile (tidak menempel tembok).
                    var pos = new Vector3(x + 0.5f, y + 0.28f, 0f);
                    if (ch == '#') { walls.SetTile(cell, rockTile); continue; }
                    floor.SetTile(cell, floorTile);
                    switch (ch)
                    {
                        case 'S':
                            spawn.position = pos;
                            Spawn(P + "Characters/Slime_Player.prefab", spawn, pos, "Slime_Player");
                            break;
                        case 'E':
                        {
                            var e = new GameObject("Exit_Zone");
                            e.transform.SetParent(exitRoot, false);
                            e.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);   // sprite tile pivot tengah
                            var sr = e.AddComponent<SpriteRenderer>();
                            sr.sprite = floorTile.sprite;
                            sr.sharedMaterial = envMat;
                            sr.color = new Color(0.45f, 0.95f, 0.55f);
                            sr.sortingOrder = -80;
                            var bc = e.AddComponent<BoxCollider2D>();
                            bc.isTrigger = true;
                            bc.size = new Vector2(0.8f, 0.8f);
                            bc.offset = Vector2.zero;
                            e.AddComponent<LevelExit>();
                            break;
                        }
                        case 'J':
                        case '1':
                        case '2':
                        {
                            string rn = ch == '2' ? c.recipe2 : c.recipe;
                            var j = Spawn(P + "Props/Jar.prefab", jarRoot, pos, "Jar_" + rn.Replace("Recipe_", ""));
                            var recipe = Load<RecipeData>(D + "Recipes/" + rn + ".asset");
                            Set(j.GetComponent<Jar>(), "recipe", p => p.objectReferenceValue = recipe);
                            break;
                        }
                        case 'L':
                            Spawn(P + "Level/Lantern.prefab", lights, pos, "Lantern");
                            break;
                        case '|':
                        {
                            // props 1 tile: pivot di tepi bawah tile, collider di tengah tile
                            var root = PropRoot();
                            doors.Add(Spawn(P + "Level/Door.prefab", root, new Vector3(x + 0.5f, y, 0f), "Door").GetComponent<Door>());
                            break;
                        }
                        case '~':
                            Spawn(P + "Level/Crawlspace.prefab", PropRoot(), new Vector3(x + 0.5f, y, 0f), "Crawlspace");
                            break;
                        case 'A':
                        case 'X':
                        {
                            var root = PropRoot();
                            string pf = ch == 'X' ? "Vent_Exit" : "Vent";
                            Spawn(P + "Level/" + pf + ".prefab", root, new Vector3(x + 0.5f, y, 0f), pf);
                            break;
                        }
                        case 'm':
                        case '=':
                        {
                            var z = new GameObject(ch == 'm' ? "Mud" : "Puddle");
                            z.transform.SetParent(PropRoot(), false);
                            z.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);   // sprite tile pivot tengah
                            var sr = z.AddComponent<SpriteRenderer>();
                            sr.sprite = floorTile.sprite;
                            sr.sharedMaterial = envMat;
                            sr.color = ch == 'm' ? new Color(0.45f, 0.32f, 0.2f) : new Color(0.35f, 0.55f, 0.9f);
                            sr.sortingOrder = -95;
                            var bc = z.AddComponent<BoxCollider2D>();
                            bc.isTrigger = true;
                            bc.size = new Vector2(0.9f, 0.9f);
                            if (ch == 'm') z.AddComponent<MudZone>(); else z.AddComponent<WashZone>();
                            break;
                        }
                        case 'n':
                        {
                            if (c.friends == null || hubSlot >= c.friends.Length) break;
                            var data = Load<RecipeData>(D + "Recipes/" + c.friends[hubSlot++] + ".asset");
                            var go = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(P + "Characters/FriendSlime.prefab"), PropRoot());
                            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                            go.name = "Hub_" + data.FriendName;
                            go.transform.position = pos;
                            Object.DestroyImmediate(go.GetComponent<FriendSlime>());
                            var hf = go.AddComponent<HubFriend>();
                            Set(hf, "friend", q => q.objectReferenceValue = data);
                            Set(hf, "bodyRenderer", q => q.objectReferenceValue = go.transform.Find("Friend_Body").GetComponent<SpriteRenderer>());
                            break;
                        }
                        case 'q':
                            Spawn(P + "Level/FragileVase.prefab", PropRoot(), new Vector3(x + 0.5f, y, 0f), "FragileVase");
                            break;
                        case 'Y':
                            Spawn(P + "Level/AuraValve.prefab", PropRoot(), new Vector3(x + 0.5f, y, 0f), "AuraValve");
                            hasFinal = true;
                            break;
                        case 'R':
                            Spawn(P + "Level/KingJar.prefab", PropRoot(), new Vector3(x + 0.5f, y, 0f), "KingJar");
                            hasFinal = true;
                            break;
                        case 'c':
                            Spawn(P + "Level/CreakyFloor.prefab", PropRoot(), new Vector3(x + 0.5f, y, 0f), "CreakyFloor");
                            break;
                        case 'B':
                            Spawn(P + "Level/PushCrate.prefab", PropRoot(), new Vector3(x + 0.5f, y, 0f), "PushCrate");
                            break;
                        case 'z':
                            breakPoints.Add(pos);
                            break;
                        case 'T':
                        case 'P':
                        {
                            var root = PropRoot();
                            string pf = ch == 'P' ? "HidingSpot_Crate" : "HidingSpot_Barrel";
                            Spawn(P + "Level/" + pf + ".prefab", root, new Vector3(x + 0.5f, y, 0f), pf);
                            break;
                        }
                        case '/':
                        {
                            var root = PropRoot();
                            var lv = Spawn(P + "Level/Lever.prefab", root, pos, "Lever").GetComponent<Lever>();
                            switches.Add((lv, new Vector2(x + 0.5f, y + 0.5f)));
                            break;
                        }
                        case 'o':
                        case 'O':
                        {
                            var root = PropRoot();
                            string pf = ch == 'O' ? "PressurePlate_Heavy" : "PressurePlate";
                            var pl = Spawn(P + "Level/" + pf + ".prefab", root, new Vector3(x + 0.5f, y, 0f), pf).GetComponent<PressurePlate>();
                            switches.Add((pl, new Vector2(x + 0.5f, y + 0.5f)));
                            break;
                        }
                        default:
                            if (uniqueStart.ContainsKey(ch)) uStart[ch] = pos;
                            else if (uniqueEnd.TryGetValue(ch, out var sk)) uEnd[sk] = pos;
                            else if (uniqueSingle.TryGetValue(ch, out var single)) uSingles.Add((single, pos));
                            else if (golemKeys.IndexOf(ch) >= 0) golemStart[ch] = pos;
                            else if (golemKeys.IndexOf(char.ToLowerInvariant(ch)) >= 0) golemEnd[char.ToLowerInvariant(ch)] = pos;
                            else if (ingredients.TryGetValue(ch, out var id))
                            {
                                var data = Load<IngredientData>(D + "Ingredients/Essence_" + id + ".asset");
                                var p = Spawn(P + "Props/IngredientPickup.prefab", ingRoot, pos, "Essence_" + id);
                                Set(p.GetComponent<IngredientPickup>(), "ingredient", q => q.objectReferenceValue = data);
                                p.GetComponent<SpriteRenderer>().color = data.PlaceholderTint;
                            }
                            break;
                    }
                }
            }

            int gi = 0;
            foreach (char key in golemKeys)
            {
                if (!golemStart.TryGetValue(key, out var a)) continue;
                var b = golemEnd.TryGetValue(key, out var e) ? e : a;
                string kind = key == 'f' || key == 'v' ? "Familiar" : key == 'd' ? "Anjing" : "Golem";
                var g = Spawn(P + "Enemies/" + kind + "_Placeholder.prefab", guardRoot, a, kind + "_" + (++gi));
                Set(g.GetComponent<Guard>(), "patrolPoints", p =>
                {
                    p.arraySize = 2;
                    p.GetArrayElementAtIndex(0).vector2Value = a;
                    p.GetArrayElementAtIndex(1).vector2Value = b;
                });
            }

            // musuh unik: patroli 2 titik, atau diam di tempat
            foreach (var kv in uStart)
            {
                var b = uEnd.TryGetValue(kv.Key, out var e) ? e : kv.Value;
                var g = Spawn(P + "Enemies/" + uniqueStart[kv.Key] + "_Placeholder.prefab", guardRoot, kv.Value, uniqueStart[kv.Key] + "_" + (++gi));
                Set(g.GetComponent<Guard>(), "patrolPoints", q =>
                {
                    q.arraySize = 2;
                    q.GetArrayElementAtIndex(0).vector2Value = kv.Value;
                    q.GetArrayElementAtIndex(1).vector2Value = b;
                });
            }
            foreach (var (kind, sp) in uSingles)
            {
                var g = Spawn(P + "Enemies/" + kind + "_Placeholder.prefab", guardRoot, sp, kind + "_" + (++gi));
                Set(g.GetComponent<Guard>(), "patrolPoints", q => { q.arraySize = 1; q.GetArrayElementAtIndex(0).vector2Value = sp; });
            }

            if (hasFinal)
            {
                var fin = new GameObject("FinalSequence");
                fin.transform.SetParent(PropRoot(), false);
                fin.AddComponent<FinalSequence>();
            }

            // shift jaga: tiap titik istirahat dipasang ke penjaga terdekat
            foreach (var bp in breakPoints)
            {
                Guard nearestGuard = null;
                float bestG = float.MaxValue;
                foreach (var g in guardRoot.GetComponentsInChildren<Guard>())
                {
                    float dg = Vector2.Distance(bp, g.transform.position);
                    if (dg < bestG) { bestG = dg; nearestGuard = g; }
                }
                if (nearestGuard == null) continue;
                var gs = nearestGuard.GetComponent<GuardShift>() ?? nearestGuard.gameObject.AddComponent<GuardShift>();
                Set(gs, "breakPoint", q => q.vector2Value = bp);
            }

            // sambungkan tiap tuas/pelat ke pintu terdekat
            foreach (var (comp, sp) in switches)
            {
                Door nearest = null;
                float best = float.MaxValue;
                foreach (var d in doors)
                {
                    float dd = Vector2.Distance(sp, (Vector2)d.transform.position + Vector2.up * 0.5f);
                    if (dd < best) { best = dd; nearest = d; }
                }
                if (nearest == null) { Debug.LogWarning("Blockout: " + comp.name + " tidak punya pintu untuk disambung."); continue; }
                Set(comp, "doors", p =>
                {
                    p.arraySize = 1;
                    p.GetArrayElementAtIndex(0).objectReferenceValue = nearest;
                });
            }

            var sys = Spawn(P + "Systems/LevelSystems.prefab", null, Vector3.zero, "LevelSystems");
            Set(sys.GetComponent<LevelGoal>(), "nextScene", p => p.stringValue = c.next);

            var boundsGo = new GameObject("CameraBounds");
            var bounds = boundsGo.AddComponent<BoxCollider2D>();
            bounds.isTrigger = true;
            bounds.offset = new Vector2(w * 0.5f, h * 0.5f);
            bounds.size = new Vector2(w, h);

            var cam = Camera.main;
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            Set(follow, "bounds", p => p.objectReferenceValue = bounds);
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f);

            // mood gelap (disetujui 10 Nov): yang terbaca hanya sumber cahaya. Light2D lewat SerializedObject
            // supaya editor asmdef tidak perlu referensi ke assembly URP 2D.
            foreach (var comp in Find(scene, "Global Light 2D").GetComponents<Component>())
                if (comp != null && comp.GetType().Name == "Light2D")
                    Set(comp, "m_Intensity", p => p.floatValue = 0.1f);

            var note = Object.FindFirstObjectByType<SceneNote>();
            if (note != null) Set(note, "note", p => p.stringValue = c.note);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Blockout selesai: " + c.scene);
        }
    }
}
