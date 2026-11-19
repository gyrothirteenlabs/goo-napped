# Backlog fitur (v2)

Status: ✅ selesai (prototipe) · 🔧 sedang dikerjakan · ⬜ belum. Sumber desain: `docs/desain-v2.md` (urutan kerja bagian 17).
Branch: `feat/redesign-lore`. Backlog v1 (resep 2–3 ciri, telan teman, nyamar bebas) ada di riwayat git `main`.
Ruang uji semua sistem v2: `Scenes/Sandbox/ProtoV2` (menu GooNapped > Blockout > Sandbox ProtoV2). Alur game sementara:
Menu → Intro → L01 … L11 (Markas di antaranya) → Ending.

## Inti v2 (selesai 11 Nov)
| | Fitur | Catatan |
|---|---|---|
| ✅ | Gerak baru: lompat rendah ala Terraria (busur ±5 px, ancang–memanjang–gepeng, kecepatan halus, napas saat diam) | `SlimeMotor`. Rasa belum dinilai manusia. Frame sprite `move` lama dimatikan (`SlimePiece.spriteMoveFrames`) |
| ✅ | Massa 1–4: kecepatan, ukuran, jarak terlihat, langkah berisik, muat celah (hanya massa 1) | `SlimeBodyState.Mass` |
| ✅ | 7 esens (nama Inggris), aura = esens terakhir, efek samping tetap, Esens Bayang baru (sulit terlihat) | `Data/Ingredients/Essence_*` |
| ✅ | Belah maks 2; esens terbaru ikut potongan baru; Space di depan toples = membelah langsung ke toples | `PlayerBrain.InsertIntoJar` |
| ✅ | Toples v2: ukuran 1/2, pendar aura terlihat dari jauh, aura salah = curiga lebih cepat, alarm penyadapan 90/30 dtk | `Jar` |
| ✅ | Teman: keluar lalu menunggu, F ikut/tunggu, menyusuri jejak, sedikit lebih lambat, berbaris, menahan pelat | `FriendSlime`. Data 8 teman: `Data/Recipes/Friend_*` |
| ✅ | Penjaga: curiga penuh = mengejar, tangkap, teriak memanggil penjaga lain, alarm toples, transformasi terlihat = curiga melonjak | `Guard` |
| ✅ | Tempat sembunyi (C): tong muat 1, peti muat 2; ditatap = goyang lalu diperiksa; terlihat masuk = langsung diperiksa | `HidingSpot` |
| ✅ | Ventilasi: aman total, checkpoint, A/D pindah lubang (hanya ke lubang yang sudah pernah dimasuki, supaya tidak jadi jalan pintas ke depan; tersimpan di checkpoint), teman ikut masuk = selamat, lubang exit | `Vent`, `Checkpoint` |
| ✅ | Lumpur (jejak bau beberapa detik) + genangan (mencuci) | `MudZone`, `WashZone` |
| ✅ | Teks game bahasa Inggris (HUD, pesan, menu, pause, cerita) | |
| ✅ | QA otomatis v2: solver tiruan, validator level v2 (+ rute kawal tanpa celah), bot lewat ventilasi exit | EditMode 37 + PlayMode 5 lulus |

