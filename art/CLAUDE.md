# Art direction & pipeline

Berlaku untuk semua kerjaan di Blender, sprite, dan aset visual.

## Siapa mengerjakan apa (keputusan tim, 9 Nov 2026)
- **Desain visual = tim desainer**: bentuk karakter, wajah, style, palet, look level. AI tidak mengubah desain atas inisiatif sendiri.
- **AI = teknis**: pipeline render, tooling (`art/tools/`), import Unity, shader, setup scene.
- Kalau diminta mengubah desain tanpa arahan konkret (sketsa, referensi, "mata lebih kecil"), minta arahan dulu.
- Panduan untuk desainer: `docs/panduan-art-desainer.md`. Pembagian kerja: `docs/ai-vs-tim.md`.

## Gaya (arah awal, desain final oleh tim)
- **Bentuk:** cute dan soft-blobby, terinspirasi feel Slime Rancher. Bulat, kenyal, ekspresif, mata besar sederhana.
- **Output:** sprite pixel art top-down 3/4, mirip feel aset Momo (dango slime, itch.io).
- **BUKAN PSX/low-poly.** Model boleh smooth (Subdivision OK) karena yang dipakai di game hanya hasil render-nya.
- **Desain harus original.** Slime Rancher dan Momo hanya acuan feel. Aset Momo tidak boleh masuk repo (lisensi melarang redistribusi, repo publik).

## Spesifikasi sprite (sudah dikunci)
| Item | Nilai |
|---|---|
| Pipeline | Blender → pre-render → sprite sheet → Unity 2D (atau gambar tangan, aturan sama) |
| Ukuran cell | 48×48 px |
| Sudut | top-down 3/4, kamera orthographic (tilt 55°) |
| Arah | 3: `front`, `side` (hadap kanan), `back`. Kiri = flip X di Unity |
| Unity import | Otomatis oleh `PixelSpriteImporter`: PPU 48, Point, no compression, slice grid 48, pivot bottom-center |
| Bayangan jatuh | TIDAK di-render. Blob shadow sprite terpisah di Unity |
| **Cahaya** | **TIDAK di-bake.** Sprite = albedo flat + normal map. Cahaya dihitung real-time di Unity (shader `GooNapped/PixelToonLit`, Light 2D) lalu dipotong jadi band |

## Lighting di engine (penting)
- **Albedo flat:** warna material saja. Tanpa highlight/bayangan dari arah cahaya. Outline gelap boleh (bagian dari desain, bukan cahaya).
- **Nada menengah:** albedo jangan terlalu gelap. Lampu hanya bisa menggelapkan, tidak bisa membuat lebih terang dari albedo. Gelapnya stage datang dari Global Light 2D yang redup.
- **Normal map** (`<nama>_normal`): ruang layar, R = kanan, G = atas, B = ke penonton, linear. Ukuran dan layout frame sama persis dengan albedo. Dipasang otomatis sebagai secondary texture `_NormalMap`.
- **Light 2D** yang harus mempengaruhi bentuk: `Normal Map Quality = Accurate`, `Distance` ±1.5.
- Bagian yang menyala sendiri (api, kristal glow) pakai material unlit/emisif, bukan dicerahkan di albedo.

## Slime pemain & tiruan: layer
Warna tubuh berubah terus karena bahan, jadi warna JANGAN di-render per varian.
- **`body`**: flat putih (255) + outline abu 40. Di Unity, intensitas cahaya dipetakan ke 4 warna palet state bahan (gradient-map dari cahaya, `_UseGradient = 1`).
- **`normal`**: normal map body (render Blender, material `Slime_NormalPass`, view transform `Raw`, Z dibalik supaya +Z ke kamera). Lalu `art/tools/normal_pad.py` (isi 1px di bawah outline).
- **`face`**: mata, glint, mulut, shine. Body jadi holdout. Tampak belakang: kosong.
- Body + face = dua SpriteRenderer dengan animasi yang sama.
- Efek nyala (bara), uap (panas), garis bau ditambahkan di Unity (particle/overlay), tidak di-bake.
- Musuh, prop, toples: albedo berwarna flat + normal map (mode `palette` di pixelize untuk albedo).

