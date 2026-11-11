# Cara edit di Unity (dan hubungannya dengan Blender)

Resep langkah demi langkah untuk pekerjaan yang paling sering. Detail sistem: [arsitektur.md](arsitektur.md).

> Sebelum edit scene: klaim di chat tim (scene tidak bisa di-merge kalau dua orang mengedit bersamaan). Kerja di prefab kalau bisa.

---

## 1. Bikin / ubah level

### Cara cepat: peta teks (blockout)
1. Buka `Assets/_Game/Scripts/Editor/Level/LevelBlockout.cs`. Contoh lengkap v2: config `SandboxV2` (scene `Sandbox/ProtoV2`).
2. Ubah peta (satu karakter = satu tile 48 px):

   | Huruf | Arti |
   |---|---|
   | `#` | batu / tembok (collider + bayangan) |
   | `.` | lantai |
   | `S` | spawn pemain |
   | `X` | lubang ventilasi **exit** (level selesai setelah semua teman selamat) |
   | `A` | lubang ventilasi (aman, checkpoint, jaringan "A": A/D pindah lubang) |
   | `E` | exit lama (trigger `LevelExit`, level v1) |
   | `J` atau `1` | toples teman pertama (`recipe` di config, aset `Friend_*`) |
   | `2` | toples teman kedua (`recipe2`) |
   | `b` `l` `w` `r` `y` `i` `u` | Esens Batu, Pendar, Embun, Bara, Belerang, Beku, Bayang |
   | `T` / `P` | tempat sembunyi: tong (muat 1) / peti (muat 2) |
   | `m` / `=` | lumpur (jejak bau) / genangan (mencuci lumpur) |
   | `L` | lentera |
   | `g`/`G`, `h`/`H`, `k`/`K` | titik awal/akhir patroli Golem 1, 2, 3 |
   | `f`/`F`, `v`/`V` | titik awal/akhir patroli Familiar Api 1, 2 |
   | `d`/`D` | titik awal/akhir patroli Anjing Lendir |
   | `\|` | pintu (tertutup) |
   | `/` | tuas (E: buka/tutup pintu, berisik) |
   | `o` / `O` | pelat tekan / pelat berat (tubuh berat: Esens Batu atau massa >= 3, atau teman besar menunggu di atasnya) |
   | `~` | celah (slime massa 1 lewat, penjaga tidak) |

   Tuas dan pelat otomatis tersambung ke **pintu terdekat**. Jangan taruh celah di rute kawal teman (validator mengeceknya).

3. Unity: menu **GooNapped > Blockout > ...**. Scene diisi ulang dan disimpan.
4. Level baru: salin satu `Config`, ganti `scene`, `recipe`, `next`, `note`, tambah `[MenuItem]`, lalu tambahkan nama scene di `GameFlow.Levels`. Di editor scene di luar Build Settings tetap bisa dimuat; sebelum build Windows, Build Settings harus diperbarui (keputusan tim).

### Cara manual (desain final)
- **Lantai/tembok**: Tile Palette, gambar di Tilemap `Grid/Floor` dan `Grid/Walls`. Penjaga mencari jalan dari dua Tilemap ini.
- **Objek** (drag prefab):
  - `Prefabs/Props/Jar` → isi `Recipe` dengan aset `Friend_*`. `Alarm Delay` / `Wrong Alarm Delay` (detik), `Glow Material` = `Vfx_Particle`.
  - `Prefabs/Props/IngredientPickup` → isi `Ingredient` dengan aset `Essence_*`.
  - `Prefabs/Level/Vent` / `Vent_Exit` → `Network` (lubang dengan nama sama tersambung; exit default jaringan "Exit" supaya tidak bisa dicapai lewat jalan pintas), `Exits Level`, `Saves Checkpoint`.
  - `Prefabs/Level/HidingSpot_Barrel` / `HidingSpot_Crate` → `Capacity`, `Stare Fill Time` (detik ditatap sampai diperiksa), `Crowd Multiplier` (berdua lebih cepat).
  - Lumpur / genangan: objek trigger + `MudZone` (`Muddy Seconds`) / `WashZone`.
  - `Prefabs/Enemies/*_Placeholder` → `Patrol Points`.
  - `Door`, `Lever`, `PressurePlate`, `Crawlspace`, `Lantern`: seperti v1 (pelat juga ditekan teman yang menunggu).
  - Sprite props masih placeholder (`Art/Sprites/Props/*_placeholder.png`). Buat ulang: **GooNapped > Placeholder > Props level**.