## Berikutnya
| | Fitur | Catatan |
|---|---|---|
| ✅ | Kekuatan teman dari dalam ventilasi: Shift pilih, A/D pilih lubang, E pakai. Dentum, Padam (10 dtk), Umpan Kristal (dikejar lalu pecah), Kabut (6 dtk). Terbuka saat teman pembawanya selamat (PlayerPrefs `gn_powers`), jatah isi ulang tiap checkpoint | `FriendPowers`, `Decoy`, `MistCloud`. Di ProtoV2: selamatkan Bolu = Dentum |
| ✅ | Markas antar level: teman yang sudah selamat duduk di kursinya (PlayerPrefs `gn_rescued`), E = bicara (`RecipeData.HubLines`, placeholder), ventilasi exit = level berikutnya. Level terakhir langsung ke Ending | scene `Story/Markas` (GooNapped > Blockout > Markas), `HubFriend`, `RescueLog`, `GameFlow.LoadAfterLevel` |
| ✅ | Pergantian shift jaga: penjaga bertugas N dtk lalu istirahat di titik tertentu, pos kosong sebentar (menunggu tenang dulu kalau sedang curiga) | `GuardShift`, legenda `z` |
| ✅ | Barang rapuh (tersenggol = pecah + bunyi), lantai berderit (tubuh besar/berat/teman besar = bunyi), peti dorong (massa >= 2, per tile, menekan pelat, menutup pandangan) | `FragileItem`, `CreakyFloor`, `PushCrate`, legenda `q c B`. Uji: `Sandbox/ProtoInteract` |
| ✅ | Musuh unik (perilaku + prefab placeholder): Lukisan Bermata (diam, menyapu, alarm), Monster Gerbang (diam, sorot = tertangkap), Zirah Hampa (buta, pendengaran x1.8), Mata Penyihir (periksa tempat sembunyi + bongkar toples aura salah), Pelayan Tudung (periksa tempat sembunyi), Alkemis (pemburu tiap 9 dtk) | `Guard` (Perilaku khusus), `Prefabs/Enemies/*_Placeholder`, legenda `p @ 3-4 5-6 7-8 9-0`, uji `Sandbox/ProtoEnemies`. **Sprite final: tim** |
| ✅ | Blockout 11 level v2 (L01_Gate s/d L11_Summit, sesuai daftar usulan desain-v2 bagian 3), alur Intro > L01 > Markas > ... > L11 > Ending. Lolos validator + bot tamat semua | `Scenes/Levels/V2`, menu GooNapped > Blockout > Semua level v2. Peta diperbesar (12 Nov) jadi 44 kolom x 14–18 baris, luas lantai sekitar 2x: konsep tiap level sama, ruang dan rute lebih panjang, tempat sembunyi lebih banyak. Golem shift jaga L07 sekarang menjaga pos pendek dekat exit. Layout final dan ukuran: tim desainer. |
| ✅ | Adegan final L11: putar balik 3 katup aura (E, diam 1,2 dtk, berisik), toples raja terbuka, Alkemis mengamuk (berburu tiap 3 dtk, 1,25x cepat). Tinggalkan potongan di toples raja: Alkemis menerkamnya di dalam = tersegel, dialog raja, lalu Ending. Katup tersimpan di checkpoint | `AuraValve`, `KingJar`, `FinalSequence`, `Guard.isBoss`, legenda `Y R`. Teks dialog placeholder: tim |
| ✅ | Art fondasi sementara (AI, gaya gambar konsep): tileset + dekor taman kastil (rumput, tanah, lumpur/air, pagar tanaman, pohon, semak) dan penjara (lantai batu, tembok bertutup + lumut/rantai/jendela, dekor), props (tong, peti, obor, ventilasi, toples kaca, pintu sel, rak ramuan). Ditambah kastil atas (lantai kayu, plester anggur + list emas, lukisan/lilin/panji/jendela; lemari, tudung saji, panci, tirai, vas, kandelabra) dan puncak (panel mesin ungu, pipa aura; katup, toples raja 96 px, mesin penyadap). Semua 11 level + Markas memakai art area | `art/tools/gen_area_art.py`, `AreaArt`, art/CLAUDE.md. **Final: tim** |
| ✅ | Dekor area otomatis: karpet runner (maks 2 baris, rata bawah) di ruangan/koridor L07–L10, kandelabra (kastil/puncak) dan rak ramuan (penjara) di depan tembok tanpa cahaya, mesin penyadap di belakang toples raja L11, jalan tanah taman dari spawn ke ventilasi tujuan L01–L03 | `LevelBlockout` (CarpetCells, PathCells, WallProps, DrainMachine) |
| ✅ | Nama + logo + ikon SEMENTARA: "Goo-napped" (huruf pixel slime menetes, O kedua jadi wajah slime), dipasang saat build (nama produk, tim, versi 0.2.0-jam, ikon) tanpa mengubah ProjectSettings; logo di main menu | `art/tools/gen_logo.py`, `Branding.cs`. **Nama/logo final: tim** |
| 🔧 | Isi cerita (strip, dialog, catatan). Sementara (15 Nov): strip Intro 5 panel + Ending 4 panel dirakit dari sprite game + teks Inggris. Dialog markas/catatan level belum | `gen_story_strips.py`, `Story_Intro/Ending`. **Final: tim** |
| ✅ | Witch Eye dari model tim (15 Nov, revisi): model asli + tengkorak pendamping (material tulang, lubang gelap), hanya perbaikan teknis/keterbacaan, animasi idle/move/search 3 arah, lensa monokel menyala. Guard memakai clip `search` saat mencari/memeriksa | `art/blender/witcheye_v02.blend`, `Sprites/Enemies/WitchEye`, `docs/review/witcheye/` |
| ✅ | Teman yang dikawal: hadap sesuai arah gerak (front/side/back, diam = menghadap pemain), lompatan jeli seperti pemain (busur + squash), napas, gemetar saat menunggu, bayangan | `FriendSlime`, prefab FriendSlime (animator + SlimeJelly) |
| ✅ | Pembuka level sinematik (LevelIntro): masuk lewat alur game = kamera pan/zoom ke titik penting + dialog, letterbox, HUD disembunyikan, penjaga buta; tutorial: sorot ruang/toples/golem/ventilasi, slime jatuh dari pipa lalu melompat masuk, baru tip WASD. Level lain: sorot ventilasi tujuan + objektif, lalu kalimat pembuka (LevelCatalog.intro). Tidak diputar saat Retry/checkpoint, Esc = lewati | `LevelIntro`, `LevelBlockout` (intro), `LevelCatalog`, `LevelIntroTests` |
| ✅ | Cutscene gaya Zelda SNES + teks tidak scroll; tutorial pakai teman sendiri (Dot) supaya Pip tidak diselamatkan dua kali; input tidak tembus saat pause/resume (Space/E) | `StoryPlayer`, `Story.uss`, `Friend_Dot`, `PauseMenu.BlocksInput` |
| ✅ | Build Windows dari menu GooNapped > Build (scene alur ikut otomatis tanpa mengubah Build Settings), versi dev dengan log file + F1, smoke test build (`smoke_test.bat`, screenshot tiap scene). Fix: layar hitam setelah Intro di build (L01 tidak ikut di-build); scene yang hilang sekarang kembali ke menu + error di log | `GameBuild`, `DevLog`, `BuildSmokeTest`, docs cara-edit-unity bagian 12 |
| ✅ | Main menu placeholder "adegan hidup gelap": ruang lab, lentera, 3 toples teman berpendar, slime bergerak sendiri, menu UI Toolkit (keyboard + mouse), pilih level kartu 4 kolom, konfirmasi new game/reset | `MainMenuBuilder`, `MainMenu.uxml/uss`, `MainMenuController`, `MenuSlime`. **Tampilan final: tim** |
| ✅ | Pause + Options versi UI Toolkit (13 Nov): pause Resume/Retry/Options/Main Menu, Options di main menu + pause (volume musik, volume efek + ambience, fullscreen, resolusi; disimpan PlayerPrefs, layar hanya berlaku di build). Keyboard + mouse | `PauseMenu`, `OptionsPanel`, `GameSettings`, `UI/Options.uss`. **Tampilan final: tim** |
| ✅ | UI pixel tema dungeon (14 Nov): font Pixelify Sans (OFL), main menu miring + pita kain + obor, Select Level (jalur progres, nama terkunci glitch, preview bisa diklik), konfirmasi New Game, Options layar penuh + slime yang bereaksi ke volume, Pause baru, transisi (slash wipe, goo drip + kartu judul, retry, balik ke menu), tahan W/S + roda mouse di semua menu, HUD medali besi + potret slime (warna aura, besar sesuai massa). Tekstur placeholder dari GooNapped > UI > Generate Dungeon Textures. Desain di Figma "Goo-napped UI" | `UiFx`, `MenuInput`, `LevelCatalog`, `SlimeMusicArt`, `SlimePortrait`, `UiTextureGen`, `UI/*.uss`. **Tampilan final: tim** |
| ✅ | Props level + rapih layout (16 Nov): perabot penghalang `t` dan pilar `I` per area (taman: sumur, bangku, bak bunga, patung slime raja; penjara: meja, kandang, meja panjang, pilar berantai; lab L06: kuali, rak tabung, meja kerja, tabung spesimen; kastil: meja bundar, rak buku, meja makan panjang, tiang marmer; puncak: konsol, generator, saluran pipa, tiang pipa aura). Menghalangi jalan + pandangan + cahaya = bayangan untuk menyelinap. Serakan lantai tanpa collider (tulang, ember, jerami, kertas, batu, daun, nisan), rantai/pipa di tembok. Ditempatkan di 11 level + tutorial tanpa memperpanjang rute atau patroli (dicek skrip). Fix: ruang kanan bawah L11 dulu tertutup total, sekarang ada pintu; tembok nyasar di pojok L06 dihapus. Lolos validator + bot | `art/tools/gen_level_props.py`, `AreaArt` (block/pillar/clutter), `LevelBlockout` (legenda `t I`), `docs/review/level-props/`. **Art final: tim** |
| ✅ | Fix QA 19 Nov: slime terlihat dobel (batu samaran sisa v1 ikut menyala) setelah pembuka sinematik; dialog Raja L11 menyela pembuka; HUD/pause/kartu judul L11 menulis "reach the vent" padahal objektif = katup aura. Test pembuka sekarang jalan di semua level + cek tampilan slime + screenshot | `LevelIntro`, `FinalSequence`, `StoryTrigger`, `GameHud`, `LevelIntroTests` |
| ⬜ | Build Settings ke daftar level v2 (opsional, build lewat GooNapped > Build sudah lengkap) | ProjectSettings = keputusan tim |

## Art & audio (tim desainer)
| | Fitur |
|---|---|
| ⬜ | Slime final + teman (sprite `move` sesuai panduan-blender-3d C.1) |
| ⬜ | Palet esens final (Slime_Embun/Belerang/Beku/Bayang/Pendar masih placeholder AI) |
| ⬜ | Tileset 3 area + puncak, props tempat sembunyi, ventilasi, toples 2 ukuran |
| ⬜ | Sprite musuh v2, audio final (tegang, kejar, alarm) |

## Angka yang perlu disetel lewat playtest
- Gerak: `SlimeMotor` (busur, waktu fase, groundSpeedShare).
- Kecepatan kejar: Golem 1.6, Familiar 2.1, Anjing 2.2 unit/dtk (pemain massa 1 = 2.2).
- Curiga tempat sembunyi 3.5 dtk ditatap, alarm 90/30 dtk, lumpur 7 dtk.
