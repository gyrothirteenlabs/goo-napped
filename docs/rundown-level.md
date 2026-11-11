# Rundown chapter & level (USULAN, belum disepakati tim)

Ditulis 10 Nov 2026 dari arahan: lokasi jangan hanya dalam ruangan, musuh muncul di tempat yang masuk akal, pace seperti dungeon Zelda. Desain level final tetap keputusan tim desainer. Dokumen ini bahan diskusi, bukan keputusan.

## Pola pace (dipinjam dari Zelda)
Tiap chapter memakai urutan yang sama, supaya pemain selalu belajar satu hal baru lalu diuji:

1. **Kenalan:** musuh/mekanik baru muncul sendirian, di ruang aman, tanpa bisa gagal fatal.
2. **Latihan:** hal yang sama, tapi dengan satu tekanan (waktu patroli, ruang sempit).
3. **Pelintir:** aturan yang baru dipelajari dibalik atau digabung dengan mekanik chapter sebelumnya.
4. **Ujian:** level terpanjang chapter itu, menyelamatkan slime "penting" (keluarga/tokoh), diakhiri pintu ke area berikutnya.

Perjalanan pemain bergerak **dari luar ke dalam, dari bawah ke atas**: kaki gunung → tembok kastil → menara alkemis. Tiap chapter punya sumber cahaya dan warna aksen sendiri supaya terasa pindah tempat walau semuanya gelap.

## Chapter 1: Tambang & tebing batu (luar, malam)
Slime yang baru diculik dikumpulkan di kamp tambang sebelum dibawa ke kastil. Golem Batu "di rumahnya": terbuat dari batu tambang itu sendiri, jadi tidak aneh.
- **Cahaya:** bulan (biru redup), lentera tambang, kristal mentah yang berpendar.
- **Musuh:** Golem Batu (lambat, melihat lurus).
- **Bahan:** Serbuk Batu (berat), Lumut Pendar (kilau), Air Embun (dingin).

| Level | Isi | Yang diajarkan |
|---|---|---|
| C1_L01 | Jalan setapak tebing, 1 toples di gerobak, tanpa penjaga | Gerak, intip toples, makan 1 bahan, belah, tukar |
| C1_L02 | Kamp tambang, 1 golem patroli lurus | Cone lurus, nyamar jadi batu saat golem lewat |
| C1_L03 | Lorong tambang sempit, 2 golem, resep 2 ciri | Urutan makan (ciri sejenis tertimpa), Shift antar potongan |
| C1_L04 | Mulut tambang → gerbang kastil, slime tetua | Potongan sebagai umpan, kabur sambil berat (lambat) |

## Chapter 2: Kastil bawah (halaman, dapur tungku, kandang)
Masuk tembok kastil. Sebagian level di halaman terbuka (hujan/kabut), sebagian di ruang kerja para pembantu alkemis.
- **Cahaya:** obor dinding, tungku pelebur (jingga), jendela berpalang.
- **Musuh baru:** Familiar Api (melihat yang panas/menyala), lalu Anjing Lendir (mencium bau).
- **Bahan:** Ramuan Api (panas, menyala), Minyak Belerang (bau), Garam Beku (dingin, memadamkan).

| Level | Isi | Yang diajarkan |
|---|---|---|
| C2_L01 | Halaman kastil saat hujan, 1 Familiar Api | Menyala = terlihat dari jauh; resep butuh "panas" jadi harus berisiko |
| C2_L02 | Kandang & selokan, Anjing Lendir (blockout ada) | Bau meninggalkan jejak; potongan berbau sebagai umpan; muntahkan bahan bau |
| C2_L03 | Ruang tungku, familiar + golem | Makan Ramuan Api paling akhir; Garam Beku untuk memadamkan diri |
| C2_L04 | Gudang toples besar, ketiga musuh, slime keluarga pertama | Gabungan: umpan bau + nyamar + urutan makan |

## Chapter 3: Menara alkemis (kandidat dipotong kalau molor)
Naik ke lab. Di sinilah gambar konsep lab (rak ramuan, kuali, deretan toples) dipakai.
- **Cahaya:** kuali (hijau/kuning), kristal ungu, hampir tanpa cahaya lain.
- **Musuh baru:** Mata Penyihir (memeriksa toples, bisa membongkar tiruan yang resepnya salah).
- **Bahan:** Esens Bayangan (gelap, tidak berkilau), Debu Kristal (kilau).

| Level | Isi | Yang diajarkan |
|---|---|---|
| C3_L01 | Perpustakaan resep, 1 Mata Penyihir | Tiruan harus benar: salah resep = cepat dibongkar |
| C3_L02 | Lab ramuan (gambar konsep), mata + familiar | Resep 3 ciri, bahan terbatas (resep ATAU umpan) |
| C3_L03 | Ruang toples utama, semua musuh | Ujian semua mekanik, 2 toples sekaligus |
| C3_L04 | Puncak menara: alkemis | Final: bukan bertarung. Pemain menukar diri dengan isi toples terakhir sampai alkemis sendiri yang tersegel (peran tertukar) |

Kalau chapter 3 dipotong: C2_L04 jadi final, Mata Penyihir tidak dibuat, dan ending dipindah ke gudang toples.

## Prompt gambar konsep (dasar, ganti bagian [..] per level)
Versi sebelumnya menghasilkan gambar terlalu cerah. Yang ini memaksa gelap:

```
Top-down 3/4 pixel art game screenshot, 16:9, 48px tiles, stealth puzzle game about a small slime.
Scene: [LOKASI, mis. "abandoned quarry on a cliff at night, mine carts, crystal veins"].
VERY DARK, low-key lighting: 75% of the image is near-black blue (#0b0f1a to #1b2740).
Only light sources: [SUMBER, mis. "two hanging mine lanterns and faint glowing crystals"], warm, small radius, hard falloff.
No ambient daylight, no evenly lit floor. Objects outside the light are dark silhouettes with a thin rim.
Characters: one small green slime (player) hiding, [MUSUH, mis. "a stone golem with a red vision cone pointing straight ahead"].
Glass jars with captured slimes on [TEMPAT]. Alchemy ingredients on the floor: [BAHAN, mis. "a red potion flask, a pouch of stone powder"].
Flat colors, hard pixel edges, no anti-aliasing, limited palette, no text, minimal HUD.
```

## Yang perlu diputuskan tim
- Setuju urutan lokasi luar → kastil bawah → menara?
- Nama dan daftar bahan final (6–8), plus ciri tiap bahan.
- Slime "penting" tiap ujian chapter (siapa, ciri resepnya).
- Chapter 3 dikerjakan atau dipotong (putuskan paling lambat akhir minggu 2).
