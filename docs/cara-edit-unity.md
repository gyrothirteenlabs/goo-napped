# Cara edit di Unity (dan hubungannya dengan Blender)

Resep langkah demi langkah untuk pekerjaan yang paling sering. Detail sistem: [arsitektur.md](arsitektur.md).

> Sebelum edit scene: klaim di chat tim (scene tidak bisa di-merge kalau dua orang mengedit bersamaan). Kerja di prefab kalau bisa.

---

## 1. Bikin / ubah level

### Cara cepat: peta teks (blockout)
1. Buka `Assets/_Game/Scripts/Editor/Level/LevelBlockout.cs`, cari `C1L01` dsb.
2. Ubah peta (satu karakter = satu tile 48 px):

   | Huruf | Arti |
   |---|---|
   | `#` | batu / tembok (collider + bayangan) |
   | `.` | lantai |
   | `S` | spawn pemain |
   | `E` | exit |
   | `J` atau `1` | toples resep pertama (`recipe` di config) |
   | `2` | toples resep kedua (`recipe2` di config) |
   | `b` `l` `w` `r` `y` `i` | Serbuk Batu, Lumut Pendar, Air Embun, Ramuan Api, Minyak Belerang, Garam Beku |
   | `L` | lentera |
   | `g`/`G`, `h`/`H`, `k`/`K` | titik awal/akhir patroli Golem 1, 2, 3 |
   | `f`/`F`, `v`/`V` | titik awal/akhir patroli Familiar Api 1, 2 |
   | `d`/`D` | titik awal/akhir patroli Anjing Lendir |
   | `\|` | pintu (tertutup) |
   | `/` | tuas (E: buka/tutup pintu, berisik) |
   | `o` / `O` | pelat tekan / pelat tekan berat (butuh tubuh berat) |
   | `~` | celah / lorong tikus (slime bisa lewat, penjaga tidak, tubuh berat tidak muat) |

   Tuas dan pelat otomatis tersambung ke **pintu terdekat**. Atur peta supaya jelas, atau ubah daftar `Doors` di Inspector.
   Contoh pemakaian semua props: scene `Sandbox/ProtoProps` (menu **GooNapped > Blockout > Sandbox ProtoProps**).

3. Unity: menu **GooNapped > Blockout > C1_L0x** (atau **Semua chapter 1**). Scene diisi ulang dan disimpan.
4. Level baru: salin satu `Config`, ganti `scene`, `recipe`, `next`, `note`, tambah `[MenuItem]`. Lalu **tambahkan nama scene di `GameFlow.Levels`** (urutan pilih level) dan pastikan scene ada di Build Settings.

### Cara manual (desain final)
- **Lantai/tembok**: buka Tile Palette (Window > 2D > Tile Palette), gambar di Tilemap `Grid/Floor` dan `Grid/Walls`. `Walls` punya collider + bayangan otomatis. Penjaga mencari jalan dari dua Tilemap ini: sel bisa dilewati = ada tile `Floor` dan tidak ada tile `Walls`. Prop besar yang menghalangi jalan sebaiknya juga diberi tile `Walls` di bawahnya.
- **Objek**: drag prefab ke scene di bawah `Actors/…`:
  - `Prefabs/Props/Jar` → isi `Recipe` di Inspector
  - `Prefabs/Props/IngredientPickup` → isi `Ingredient`
  - `Prefabs/Enemies/Golem_Placeholder` → isi `Patrol Points` (posisi dunia)
  - `Prefabs/Level/Lantern` → di `Lights/` (punya `Extinguishable`: bisa dipadamkan slime bertubuh dingin)
  - `Prefabs/Level/Door` → pivot di tepi bawah tile, taruh di `(x + 0.5, y)`. `Starts Open`, `Noise Radius`. Tertutup = menghalangi slime, penjaga, dan pandangan; jalur penjaga otomatis memutar. Tidak menutup selama ada yang berdiri di ambangnya.
  - `Prefabs/Level/Lever` → isi `Doors`. E = toggle semua pintu di daftar, bunyinya (`Noise Radius`) memancing penjaga.
  - `Prefabs/Level/PressurePlate` / `PressurePlate_Heavy` → isi `Doors`. Pintu terbuka selama ada potongan (termasuk yang nyamar) di atas pelat. Pintu terbuka = tuas XOR pelat, jadi pelat juga bisa menutup pintu yang `Starts Open`.
  - `Prefabs/Level/Crawlspace` → satu tile di celah tembok. Slime lewat, penjaga tidak, tubuh berat tidak muat.
  - Gambar props masih placeholder (`Art/Sprites/Props/door_|lever_|plate_|plate_heavy_|crawlspace_placeholder.png`, frame 0/1). Ganti PNG dengan nama dan urutan frame sama; prefab ikut. Buat ulang: **GooNapped > Placeholder > Props level**.