- **Sistem**: satu `Prefabs/Systems/LevelSystems` per level, isi `LevelGoal > Next Scene`.
- **Posisi aktor**: `(x + 0.5, y + 0.28)`. Props 1 tile: `(x + 0.5, y)`.

### Uji
Play di scene level. HUD: ukuran (massa), aura, ciri lain, status (LIT, GLOWING, SMELLY, MUDDY, SLOW, NOISY, HIDDEN), kartu toples (ukuran + aura cocok?), status teman, alarm.

---

## 2. Esens baru
1. `Assets/_Game/Data/Ingredients/` > **Create > GooNapped > Bahan (Ingredient)**, nama `Essence_<Nama>`, `Display Name` bahasa Inggris.
2. Isi: **Traits** (Warna = aura, wajib; ciri lain = efek), `Speed Multiplier`, `Emits Light`, `Smell Radius`, `Extinguishes`, `Step Noise Radius`, `Sight Multiplier` (0.7 = sulit terlihat), `Body Material` (palet aura).
3. Nama aura di layar diambil dari warna (`TraitNames`): abu Stone, hijau Glow, biru Dew, merah Ember, kuning Sulfur, putih Frost, ungu Shade.

## 3. Teman (toples)
1. `Assets/_Game/Data/Recipes/` > **Create > GooNapped > Teman (Friend)**, nama `Friend_<Nama>`.
2. Isi: nama (Inggris), **tepat 1 Trait Warna** (aura), `Size` 1 (kecil) atau 2 (besar), `Power` (kekuatan setelah selamat), `Jar Line` (kalimat saat didekati), material palet.
3. Tiruan butuh massa = `Size` (pemain massa `Size + 1`), esens terakhir = aura.

## 4. Palet warna slime per aura (desainer)
Material `Art/Materials/Slime_*` (shader `GooNapped/PixelToonLit`): `Band 1..4` gelap ke terang, `Outline`, `Min Light`.
Slime_Embun, Slime_Belerang, Slime_Beku, Slime_Bayang, Slime_Pendar dibuat AI sebagai placeholder: silakan diganti.

## 5. Menyetel penjaga
Inspector `Guard`:

| Bagian | Field penting |
|---|---|
| Jenis | `Guard Name` (Inggris, untuk pesan "Caught by ..."), `Needs Light To See`, `Heat Sight Range`, `Hover Height` |
| Penciuman | `Smell Range`, `Smell Rate` (jejak bau dan lumpur) |
| Patroli | `Patrol Points`, `Speed`, `Wait At Point` |
| Penglihatan | `View Range`, `View Angle`, `Awareness Radius` |
| Cahaya | `Light Sight Range`, `Lit Threshold`, `Light Rate` |
| Curiga | `Fill Time`, `Decay Per Second`, `Search Threshold`, `Look Around Time`, `Transform Suspicion` (lonjakan saat melihat transformasi), `Alarm Fill Multiplier` |
| Mengejar | `Chase Speed Multiplier` (Golem 1.8, Familiar 1.5, Anjing 1.45), `Lose Sight Time`, `Catch Distance`, `Call Radius` (teriakan) |
| Tempat sembunyi | `Stare Range`, `Inspects Hiding Spots` (pemeriksa), `Inspect Radius`, `Inspect Time`, `Reinspect Delay` |

