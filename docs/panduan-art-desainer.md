# Panduan art untuk desainer (rule of thumb)

Game ini pakai **cahaya real-time di Unity** (lampu obor, kuali, cone penjaga), jadi art **tidak boleh punya cahaya yang di-bake**. Cahaya dan bayangan dihitung engine, lalu dipotong jadi beberapa "band" oleh shader `GooNapped/PixelToonLit` supaya tetap terlihat pixel art.

Contoh hidup: buka `Assets/_Game/Scenes/Sandbox/DemoSlime.unity`, tekan Play. Slime di kiri obor terang di sisi kanan, slime di kanan obor terang di sisi kiri.

## 1. Tiap aset = 2 gambar (kadang 3)

| File | Isi | Contoh |
|---|---|---|
| `nama.png` (albedo) | Warna material saja, **flat**. Tanpa highlight, tanpa bayangan dari arah cahaya. | `crate.png` |
| `nama_normal.png` | Arah permukaan tiap pixel (lihat bagian 3). Ukuran & layout frame **sama persis** dengan albedo. | `crate_normal.png` |
| (opsional) bagian menyala | Api, kristal glow, mata menyala → sprite/layer terpisah, nanti diberi material unlit. | `torch_flame.png` |

Untuk sprite sheet beranimasi pakai token layer: `slime_body_idle_front.png` ↔ `slime_normal_idle_front.png` (`_body_`/`_albedo_` → `_normal_`). Unity memasang normal map otomatis kalau namanya cocok.

## 2. Aturan albedo

1. **Flat.** Satu warna per material (kayu, besi, kain). Boleh 1 warna gelap untuk celah/nat yang memang lubang (ambient occlusion tipis), tapi **jangan** sisi terang/gelap karena arah lampu.
2. **Nada menengah.** Jangan gambar warna yang sudah gelap. Lampu hanya bisa *menggelapkan* sampai warna albedo, tidak bisa membuat lebih terang. Mood gelap datang dari lampu global yang redup, bukan dari cat gelap.
   - Contoh nyata: lantai biru tua `#1b2740` tidak pernah terang walau dekat obor. Diganti abu-biru menengah `#5d6472`, langsung hidup.
3. **Outline boleh** (bagian dari gaya, bukan cahaya). Pakai versi gelap dari warna objek, bukan hitam pekat.
4. **Hindari putih murni dan hitam murni** kecuali mata/detail kecil.
5. **Tanpa anti-aliasing.** Pixel tegas, alpha 0 atau 255.
6. **Detail kecil sedikit saja.** Tekstur ramai bersaing dengan karakter. Lantai harus "diam", karakter yang menonjol.

## 3. Cara bikin normal map

Format: **R = ke kanan, G = ke atas, B = ke arah penonton** (OpenGL / "Y+"). Permukaan menghadap layar = `(128, 128, 255)` (biru muda). Simpan sebagai PNG biasa.

Pilih sesuai jenis aset:

| Cara | Cocok untuk | Catatan |
|---|---|---|
| **Pre-render Blender** | Karakter & benda bulat (slime, toples, musuh) | Otomatis, paling akurat. Pakai template `art/blender/sprite_template.blend` (panel GooNapped, render 1 klik). Panduan: [panduan-blender-3d.md](panduan-blender-3d.md). |
| **Generator dari gambar** (Laigter, gratis & open source) | Prop & tile gambar tangan | Masukkan albedo, atur bevel/kedalaman, export normal. Cek hasilnya di scene demo. |
| **Gambar tangan 4 arah** | Bentuk sederhana | Gambar versi "disinari dari kiri/kanan/atas/bawah" lalu gabung di tool (mis. SpriteIlluminator, berbayar) |
| **Tanpa normal map** | Lantai rata, dinding datar | Boleh. Tetap kena cahaya (gelap-terang sesuai jarak lampu), hanya tanpa efek bentuk. |

Tips: bagian atas objek (menghadap ke atas layar) cenderung hijau, kiri cyan, kanan pink, bawah ungu. Kalau terbalik (kiri pink), channel R atau G salah arah.

## 4. Ukuran & orientasi (dikunci)

- Grid **48×48 px**, PPU 48. 1 tile = 48 px = 1 unit Unity.
- Top-down 3/4. Karakter 3 arah: `front`, `side` (hadap kanan), `back`. Kiri = flip otomatis.
- Pivot otomatis di **tengah bawah** cell (titik kaki).
- Sheet horizontal: frame 0 di kiri.
- **Jangan gambar bayangan jatuh** di sprite. Unity pakai blob shadow terpisah.

## 5. Slime (khusus)

- Body digambar/dirender **putih flat + outline abu 40**. Warna slime ditentukan palet 4 warna per state bahan (material di Unity, desainer yang pilih warnanya): band 1 = sisi gelap, band 4 = sisi paling terang.
- Geser hue antar band (bayangan agak biru, terang agak kuning), bukan cuma terang-gelap.
- Mata/mulut di layer `face` terpisah, warna tetap.

## 6. Alur kerja

1. Desain/sketsa di tool pilihan (Aseprite, Krita, Blender, dst.). File sumber di `art/` (`art/blender/`, atau tambah `art/source/`).
2. Export albedo + normal ke `art/sprites/<kategori>/`.
3. Copy ke `Assets/_Game/Art/Sprites/<Characters|Enemies|Props|Environment|UI|VFX>/`. Import setting terpasang otomatis.
   **Tile untuk Tilemap (lantai, tembok) masuk `Sprites/Tiles/`**: di folder itu pivot otomatis di tengah, supaya gambar tile pas dengan collision. Sprite di folder lain pivot tengah-bawah (titik kaki).
4. Pasang material:
   - lingkungan/prop: `Env_Lit`
   - slime body: `Slime_*`
   - wajah: `Slime_Face`
   - api/glow: material unlit
5. Cek di `DemoSlime` atau scene level dengan lampu. Lampu harus `Normal Map Quality = Accurate`.
6. Commit ke branch kerja (bukan `main`), lalu PR.

## 7. Checklist sebelum commit

- [ ] Albedo flat, tanpa highlight/bayangan arah cahaya
- [ ] Nada menengah (tidak gelap pekat)
- [ ] Normal map ada (atau sengaja tidak, untuk benda rata), ukuran sama dengan albedo
- [ ] Nama file sesuai pola, grid 48 px, tanpa anti-aliasing
- [ ] Sudah dicek di scene dengan lampu: sisi yang menghadap lampu = terang
- [ ] Tidak ada aset berbayar/berlisensi tertutup (repo publik)
