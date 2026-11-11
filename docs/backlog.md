# Backlog fitur

Status: ✅ selesai (prototipe) · 🔧 sedang dikerjakan · ⬜ belum. Prioritas: **P1** wajib untuk jam, **P2** penting, **P3** kalau sempat.
Fokus minggu 2–3: **perbanyak fitur & mekanik dulu, bug menyusul** (keputusan 10 Nov).

## Mekanik slime
| | Fitur | Prio | Catatan |
|---|---|---|---|
| ✅ | Gerak lompat-merayap + jeli | P1 | `SlimeMotor`, `SlimeJelly` |
| ✅ | Makan bahan, ciri tubuh, urutan makan | P1 | `SlimeBodyState` |
| ✅ | Belah / gabung / Shift + jeda anti-spam | P1 | `PlayerBrain` |
| ✅ | Nyamar jadi barang (tahan C) | P1 | sprite samaran per chapter nanti dari desainer |
| ✅ | Umpan (potongan nyamar menyala/berbau didatangi penjaga) | P1 | |
| ✅ | Muntahkan bahan (Q): tubuh kembali seperti sebelum bahan terakhir, bahan jatuh ke lantai | P2 | koreksi resep salah tanpa ulang level |
| ✅ | Lempar gumpalan (F) sebagai pengalih suara (`NoiseBus`) | P2 | "skill" utama selain belah |
| ✅ | Suara langkah terdengar penjaga: tubuh berat mendarat "dug", penjaga dalam radius datang memeriksa (tembus tembok). Tag HUD "BERISIK" | P2 | `IngredientData.stepNoiseRadius` (Serbuk Batu 2.2) |
| ✅ | Ciri tubuh terlihat di badan: uap (panas), serpih es (dingin), garis bau, kerlip (berkilau), debu mendarat (berat) | P2 | `SlimeTraitFx`, partikel placeholder |

## Musuh
| | Fitur | Prio | Catatan |
|---|---|---|---|
| ✅ | Golem Batu: patroli, kerucut lurus, cari, sapu pandangan, radius sadar, cahaya | P1 | |
| ✅ | Familiar Api: melayang, melihat yang PANAS/menyala dari jauh, buta di gelap, bawa cahaya sendiri | P1 | chapter 2 |
| ✅ | Anjing Lendir: mencium jejak bau (`ScentTrail`) dan menyusurinya lewat tikungan, mencium slime berbau di dekatnya, terpancing umpan berbau | P2 | chapter 2 (C2_L02) |
| ✅ | Pathfinding penjaga: A* di grid tile (Tilemap lantai/tembok), dipakai patroli, mencari, umpan, jejak bau | P1 | `GridPath`, `NavGrid` |
| ⬜ | Mata Penyihir: memeriksa toples, membongkar tiruan salah resep | P3 | chapter 3 (kandidat dipotong) |

## Bahan (chapter 1–2)
| | Bahan | Ciri | Efek |
|---|---|---|---|
| ✅ | Serbuk Batu | abu, berat | lambat |
| ✅ | Lumut Pendar | hijau, berkilau | menyala |
| ✅ | Air Embun | biru, dingin | – |
| ✅ | Ramuan Api | merah, panas | menyala |
| ✅ | Minyak Belerang | kuning, bau | meninggalkan jejak bau (Anjing) |
| ✅ | Garam Beku | putih, dingin | memadamkan nyala bahan yang dimakan sebelumnya; tubuh dingin bisa memadamkan lentera |

## Level
| | Fitur | Prio |
|---|---|---|
| ✅ | Blockout chapter 1 (4 level) + tool peta teks | P1 |
| ✅ | Blockout chapter 2 (4 level): C2_L01 Familiar, C2_L02 Anjing, C2_L03 tungku (padamkan), C2_L04 dua toples + 3 musuh | P1 |
| ✅ | Pintu + tuas (berisik) + pelat tekan (biasa/berat, ditahan potongan nyamar) + celah/lorong tikus (slime lewat, penjaga tidak, tubuh berat tidak muat). Legenda blockout `\| / o O ~`, uji di `Sandbox/ProtoProps` | P2 |
| ⬜ | Pakai props di level chapter 1–2 (keputusan desain level: tim) | P2 |
| ✅ | Lentera bisa dipadamkan (E, butuh tubuh dingin); desisnya = suara yang memancing penjaga | P2 |