## 6. Menyetel rasa gerak slime
Prefab `Characters/Slime_Player`:
- **`SlimeMotor`**: `Base Speed` (2.55 = rata-rata ±2.2 unit/dtk), `Ground Speed Share`, `Velocity Smoothing`, waktu fase (`Crouch`, `Hop`, `Settle`, `Stop`), `Hop Arc` (tinggi busur), `Steer In Air`, squash/stretch, `Breathe`.
- **`SlimeJelly`**: `Stiffness`, `Damping`, `Max Stretch`.
- **`SlimePiece`**: `Sprite Move Frames` (nyalakan setelah sprite final dirender), `Eat Duration`, `Enter Time` / `Exit Time` (tempat sembunyi).
- Massa mengubah kecepatan/ukuran/jarak terlihat lewat tabel di `SlimeBodyState` (`MassSpeed`, `MassScale`, `MassSight`).
- `LevelSystems > PlayerBrain`: `Max Pieces` (2), jeda aksi, `Hide Radius`, `Friend Join Radius`.

## 7. Tampilan UI (desainer)
- **HUD**: `Assets/_Game/UI/Hud.uss`. Ubah warna, ukuran font, padding, sudut panel di sini (format mirip CSS). Kelas penting: `.panel`, `.chip`, `.tag--light|glow|smell|slow|hidden`, `.recipe-card`, `.recipe-dot--ok|miss`, `.banner--win|fail`.
- Posisi/susunan: `Hud.uxml` (buka dengan UI Builder: Window > UI Toolkit > UI Builder). Jangan ganti `name` elemen (dipakai `GameHud.cs`).
- Skala keseluruhan: `HudPanelSettings.asset` > Reference Resolution (besar = UI lebih kecil).
- Font pixel: taruh font di `Art/Fonts/`, set `-unity-font-definition` di `.hud-root`.
- Ikon di atas penjaga: komponen `GuardMeter` di prefab penjaga, isi `Question Sprite` / `Exclaim Sprite` dengan sprite sendiri (kosong = ikon placeholder dari kode).

## 8. Cerita: cutscene & dialog
Sistem siap pakai; isi cerita ditulis tim. Teks dan gambar tinggal diganti, tanpa kode.

**Bikin cerita baru**: `Assets/_Game/Data/Story/` > klik kanan > **Create > GooNapped > Cerita (Story Sequence)**. Isi `Pages`; tiap halaman:

| Field | Isi |
|---|---|
| Layout | `ImageLeft` (gambar tinggi kiri, baris teks muncul bertumpuk di kanan), `ImageTop` (gambar lebar atas, teks bawah), `ImageOnly` (gambar saja), `Dialogue` (kotak dialog bawah dengan potret, game tetap terlihat) |
| Image | gambar cutscene (taruh di `Art/Story/`, import Sprite Single + Point) |
| Lines | baris teks: `Speaker` (opsional), `Text`, `Portrait` (untuk Dialogue) |
| Auto Advance | > 0 = lanjut sendiri setelah sekian detik |

Opsi urutan: `Pause Game` (penjaga berhenti), `Skippable` (Esc melewati). Contoh semua layout: `Story_Contoh.asset`. Placeholder: `Story_Intro`, `Story_Ending`.

**Memutar cerita**
- Scene cerita penuh (Intro, Ending): komponen `StoryScene` (cerita + scene berikutnya). Intro diputar saat **Mulai** permainan baru; Ending sesudah level terakhir.
- Di level: objek dengan `StoryTrigger`: `OnStart` (pembuka level), `OnEnter` (masuk area, butuh Collider2D Is Trigger), `Manual` (dipanggil event lain). Ada event `On Finished`.
- Dari kode, kapan saja: `StoryPlayer.Play(sequence, () => { ... })` atau dialog cepat `StoryPlayer.Say("Nama", "Teks", potret)`.
- Scene harus punya prefab `Prefabs/UI/StoryUI` (sudah ada di LevelSystems, Intro, Ending).

Kontrol: E / Space / Enter / klik = lanjut (menekan saat teks diketik = tampilkan seluruh baris), Esc = lewati. Tampilan: `Assets/_Game/UI/Story.uss` (font pixel: `-unity-font-definition` di `.story-root`).