## Cara ciri resep terbaca di 48px (usulan, boleh diubah tim)
| Ciri | Visual |
|---|---|
| Warna | palet gradient-map body |
| Suhu | glow + uap (Unity), light 2D kecil menempel di slime |
| Berat | squash lebih pipih, gerak lambat, palet lebih gelap |
| Kilau | 1–2 pixel highlight tambahan (layer `face`/overlay) |
| Bau | garis bergelombang di atas kepala (Unity) |

## Aturan bentuk slime (arah awal)
- Origin di tengah bawah, berdiri di Z=0, hadap **-Y**. Tinggi badan ±1.0 m, lebar ±1.2 m.
- Siluet: kubah bulat, bawah agak rata/melebar seperti tetesan jeli.
- Setiap bagian harus lolos **tes siluet**: render 48px hitam-putih, bentuknya masih bisa dikenali.

## Render Blender
- Body flat: `render_slime.py` memaksa ramp toon `Slime_Body_Toon` jadi putih selama render (nilai ramp tetap disimpan untuk preview viewport).
- Catatan ramp (kalau suatu saat butuh render grayscale ber-band lagi): isi nilai sRGB 32/96/160/224 (tengah bin `pixelize.py`), bukan linear 0.25/0.5/0.75/1.0.
- Tanpa AA (`filter_size 0`), film transparan, view `Standard` (albedo) / `Raw` (normal).

## Mood stage & palet (USULAN, belum disepakati tim)
Game ini stealth, jadi cahaya = mekanik (Familiar Api melihat benda menyala, slime yang makan bara jadi gampang terlihat). Stage TIDAK boleh cerah.
- **Dasar:** gelap dan dingin (Global Light 2D redup kebiruan). Cahaya hangat hanya dari sumber: kuali, api, obor.
- **Slime pemain harus menonjol**: palet gradient-map lebih terang dari lingkungan, outline gelap sewarna badan.
- **Cone penjaga:** merah, tetap terbaca di atas lantai gelap.
- **Palet:** `art/palette.hex` (lingkungan, musuh, prop). Slime pemain memakai palet gradient-map per state bahan (material di Unity).

## Konvensi Blender
- Satu file per karakter: `art/blender/<nama>.blend`. Simpan incremental (`_v01`, `_v02`) sebelum perubahan besar. Terbaru slime: `slime_v06.blend`.
- Nama objek: `<Karakter>_<Bagian>` (contoh: `Slime_Body`, `Slime_EyeL`, `Golem_Head`).
- Animasi: Action per anim (`idle`, `move`, `eat`, `split`, `merge`, `jelly`, `disguise`, `alert`). Squash & stretch pakai shape key/lattice.
- Jangan hapus objek/file tanpa konfirmasi.

## Folder
```
art/
  blender/      .blend sumber
  ref/          referensi (gitignore kalau aset berbayar)
  renders/raw/  hasil render mentah (gitignore)
  sprites/      sheet final (body/face/normal) → di-copy ke Assets/_Game/Art/Sprites/<kategori>/
  tools/        goonapped_sprite_addon.py (panel render), make_sprite_template.py,
                pixelize.py, normal_pad.py, render_slime.py (khusus slime_v06)
```

## Template render (model baru)
- `art/blender/sprite_template.blend` + add-on `art/tools/goonapped_sprite_addon.py`: kamera, render setting, material flat sementara, 3 layer, pixelize, preview. Panduan desainer: `docs/panduan-blender-3d.md`.
- Template dibangun ulang dari skrip, jangan diedit tangan: `blender -b --factory-startup --python art/tools/make_sprite_template.py`. Konstanta spek (CELL, PX_PER_M, CAM_TILT) ada di add-on.
- Render headless di mesin ini butuh `--gpu-backend vulkan` (backend OpenGL crash di EEVEE background).
- Hasil v06 lewat template = identik pixel dengan `art/sprites/slime/` (sudah dites 9 Nov).

## Workflow
- Modeling → skill `slime-model`. Render → template + add-on (model baru) atau `art/tools/render_slime.py` (slime_v06).
- Pakai Blender MCP. Setelah tiap tahap, ambil screenshot/preview dan **cek sendiri** sebelum lanjut.
- Cek hasil akhir di scene `Assets/_Game/Scenes/Sandbox/DemoSlime.unity` (ada obor dengan normal map aktif).
