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
| ⬜ | Suara langkah terdengar penjaga (berat = lebih berisik) | P2 | |
| ⬜ | Ciri tubuh terlihat di badan (uap panas, garis bau, kilau) | P2 | VFX Unity |

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
| ⬜ | Pintu / tuas / lorong tikus (hanya potongan kecil bisa lewat) | P2 |
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

## Bug / poles yang diketahui (menyusul)
- Sprite slime lama + squash engine bisa dobel sampai model baru dirender.
- Angka rasa gerak dan kesulitan penjaga belum disetel lewat playtest.
- Air Embun dan Minyak Belerang belum punya palet warna.
