# Art direction & pipeline

Berlaku untuk semua kerjaan di Blender, sprite, dan aset visual.

## Gaya
- **Bentuk:** cute dan soft-blobby, terinspirasi feel Slime Rancher. Bulat, kenyal, ekspresif, mata besar sederhana.
- **Output:** sprite pixel art top-down 3/4, mirip feel aset Momo (dango slime, itch.io).
- **BUKAN PSX/low-poly.** Tidak ada batas segitiga dan tidak ada tekstur 64px. Model boleh smooth (Subdivision OK) karena yang dipakai di game hanya hasil render-nya.
- **Desain harus original.** Slime Rancher dan Momo hanya acuan feel. Jangan menyalin desain slime mereka (contoh: kristal pink ala Rock slime, telinga dango ala Momo). Aset Momo tidak boleh masuk repo karena lisensinya melarang redistribusi, sedangkan repo kita publik.

## Spesifikasi sprite (sudah dikunci)
| Item | Nilai |
|---|---|
| Pipeline | Blender → pre-render → sprite sheet → Unity 2D |
| Ukuran cell | 48×48 px |
| Sudut | top-down 3/4, kamera orthographic |
| Arah | 3: `front`, `side` (hadap kanan), `back`. Kiri = flip X di Unity |
| Unity import | PPU 48, Filter Point, Compression None, Pixel Perfect Camera |
| Bayangan | TIDAK di-render. Pakai blob shadow sprite terpisah di Unity |

## Slime pemain & tiruan: render grayscale + layer
Warna tubuh berubah terus karena bahan, jadi warna JANGAN di-render per varian.
- **Layer `body`:** grayscale, toon 4 tingkat + outline hitam. Di Unity diwarnai lewat gradient-map (1 baris palet per state bahan).
- **Layer `face`:** mata, mulut, dan detail berwarna tetap. Di-render terpisah dengan body disembunyikan.
- Dua layer ini jadi dua SpriteRenderer dengan animasi yang sama.
- Efek nyala (bara), uap (panas), dan garis bau ditambahkan di Unity (particle/overlay), tidak di-bake ke sprite.
- Musuh, prop, dan toples langsung di-render berwarna (mode `palette`).

## Cara ciri resep terbaca di 48px (usulan, boleh diubah tim)
| Ciri | Visual |
|---|---|
| Warna | gradient-map body |
| Suhu | glow + uap (Unity) |
| Berat | squash lebih pipih, gerak lambat, palet lebih gelap |
| Kilau | 1–2 pixel highlight tambahan (layer `face`/overlay) |
| Bau | garis bergelombang di atas kepala (Unity) |

## Aturan bentuk slime
- Origin di tengah bawah, berdiri di Z=0, hadap **-Y**. Skala: tinggi badan sekitar 1.0 m, lebar sekitar 1.2 m.
- Siluet: kubah bulat, bawah agak rata/melebar seperti tetesan jeli. Jangan bola sempurna.
- Mata: 2 oval hitam vertikal, besar, posisinya sekitar 45% tinggi badan (agak ke bawah = lebih cute), jarak antar mata sekitar 1 lebar mata. Tanpa hidung. Mulut opsional dan kecil.
- Aksesori/ciri (tanduk, daun, bara, batu): bentuk chunky sederhana dengan 1 bentuk dasar yang jelas. Harus tetap kebaca jadi 3–6 pixel di 48px. Jangan bola yang cuma di-scale.
- Setiap bagian harus lolos **tes siluet**: render 48px hitam-putih, bentuknya masih bisa dikenali.

## Shading render
- Toon: Shader to RGB → Color Ramp **Constant**, 3–4 band. Tanpa gradasi halus, tanpa SSS, tanpa transparansi halus.
- Body slime grayscale: warna ramp harus jatuh di tengah bin `pixelize.py` (bin per 64). Isi nilai **sRGB 32/96/160/224** (linear 0.0144/0.117/0.3515/0.7454), posisi 0/0.3/0.55/0.85. Kalau diisi linear 0.25/0.5/0.75/1.0, hasilnya cuma 2 band.
- Satu sun light dari kiri-atas-depan. Arahnya SAMA untuk semua aset.
- Reuse pipeline dari `Knight_mc__1.blend` (Blender 3.6): material `PixelArt_Simple`, image `Bayer Matrix` (dither, pakai seperlunya), `Dolly_Camera`. Append, jangan bikin ulang dari nol.

## Mood stage & palet (USULAN, belum disepakati tim)
Game ini stealth, jadi cahaya = mekanik (Familiar Api melihat benda menyala, slime yang makan bara jadi gampang terlihat). Stage TIDAK boleh cerah.
- **Dasar:** gelap dan dingin (biru kehijauan). Cahaya hangat (oranye/kuning) hanya dari sumber: kuali, api, obor. Warna hangat = sinyal "penting/berbahaya".
- **Slime pemain harus menonjol** dari lantai gelap: palet gradient-map lebih terang dari lingkungan, outline gelap yang sewarna badan, rim light tipis kalau perlu.
- **Cone penjaga:** merah, tetap terbaca di atas lantai gelap.
- **Jangan terlalu gelap:** area aman agak gelap, area penjaga dan bahan tetap terbaca (cek saat playtest).
- **Palet:** `art/palette.hex` (lingkungan, musuh, prop). Slime pemain TIDAK memakai file ini, ia di-render grayscale dan diwarnai gradient-map di Unity.
- **Gradient-map slime:** nilai abu 40 = outline (hijau/merah/dll gelap versi warna badan), 64/128/191/255 = 4 band. Geser hue antar band (bayangan agak ke biru, terang agak ke kuning), bukan sekadar terang-gelap.
- Referensi mockup dari generator gambar hanya acuan mood, bukan target piksel. Pipeline render 48px tidak akan sedetail itu.

### Slime: perbaikan yang direncanakan (belum dikerjakan)
- Layer highlight terpisah (1-3 piksel). Hati-hati: "kilau" adalah ciri resep. Highlight dasar harus kecil, ciri kilau = highlight besar + kilauan.
- Mata: tambah glint kecil dan mulut kecil opsional di layer `face`.
- Bentuk lebih bulat dan jeli (Subdivision + smooth), bukan facet low-poly.

## Konvensi Blender
- Satu file per karakter: `art/blender/<nama>.blend`. Simpan incremental (`_v01`, `_v02`) sebelum perubahan besar.
- Nama objek: `<Karakter>_<Bagian>` (contoh: `Slime_Body`, `Slime_EyeL`, `Golem_Head`). Collection: `<Karakter>`, `Render_Rig`, `Ref`.
- Animasi: Action per anim (`idle`, `move`, `eat`, `split`, `merge`, `disguise`, `alert`). Squash & stretch pakai shape key/lattice.
- Jangan hapus objek/file tanpa konfirmasi.

## Folder
```
art/
  blender/      .blend sumber
  ref/          referensi (gitignore kalau aset berbayar)
  renders/raw/  hasil render mentah (gitignore)
  sprites/      sheet final → di-copy ke Unity Assets/Sprites
  tools/        pixelize.py, render_slime.py (render semua anim slime, jalankan di Blender)
```

## Workflow (wajib)
- Modeling karakter/prop → ikuti skill `slime-model`.
- Render ke sprite → ikuti skill `sprite-render`.
- Pakai Blender MCP. Setelah tiap tahap, ambil screenshot viewport/render dan **cek sendiri** sebelum lanjut. Jangan bangun semua sekaligus tanpa cek.