## Alur game & UI
| | Fitur | Prio |
|---|---|---|
| ✅ | Pause + restart + menu utama + pilih level (OnGUI placeholder) | P1 |
| ✅ | UI fungsional (UI Toolkit): kartu resep, chip ciri tubuh, tag status, meter curiga + ?/! di atas penjaga | P1 |
| ⬜ | Tampilan UI final (font pixel, ikon ciri, gaya) oleh desainer | P1 |
| ✅ | Simpan progres level (PlayerPrefs) | P2 |
| ✅ | Sistem cerita: cutscene 3 layout + kotak dialog, StoryTrigger/StoryScene, Intro & Ending tersambung | P2 |
| ⬜ | Isi cerita: intro, dialog per chapter, ending peran tertukar (teks + gambar) oleh tim | P2 |

## Audio & art (tim desainer)
| | Fitur | Prio |
|---|---|---|
| ✅ | SFX/ambience/BGM placeholder + AudioDirector | P1 |
| ⬜ | Desain final slime + render sprite 8 frame `move` | P1 |
| ⬜ | Tileset chapter 1, toples, Golem final | P1 |
| ⬜ | Audio final | P2 |

## QA (10 Nov 2026)
Hasil test otomatis (detail cara jalan: `docs/cara-edit-unity.md` bagian 11):
- ✅ 8/8 level **bisa diselesaikan** bot dengan gerak & collision asli (penjaga dimatikan). Waktu tempuh bot tanpa
  penjaga: C1 22–60 dtk, C2 49–106 dtk. Manusia + stealth = beberapa kali lipat, masih masuk target 2–4 menit.
- ✅ Resep tiap toples bisa dibuat dari bahan yang ada; semua patroli penjaga jalan dan sampai ke tiap titiknya;
  spawn tidak terlihat penjaga; alur Boot → Menu, Intro → C1_L01, C2_L04 → Ending → Menu jalan.
- ✅ Tidak ada script hilang / referensi rusak / field wajib kosong di prefab dan scene alur game.
- 🔎 Peta pengawasan (rute terpendek, pemain tidak menyala): tidak ada sel yang diawasi terus-menerus. Bagian rute
  yang pernah terlihat penjaga: C1_L02 50%, C1_L03 78%, C1_L04 76%, C2_L01 29%, C2_L02 36%, C2_L03 34%, C2_L04 52%.
  **Kurva kesulitan terbalik**: chapter 1 lebih ramai diawasi daripada chapter 2 (C2 mengandalkan indra khusus
  Familiar/Anjing yang tidak terhitung di peta ini). Perlu dicek di playtest manusia.
- Diperbaiki saat QA: E di dekat toples (masih 1 potongan) tidak bisa makan bahan di sebelahnya; HUD "Teman x/y"
  hanya menghitung potongan aktif; warning USS `:last-child` di tiap level; cache lampu mencari ulang tiap frame di
  scene tanpa lampu titik.
- Belum bisa otomatis: rasa kontrol, keterbacaan, adil/serunya stealth. Bot stealth sederhana (nyamar saat terlihat)
  macet di C1_L02–L03, jadi itu bukan ukuran; butuh playtest manusia.

## Bug / poles yang diketahui (menyusul)
- Build Settings masih memuat C3_L01–L04, ChapterSelect, Credits (scene placeholder kosong, tidak dipakai alur game).
  Bersihkan saat tim memutuskan chapter 3 (ProjectSettings = keputusan tim).
- Sprite slime lama + squash engine bisa dobel sampai model baru dirender.
- Angka rasa gerak dan kesulitan penjaga belum disetel lewat playtest.
- Air Embun dan Minyak Belerang belum punya palet warna.
- Sprite props (pintu, tuas, pelat, celah) dan partikel ciri tubuh masih placeholder.
- Langkah berat memancing penjaga tiap mendarat: angka radius perlu disetel lewat playtest.