## 9. Suara
- Komponen **`AudioDirector`** di prefab `Systems/LevelSystems`: drag klip ke slot `Music`, `Ambience`, dan tiap efek (langkah slime, langkah golem, makan, belah, gabung, nyamar, Shift, waspada, ketahuan).
- Kretek obor: `AudioSource` di prefab `Level/Lantern`.
- Klip `proto_*` adalah placeholder hasil sintesis (**GooNapped > Audio > Buat suara placeholder**). Klip asli: taruh di `Assets/_Game/Audio/Music|SFX/` (WAV/OGG), ganti referensinya. Kode tidak perlu diubah.

## 10. Sprite dari Blender
1. Desainer 3D bikin model di template Blender: [panduan-blender-3d.md](panduan-blender-3d.md) (render 1 klik → `art/sprites/<nama>/`).
2. Copy sheet ke `Assets/_Game/Art/Sprites/<Characters|Enemies|Props>/<Nama>/`. Slice 48 px, pivot, dan normal map terpasang otomatis.
3. Karakter slime: drag frame ke `SlimeSpriteAnimator > Clips` (anim + arah + frame body/face) di prefab `Slime_Player`. Spek jumlah frame tiap anim: panduan Blender bagian C.1.
4. Musuh/prop: material `Env_Lit` untuk albedo, `Enemy_Glow_Unlit` untuk bagian menyala.

## 11. Test & QA otomatis
Window > General > **Test Runner**. Wajib hijau sebelum PR, terutama setelah mengubah level, prefab, atau data.

**EditMode** (± 1 detik, tanpa Play):
| Test | Mengecek |
|---|---|
| `LevelValidationTests.Level_Valid` (per level + `Sandbox/ProtoProps`) | isi wajib (pemain, toples, exit, LevelSystems, kamera), `nextScene` sesuai urutan `GameFlow.Levels`, objek tidak di dalam tembok, **resep bisa dibuat dari bahan yang ada** (urutan makan dicari otomatis, termasuk muntah), jalan spawn → bahan → toples → exit bisa ditempuh (ikut aturan tubuh berat + celah), ruang untuk belah di samping toples, patroli penjaga di lantai dan tersambung, spawn tidak langsung terlihat penjaga, tuas/pelat tersambung |
| `AssetIntegrityTests` | script hilang, referensi aset rusak, field wajib kosong (prefab + semua scene alur game), data bahan/resep lengkap (2–3 ciri, tidak dobel) |
| `AlurGame_SemuaSceneAdaDiBuild` | semua scene alur game ada di Build Settings |
| logika murni | `SlimeBodyState`, `VisionMath`, `ScentTrail`, `GridPath`, solver resep |

**PlayMode** (± 4 menit, memainkan scene asli):
| Test | Mengecek |
|---|---|
| `Bot_MenyelesaikanLevel` (per level) | penjaga dimatikan, bot memainkan alur lengkap dengan gerak & collision asli (makan sesuai resep → belah → Shift → tukar → telan → exit). Gagal = level tidak bisa diselesaikan atau badan slime menyangkut. Console mencatat waktu tempuh bot. |
| `Penjaga_PatroliJalan_SpawnAman` (per level) | pemain diam 40 detik game: tidak ketahuan, tiap penjaga sampai ke semua titik patrolinya |
| `Alur_*` | Boot → MainMenu, Intro → level pertama, Ending → MainMenu, timeScale kembali normal |

**Laporan** (kategori `Laporan`, tidak ikut Run All, jalankan manual): `Laporan_PetaPengawasan` mencatat berapa persen rute terpendek diawasi penjaga dan sel mana yang tidak punya jeda aman. Pakai untuk menyetel kesulitan, bukan lulus/gagal.

Error/exception apa pun di console selama PlayMode test = test gagal. Progres pemain (PlayerPrefs) dikembalikan setelah test. Kode QA bersama (peta grid level, solver resep) ada di `Tests/Shared/`.

Yang **tidak** bisa dicek otomatis: rasa kontrol, keterbacaan visual, apakah stealth terasa adil/seru, kesulitan. Itu tetap butuh playtest manusia.
