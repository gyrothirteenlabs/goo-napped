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
   | `J` | toples (resep = `recipe` di config) |
   | `b` `l` `w` `r` | Serbuk Batu, Lumut Pendar, Air Embun, Ramuan Api |
   | `L` | lentera |
   | `g`/`G`, `h`/`H`, `k`/`K` | titik awal/akhir patroli Golem 1, 2, 3 |
   | `f`/`F`, `v`/`V` | titik awal/akhir patroli Familiar Api 1, 2 |

3. Unity: menu **GooNapped > Blockout > C1_L0x** (atau **Semua chapter 1**). Scene diisi ulang dan disimpan.
4. Level baru: salin satu `Config`, ganti `scene`, `recipe`, `next`, `note`, tambah `[MenuItem]`. Lalu **tambahkan nama scene di `GameFlow.Levels`** (urutan pilih level) dan pastikan scene ada di Build Settings.

### Cara manual (desain final)
- **Lantai/tembok**: buka Tile Palette (Window > 2D > Tile Palette), gambar di Tilemap `Grid/Floor` dan `Grid/Walls`. `Walls` punya collider + bayangan otomatis.
- **Objek**: drag prefab ke scene di bawah `Actors/…`:
  - `Prefabs/Props/Jar` → isi `Recipe` di Inspector
  - `Prefabs/Props/IngredientPickup` → isi `Ingredient`
  - `Prefabs/Enemies/Golem_Placeholder` → isi `Patrol Points` (posisi dunia)
  - `Prefabs/Level/Lantern` → di `Lights/`
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
   - **Smell Radius**: bau (untuk Anjing Lendir nanti).
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
| Patroli | `Patrol Points`, `Speed`, `Wait At Point` |
| Penglihatan | `View Range`, `View Angle` (lebar kerucut), `Awareness Radius` (radius sadar) |
| Cahaya | `Light Sight Range`, `Lit Threshold`, `Light Rate` |
| Curiga | `Fill Time` (detik sampai ketahuan), `Decay Per Second`, `Search Threshold`, `Look Around Time` |

## 6. Menyetel rasa gerak slime
Prefab `Characters/Slime_Player`:
- **`SlimeMotor`**: `Base Speed`, waktu tiap fase, `Slide Share` (besar = lebih merayap), `Hop Height`, `Air Steer`.
- **`SlimeJelly`**: `Stiffness`, `Damping`, `Max Stretch`.
- **`SlimePiece`**: `Eat Duration`, `Disguise Time`, `Reveal Time`, sprite samaran (`Disguise Sprite`).

Jeda aksi (anti spam) ada di `LevelSystems > PlayerBrain`: `Switch Cooldown`, `Split Windup`, `Split Cooldown`, `Merge Lock`, dan skill gumpalan: `Throw Distance`, `Throw Noise Radius`, `Throw Cooldown`.

## 7. Suara
- Komponen **`AudioDirector`** di prefab `Systems/LevelSystems`: drag klip ke slot `Music`, `Ambience`, dan tiap efek (langkah slime, langkah golem, makan, belah, gabung, nyamar, Shift, waspada, ketahuan).
- Kretek obor: `AudioSource` di prefab `Level/Lantern`.
- Klip `proto_*` adalah placeholder hasil sintesis (**GooNapped > Audio > Buat suara placeholder**). Klip asli: taruh di `Assets/_Game/Audio/Music|SFX/` (WAV/OGG), ganti referensinya. Kode tidak perlu diubah.

## 8. Sprite dari Blender
1. Desainer 3D bikin model di template Blender: [panduan-blender-3d.md](panduan-blender-3d.md) (render 1 klik → `art/sprites/<nama>/`).
2. Copy sheet ke `Assets/_Game/Art/Sprites/<Characters|Enemies|Props>/<Nama>/`. Slice 48 px, pivot, dan normal map terpasang otomatis.
3. Karakter slime: drag frame ke `SlimeSpriteAnimator > Clips` (anim + arah + frame body/face) di prefab `Slime_Player`. Spek jumlah frame tiap anim: panduan Blender bagian C.1.
4. Musuh/prop: material `Env_Lit` untuk albedo, `Enemy_Glow_Unlit` untuk bagian menyala.

## 9. Test
Window > General > **Test Runner** > EditMode > Run All. Wajib hijau sebelum PR.
