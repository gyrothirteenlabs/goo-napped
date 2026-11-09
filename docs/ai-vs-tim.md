# Pembagian kerja: AI vs tim

Catatan dari pengalaman minggu 1 (pipeline slime Blender → Unity dikerjakan bareng Claude lewat Blender MCP + Unity MCP).
Intinya: **AI cepat untuk kerjaan yang aturannya jelas dan bisa dicek. Keputusan rasa (fun, gaya, cerita) tetap di tim.**

**Keputusan (9 Nov 2026):** semua desain art dikerjakan tim desainer. Code, tooling render, setup scene, dan **shader** dikerjakan AI (dibantu/di-review programmer). Panduan desainer: [panduan-art-desainer.md](panduan-art-desainer.md).

## Cocok dikerjakan AI (tetap di-review tim)

| Kerjaan | Kenapa cocok | Yang harus dicek tim |
|---|---|---|
| Pipeline & tooling: render sprite, `pixelize.py`, importer otomatis, skrip batch | Aturannya sudah dikunci di `art/CLAUDE.md`, hasilnya bisa diukur (ukuran sheet, jumlah band, pivot) | Sekali lihat preview per anim |
| Render ulang massal setelah model/shading berubah | Repetitif, 250+ frame, skrip `art/tools/render_slime.py` sudah ada | Konsistensi frame, tidak ada jitter |
| Setup scene prototipe, prefab, collider, kamera, lighting dasar | Banyak klik, gampang di-otomasi lewat Unity MCP | Rasa gerak & ukuran ruang di Play mode |
| Boilerplate kode Unity: controller prototipe, state machine sederhana, animator sprite, sistem data resep (ScriptableObject) | Pola umum, cepat ditulis | Arsitektur akhir, nama, keterbacaan |
| Debug error console, cari penyebab bug teknis | Bisa baca log, cek komponen, tes ulang | Pastikan fix-nya tidak nambah hack |
| Placeholder art (tile, prop sederhana dari palet) | Biar level bisa dites sebelum art final | Diganti art tim sebelum rilis |
| Dokumentasi, catatan teknis, halaman itch.io draft, changelog | Cepat dan rapi | Nada & isi akhir |

## Harus dikerjakan tim

| Kerjaan | Kenapa |
|---|---|
| **Desain karakter & art style final** (bentuk slime, wajah, ekspresi, musuh, toples) | Ini identitas game. AI bisa eksekusi di Blender, tapi hasilnya generik/"aneh" kalau arahnya tidak jelas. Contoh nyata: slime v06 masih dinilai kurang cocok. Tim perlu bikin sketsa/konsep dulu, AI baru bantu eksekusi. |
| Keputusan mood & palet (usulan gelap-dingin masih USULAN) | Selera tim, juga dampak ke keterbacaan gameplay |
| Desain level & puzzle (resep, posisi penjaga, alur kabur) | Butuh playtest dan intuisi "seru atau tidak" |
| Game feel: kecepatan, squash, timing anim, juice | Harus dirasakan langsung di tangan, bukan dari angka |
| Cerita, dialog, humor ala Helltaker | Suara/gaya penulisan tim |
| Musik & SFX | Kualitas & lisensi; aset berbayar/Momo tidak boleh masuk repo publik |
| Playtest & prioritas scope (apa yang dipotong kalau molor) | Keputusan produk |
| Review & merge PR ke `main` | AI hanya commit ke branch kerja, tidak pernah ke `main` |

## Abu-abu (AI bantu, tim yang putuskan)

- **Arsitektur Unity** (sistem potongan/Shift, sistem bahan, AI penjaga): AI bisa usulkan 2–3 opsi + risiko, tim pilih, baru AI implementasi.
- **Animasi**: AI bisa bikin keyframe/shape key dari deskripsi, tapi timing final dicek animator tim.
- **Efek** (glow bara, uap, garis bau): AI tulis shader/particle, tim nilai apakah terbaca di 48px.

## Aturan kerja dengan AI di repo ini

1. AI kerja di branch fitur (contoh `art/slime`), commit per tahap, **tidak push ke `main`, tidak force-push**.
2. Commit AI tidak memakai atribusi AI (permintaan tim).
3. Setiap tahap art wajib ada screenshot/preview yang dicek sebelum lanjut.
4. Perubahan `Packages/` dan `ProjectSettings/` **dibahas tim dulu**, jangan ikut commit art.
5. AI tidak menghapus file/objek tanpa konfirmasi.
6. Kasih AI arah yang konkret (sketsa, referensi, "lebih bulat", "mata lebih kecil") — permintaan "bikin lebih bagus" saja hasilnya untung-untungan.

## Status per 9 Nov

- Struktur project `Assets/_Game/` + 18 scene placeholder (Boot, menu, cerita, 12 level) di Build Settings.
- Lighting real-time: shader `GooNapped/PixelToonLit` (normal map + band toon), slime body flat + normal map.
- Sandbox `Assets/_Game/Scenes/Sandbox/DemoSlime.unity`: 3 slime, WASD + collider, obor dengan normal map.
- Belum: desain slime final (tim), controller asli (Input Actions), sistem makan/belah/gabung, penjaga, tileset level.