- **Exit**: objek dengan `BoxCollider2D` (Is Trigger) + `LevelExit`.
- **Sistem**: satu `Prefabs/Systems/LevelSystems` per level. Isi `LevelGoal > Next Scene` (nama scene berikutnya, harus ada di Build Settings).
- **Kamera**: `Main Camera` punya `CameraFollow`. Isi `Bounds` dengan collider yang menutup seluruh level (`CameraBounds`).
- **Posisi aktor**: pivot sprite = tengah-bawah cell. Supaya kaki di tengah tile, taruh di `(x + 0.5, y + 0.28)`.
- **Tile baru**: sprite tile wajib di `Art/Sprites/Tiles/` (pivot otomatis tengah). Jangan geser tile lewat transform/anchor (ikut menggeser collider).

### Uji
Play di scene level. HUD kiri atas menampilkan tubuh, potongan, resep toples terdekat, bar curiga penjaga, dan peringatan "kena cahaya".

---

## 2. Bahan baru
1. Project: klik kanan `Assets/_Game/Data/Ingredients/` > **Create > GooNapped > Bahan (Ingredient)**, nama `Ingredient_<Nama>`.
2. Isi:
   - **Traits**: jenis + nilai (contoh: `Temperature = panas`, `Color = merah`). Ciri sejenis dari bahan sebelumnya tertimpa.
   - **Speed Multiplier**: 1 normal, 0.6 lambat.
   - **Emits Light**: menyala (terlihat lebih jauh, kena aturan cahaya, bisa jadi umpan).
   - **Smell Radius**: bau (meninggalkan jejak, dicium Anjing Lendir).
   - **Extinguishes**: memadamkan nyala bahan yang dimakan sebelumnya (Garam Beku).
   - **Step Noise Radius**: langkah berisik. Tiap mendarat, penjaga dalam radius ini datang memeriksa (tembus tembok). Serbuk Batu = 2.2, lainnya 0.
   - **Body Material**: palet `Slime_*` saat bahan ini menentukan warna (kosong = warna tidak berubah).
3. Taruh di level lewat `IngredientPickup`, atau tambah huruf baru di dictionary `ingredients` di `LevelBlockout`.

## 3. Resep teman
1. `Assets/_Game/Data/Recipes/` > **Create > GooNapped > Resep teman (Recipe)**, nama `Recipe_<NamaTeman>`.
2. Isi nama teman, 2–3 Traits, material palet teman.
3. Pasang di `Jar > Recipe`.

## 4. Palet warna slime per bahan (desainer)
Material `Art/Materials/Slime_*` memakai shader `GooNapped/PixelToonLit` dengan `Use Gradient` aktif:
- `Band 1..4` = warna dari sisi gelap ke terang (geser hue: bayangan agak biru, terang agak kuning).
- `Outline` = garis tepi.
- `Min Light` = terang minimum di tempat gelap (makin kecil makin tenggelam).

Bahan baru dengan warna baru: duplikat `Slime_Normal.mat`, ubah band, pasang di `Body Material` bahan.

## 5. Menyetel penjaga
Inspector `Guard` (Golem dan Familiar memakai komponen yang sama):

| Bagian | Field penting |
|---|---|
| Jenis | `Guard Name`, `Needs Light To See` (buta di gelap), `Heat Sight Range` (lihat panas dari jauh), `Hover Height` |
| Penciuman | `Smell Range` (0 = tidak mencium), `Smell Rate` |
| Patroli | `Patrol Points`, `Speed`, `Wait At Point` |
| Penglihatan | `View Range`, `View Angle` (lebar kerucut), `Awareness Radius` (radius sadar) |
| Cahaya | `Light Sight Range`, `Lit Threshold`, `Light Rate` |
| Curiga | `Fill Time` (detik sampai ketahuan), `Decay Per Second`, `Search Threshold`, `Look Around Time` |

## 6. Menyetel rasa gerak slime
Prefab `Characters/Slime_Player`:
- **`SlimeMotor`**: `Base Speed`, waktu tiap fase, `Slide Share` (besar = lebih merayap), `Hop Height`, `Air Steer`.
- **`SlimeJelly`**: `Stiffness`, `Damping`, `Max Stretch`.
- **`SlimePiece`**: `Eat Duration`, `Disguise Time`, `Reveal Time`, sprite samaran (`Disguise Sprite`).
- **`SlimeTraitFx`**: efek ciri di badan (panas = uap naik, dingin = serpih es, bau = garis bau, berkilau = kerlip, berat = debu saat mendarat). Warna dan jumlah partikel per detik bisa diubah di Inspector. Material `Art/Materials/Vfx_Particle` (tanpa cahaya). Placeholder sampai ada VFX dari tim.

Jeda aksi (anti spam) ada di `LevelSystems > PlayerBrain`: `Switch Cooldown`, `Split Windup`, `Split Cooldown`, `Merge Lock`, dan skill gumpalan: `Throw Distance`, `Throw Noise Radius`, `Throw Cooldown`.

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
