# Game Off 2026: tema "SHIFT"

Game jam itch.io, 1 bulan, mulai 3 Nov 2026, dikerjakan bareng tim. Engine: Unity (2D).
Nama game belum dipilih. Kandidat: Jarbreak, Slime Swap, Goo-napped, Body Double, Shiftling.

## Status
- Sudah disepakati: konsep gameplay, lore, objektif.
- Belum dibahas: technical (arsitektur, kode).
- Visual: arah art sudah ada di `art/CLAUDE.md`. Baca file itu dulu sebelum mengerjakan apa pun di Blender atau sprite.

## Genre & feel
Puzzle stealth 2D top-down, level pendek (2–4 menit). Referensi feel: Robbery Bob (aktif, lucu, chaotic), cerita ringan ala Helltaker.

## Lore
- Kerajaan slime. Tiap warga punya kekuatan (api, kristal, bayangan, dll).
- Alkemis jahat menculik hampir semua slime dan menyedot kekuatannya untuk menguasai dunia.
- Slime yang diculik disimpan di TOPLES SEGEL di markas alkemis.
- Pemain adalah satu slime yang lolos dan ingin menyelamatkan warga serta keluarganya.
- Pemain tidak bisa menyerang/membunuh. Murni stealth.
- Slime yang diselamatkan hanya untuk lore, TIDAK jadi skill.
- Ending: alkemis disegel di toplesnya sendiri (peran tertukar), kekuatan slime kembali, dan alat alkemis dipakai untuk membangun ulang kerajaan.

## Objektif per level
1. Intip toples untuk melihat ciri teman yang dikurung.
2. Kumpulkan bahan sambil menghindari penjaga.
3. Makan bahan sesuai "resep" supaya tubuh punya ciri yang sama.
4. Belah diri: potongan masuk toples menyamar jadi teman, lalu teman ditelan untuk dibawa kabur.
5. Kabur ke exit tanpa ketahuan.

Alkemis tidak sadar karena toples masih berisi slime (hanya slime yang bisa menggantikan slime).

## Mekanik
- Makan bahan: tubuh berubah sesuai bahan (cabai = merah, bara = panas, batu = berat, dll).
- Belah diri: potongan mewarisi sifat tubuh saat itu. Bisa dipakai jadi tiruan di toples ATAU umpan.
- Nyamar jadi barang untuk ngumpet dari penjaga.
- Tombol Shift untuk ganti kontrol antar potongan. Potongan yang ditinggal otomatis nyamar.
- Gabung lagi hanya bisa kalau potongan bersentuhan langsung.
- Resep tiruan: tiap slime di toples punya 2–3 ciri (warna, suhu, berat, kilau, bau).
- Sifat bahan memengaruhi stealth: habis makan bara jadi menyala dan gampang terlihat, habis makan batu jadi lambat.
- Bahan dipakai untuk resep ATAU umpan, jadi pemain harus memilih.
- Salah resep tidak langsung kalah. Penjaga cuma jadi lebih cepat curiga.

## Musuh
Musuh dibuat dari kekuatan slime curian. Pemain mengakalinya lewat sifat musuh itu sendiri, bukan melawan.
- Golem Batu: patroli, lambat, melihat lurus ke depan.
- Familiar Api: melihat benda panas/menyala.
- Anjing Lendir: mengendus bau bahan yang dimakan.
- Mata Penyihir: memeriksa toples, bisa membongkar tiruan.

Cara mengakali: nyamar, potongan umpan, urutan makan bahan, umpan pembawa sifat (misal potongan berbau).

## Hubungan ke tema SHIFT
Shapeshift (jadi barang dan jadi teman), tubuh yang terus berubah karena bahan, shift kontrol antar potongan (tombol Shift), dan peran tertukar di ending.

## Scope
- 3 chapter, 10–12 level, 3–4 musuh, 6–8 bahan.
- Minggu 1: prototipe mekanik dasar (gerak, makan, belah, gabung, 1 penjaga, 1 toples).
- Minggu 2: sistem resep, musuh, level kasar.
- Minggu 3: semua level, art, sound, cerita.
- Minggu 4: playtest, bug fix, halaman itch.io. TIDAK tambah fitur.
- Kalau molor, potong musuh ke-4 dan chapter 3.
- Aturan Game Off: source code wajib di repo GitHub publik. Jangan commit aset berbayar yang lisensinya melarang redistribusi.

## Cara diskusi
- Bahasa Indonesia santai, singkat, langsung ke inti. Istilah teknis boleh bahasa Inggris.
- Kasih pendapat jujur, jangan cuma ikut opini user. Kalau ada yang lemah, bilang.
- Kalau kasih opsi, sertakan kecocokannya ke tema dan risikonya.
- Saat diskusi desain game, fokus satu topik dulu. Jangan loncat ke technical/visual kecuali diminta.
- Pengecualian: kalau user minta kerjaan art/Blender/sprite atau sedang di folder `art/`, langsung kerjakan sesuai `art/CLAUDE.md`.

## Next step (belum dipilih)
- Tentukan daftar bahan + ciri dan slime teman per chapter.
- Susun daftar level per chapter.
- Mulai technical (arsitektur sistem Unity).
